using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.Blog;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<BlogController> _logger;

        public BlogController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, TimeProvider timeProvider, ILogger<BlogController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BlogRead), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CreateAsync(BlogCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            bool isUserHasBlog = await _dbContext.Blogs.AnyAsync(b => b.UserId == userId, cancellationToken);
            if (isUserHasBlog)
            {
                _logger.LogInformation("User {userId} already has a blog", userId);
                return Conflict(new Error("이미 블로그가 존재합니다"));
            }

            bool isSameNameExist = await _dbContext.Blogs.AnyAsync(b => b.Name == model.BlogName, cancellationToken);
            if (isSameNameExist)
            {
                _logger.LogInformation("Blog with name {name} already exists", model.BlogName);
                return Conflict(new Error("이미 존재하는 블로그 이름입니다"));
            }

            Blog blog = new(model.BlogName, model.Description, userId);
            _dbContext.Blogs.Add(blog);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created blog with id {id}", blog.Id);

            return CreatedAtAction("Get", "Blog", routeValues: new { id = blog.Id }, null);
        }

        [UserAuthorize]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, BlogCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
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

            bool isSameNameExist = blog.Name != model.BlogName && await _dbContext.Blogs.AnyAsync(b => b.Name == model.BlogName, cancellationToken);
            if (isSameNameExist)
            {
                _logger.LogInformation("Blog with name {name} already exists", model.BlogName);
                return Conflict(new Error("이미 존재하는 블로그 이름입니다"));
            }

            blog.Name = model.BlogName;
            blog.Description = model.Description;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [UserAuthorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
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
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [UserAuthorize]
        [HttpPost("restore/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [FromBody] BlogCreate? model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
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

            if (blog.SoftDeletedAt.Add(TimeSpan.FromDays(1)) < _timeProvider.GetUtcNow())
            {
                _logger.LogInformation("Blog with id {id} is not restorable", id);
                return BadRequest(new Error("복원할 수 없는 블로그입니다"));
            }

            blog.Name = model?.BlogName ?? blog.Name;
            blog.Description = model?.Description ?? blog.Description;

            if (await _dbContext.Blogs.AnyAsync(b => b.Name == blog.Name, cancellationToken))
            {
                _logger.LogInformation("Blog with name {name} already exists", blog.Name);
                return Conflict(new Error("이미 존재하는 블로그 이름입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(blog, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }
    }
}
