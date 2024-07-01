using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.Category;

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
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, TimeProvider timeProvider, ILogger<CategoryController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryRead), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CreateAsync([FromBody] CategoryNameModel model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            int blogId = await _dbContext.Blogs.Where(b => b.UserId == userId).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (blogId == default)
            {
                _logger.LogInformation("User with id {userId} does not have a blog", userId);
                return BadRequest(new Error("블로그를 먼저 생성해주세요"));
            }

            Category category = new(model.Name, blogId);
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category {categoryName} created", model.Name);

            return CreatedAtAction("Get", "Category", new { id = category.Id }, null);
        }

        [UserAuthorize]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CategoryNameModel model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.Where(c => c.Id == id).Select(c => new { category = c, userId = c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo is null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.userId != userId)
            {
                _logger.LogInformation("User with id {userId} does not have permission to update category with id {id}", userId, id);
                return Forbid();
            }

            categoryInfo.category.Name = model.Name;
            _dbContext.Categories.Update(categoryInfo.category);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category {categoryName} updated", model.Name);

            return NoContent();
        }

        [UserAuthorize]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.Where(c => c.Id == id).Select(c => new { category = c, userId = c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo is null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.userId != userId)
            {
                _logger.LogInformation("User with id {userId} does not have permission to delete category with id {id}", userId, id);
                return Forbid();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(categoryInfo.category, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [UserAuthorize]
        [HttpPost("restore/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.IgnoreSoftDeleteFilter().Where(c => c.Id == id).Select(c => new { category = c, userId = c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo is null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.userId != userId)
            {
                _logger.LogInformation("User with id {userId} does not have permission to delete category with id {id}", userId, id);
                return Forbid();
            }

            if (categoryInfo.category.IsSoftDeletedAtDefault())
            {
                _logger.LogInformation("Category with id {id} is not deleted", id);
                return BadRequest(new Error("삭제되지 않은 카테고리입니다"));
            }

            if (categoryInfo.category.SoftDeletedAt.Add(TimeSpan.FromDays(1)) < _timeProvider.GetUtcNow())
            {
                _logger.LogInformation("Category with id {id} is not restorable", id);
                return BadRequest(new Error("복원할 수 없는 카테고리입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(categoryInfo.category, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }
    }
}
