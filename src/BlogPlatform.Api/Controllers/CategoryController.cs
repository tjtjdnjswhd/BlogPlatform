using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, ILogger<CategoryController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            Category? category = await _dbContext.Categories.FindAsync([id], cancellationToken);
            if (category == null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            CategoryRead categoryDto = new(category.Id, category.Name, category.BlogId);
            return Ok(categoryDto);
        }

        [UserAuthorize]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] string categoryName, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            int blogId = await _dbContext.Blogs.Where(b => b.UserId == userId).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (blogId == default)
            {
                _logger.LogInformation("User with id {userId} does not have a blog", userId);
                return BadRequest(new Error("블로그를 먼저 생성해주세요"));
            }

            Category category = new(categoryName, blogId);
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category {categoryName} created", categoryName);

            return CreatedAtAction("GetAsync", "Category", new { id = category.Id }, null);
        }

        [UserAuthorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromForm] string categoryName, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            int blogId = await _dbContext.Blogs.Where(b => b.UserId == userId).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (blogId == default)
            {
                _logger.LogInformation("User with id {userId} does not have a blog", userId);
                return BadRequest(new Error("블로그를 먼저 생성해주세요"));
            }

            Category? category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id && c.BlogId == blogId, cancellationToken);
            if (category == null)
            {
                _logger.LogInformation("Category for blog with id {blogId} not found", blogId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            category.Name = categoryName;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category {categoryName} updated", categoryName);

            return NoContent();
        }

        [UserAuthorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            int blogId = await _dbContext.Blogs.Where(b => b.UserId == userId).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (blogId == default)
            {
                _logger.LogInformation("User with id {userId} does not have a blog", userId);
                return BadRequest(new Error("블로그를 먼저 생성해주세요"));
            }

            Category? category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id && c.BlogId == blogId, cancellationToken);
            if (category == null)
            {
                _logger.LogInformation("Category for blog with id {blogId} not found", blogId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(category, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [UserAuthorize]
        [HttpPost("restore/{id:int}")]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            int blogId = await _dbContext.Blogs.Where(b => b.UserId == userId).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (blogId == default)
            {
                _logger.LogInformation("User with id {userId} does not have a blog", userId);
                return BadRequest(new Error("블로그를 먼저 생성해주세요"));
            }

            Category? category = await _dbContext.Categories.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(c => c.Id == id && c.BlogId == blogId, cancellationToken);
            if (category == null)
            {
                _logger.LogInformation("Category for blog with id {blogId} not found", blogId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (category.IsSoftDeletedAtDefault())
            {
                _logger.LogInformation("Category with id {id} is not deleted", id);
                return BadRequest(new Error("삭제되지 않은 카테고리입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(category, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }
    }
}
