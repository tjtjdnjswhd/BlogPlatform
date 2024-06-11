using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly ILogger<BlogController> _logger;

        public BlogController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, ILogger<BlogController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 블로그입니다"));
            }

            BlogRead blogDto = new(blog.Id, blog.Name, blog.Description, blog.UserId);
            return Ok(blogDto);
        }

        [UserAuthorize]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] string blogName, [FromForm] string description, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            bool isUserHasBlog = await _dbContext.Blogs.AnyAsync(b => b.UserId == userId, cancellationToken);
            if (isUserHasBlog)
            {
                _logger.LogInformation("User {userId} already has a blog", userId);
                return Conflict(new Error("이미 블로그가 존재합니다"));
            }

            Blog blog = new(blogName, description, userId);
            _dbContext.Blogs.Add(blog);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created blog with id {id}", blog.Id);

            return CreatedAtAction(nameof(GetAsync), "Blog", routeValues: new { id = blog.Id }, null);
        }

        [UserAuthorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromForm, Required(AllowEmptyStrings = false)] string blogName, [FromForm, Required(AllowEmptyStrings = false)] string description, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 블로그입니다"));
            }

            if (blog.UserId != userId)
            {
                _logger.LogInformation("User {userId} is not authorized to update blog with id {id}", userId, id);
                return Forbid();
            }

            blog.Name = blogName;
            blog.Description = description;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [UserAuthorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 블로그입니다"));
            }

            if (blog.UserId != userId)
            {
                _logger.LogInformation("User {userId} is not authorized to delete blog with id {id}", userId, id);
                return Forbid();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(blog, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? BadRequest(status.Message) : NoContent();
        }

        [UserAuthorize]
        [HttpPost("restore/{id:int}")]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 블로그입니다"));
            }

            if (blog.IsSoftDeletedAtDefault())
            {
                _logger.LogInformation("Blog with id {id} is not deleted", id);
                return BadRequest(new Error("삭제되지 않은 블로그입니다"));
            }

            if (blog.UserId != userId)
            {
                _logger.LogInformation("User {userId} is not authorized to restore blog with id {id}", userId, id);
                return Forbid();
            }

            if (_dbContext.Blogs.Any(b => b.UserId == userId))
            {
                _logger.LogInformation("User {userId} already has a blog", userId);
                return Conflict(new Error("이미 블로그가 존재합니다"));
            }

            if (blog.SoftDeletedAt.Add(TimeSpan.FromDays(1)) < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Blog with id {id} is not restorable", id);
                return BadRequest(new Error("복원할 수 없는 블로그입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(blog, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? BadRequest(status.Message) : NoContent();
        }
    }
}
