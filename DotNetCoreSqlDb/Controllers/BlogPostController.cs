using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DotNetCoreSqlDb;
using DotNetCoreSqlDb.Data;
using DotNetCoreSqlDb.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace DotNetCoreSqlDb.Controllers
{
    [ActionTimerFilter]
    public class BlogPostController : Controller
    {
        private readonly MyDatabaseContext _context;
        private readonly IDistributedCache _cache;
        private readonly string _BlogPostsCacheKey = "BlogPostsList";

        public BlogPostController(MyDatabaseContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Blogs
        public async Task<IActionResult> Index()
        {
            var blogs = new List<Blog>();
            byte[]? BlogPostsByteArray;

            BlogPostsByteArray = await _cache.GetAsync(_BlogPostsCacheKey);
            if (BlogPostsByteArray != null && BlogPostsByteArray.Length > 0)
            { 
                blogs = ConvertData<Blog>.ByteArrayToObjectList(BlogPostsByteArray);
            }
            else 
            {
                blogs = await _context.Blog.ToListAsync();
                BlogPostsByteArray = ConvertData<Blog>.ObjectListToByteArray(blogs);
                await _cache.SetAsync(_BlogPostsCacheKey, BlogPostsByteArray);
            }

            return View(blogs);
        }

        // GET: Blog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            byte[]? blogItemByteArray;
            Blog? blog;

            if (id == null)
            {
                return NotFound();
            }

            blogItemByteArray = await _cache.GetAsync(GetBlogPostsCacheKey(id));

            if (blogItemByteArray != null && blogItemByteArray.Length > 0)
            {
                blog = ConvertData<Blog>.ByteArrayToObject(blogItemByteArray);
            }
            else 
            {
                blog = await _context.Blog
                .FirstOrDefaultAsync(m => m.PostID == id);
            if (blog == null)
            {
                return NotFound();
            }

                blogItemByteArray = ConvertData<Blog>.ObjectToByteArray(blog);
                await _cache.SetAsync(GetBlogPostsCacheKey(id), blogItemByteArray);
            }

            

            return View(blog);
        }

        // GET: Blog/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Blog/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Description,CreatedDate")] Blog blog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(blog);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(_BlogPostsCacheKey);
                return RedirectToAction(nameof(Index));
            }
            return View(blog);
        }

        // GET: Blog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blog.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }
            return View(blog);
        }

        // POST: Blog/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Title,Content,Author,CreatedDate")] Blog blog)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(blog);
                    await _context.SaveChangesAsync();
                    await _cache.RemoveAsync(GetBlogPostsCacheKey(blog.PostID));
                    await _cache.RemoveAsync(_BlogPostsCacheKey);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogExists(blog.PostID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(blog);
        }

        // GET: Blog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blog
                .FirstOrDefaultAsync(m => m.PostID == id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // POST: Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var blog = await _context.Blog.FindAsync(id);
            if (blog != null)
            {
                _context.Blog.Remove(blog);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(GetBlogPostsCacheKey(blog.PostID));
                await _cache.RemoveAsync(_BlogPostsCacheKey);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BlogExists(int id)
        {
            return _context.Blog.Any(e => e.PostID == id);
        }

        private string GetBlogPostsCacheKey(int? id)
        {
            return _BlogPostsCacheKey+"_&_"+id;
        }
    }

    
}
