using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Models.Category;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation("해당 Id의 카테고리를 반환합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "카테고리 반환 성공", typeof(CategoryRead))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 카테고리 없음")]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            Category? category = await _dbContext.Categories.FindAsync([id], cancellationToken);
            if (category == null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound();
            }

            CategoryRead categoryDto = new(category.Id, category.Name, category.BlogId);
            return Ok(categoryDto);
        }

        [UserAuthorize]
        [HttpPost]
        [SwaggerOperation("새 카테고리를 생성합니다")]
        [SwaggerResponse(StatusCodes.Status201Created, "카테고리 생성 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "블로그가 없음")]
        public async Task<IActionResult> CreateAsync([FromBody] CategoryNameModel model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            int blogId = await _dbContext.Blogs.Where(b => b.UserId == userId).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (blogId == default)
            {
                _logger.LogInformation("User with id {userId} does not have a blog", userId);
                return BadRequest();
            }

            Category category = new(model.Name, blogId);
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category {categoryName} created", model.Name);

            return CreatedAtAction("Get", "Category", new { id = category.Id }, null);
        }

        [UserAuthorize]
        [HttpPut("{id:int}")]
        [SwaggerOperation("해당 Id의 카테고리를 수정합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "카테고리 수정 성공")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "카테고리의 권한 없음")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 카테고리 없음")]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CategoryNameModel model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.Where(c => c.Id == id).Select(c => new { category = c, userId = c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo is null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound();
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
        [SwaggerOperation("해당 Id의 카테고리를 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "카테고리 삭제 성공")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "카테고리의 권한 없음")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 카테고리 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "카테고리 삭제 실패")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.Where(c => c.Id == id).Select(c => new { category = c, userId = c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo is null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound();
            }

            if (categoryInfo.userId != userId)
            {
                _logger.LogInformation("User with id {userId} does not have permission to delete category with id {id}", userId, id);
                return Forbid();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(categoryInfo.category, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(detail: status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [UserAuthorize]
        [HttpPost("restore/{id:int}")]
        [SwaggerOperation("해당 Id의 카테고리를 복원합니다")]
        [SwaggerResponse(statusCode: StatusCodes.Status204NoContent, "카테고리 복원 성공")]
        [SwaggerResponse(statusCode: StatusCodes.Status400BadRequest, "카테고리가 삭제되지 않음")]
        [SwaggerResponse(statusCode: StatusCodes.Status404NotFound, "해당 카테고리 없음")]
        [SwaggerResponse(statusCode: StatusCodes.Status500InternalServerError, "카테고리 복원 실패")]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.IgnoreSoftDeleteFilter().Where(c => c.Id == id).Select(c => new { category = c, userId = c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo is null)
            {
                _logger.LogInformation("Category with id {id} not found", id);
                return NotFound();
            }

            if (categoryInfo.userId != userId)
            {
                _logger.LogInformation("User with id {userId} does not have permission to delete category with id {id}", userId, id);
                return Forbid();
            }

            if (categoryInfo.category.IsSoftDeletedAtDefault())
            {
                _logger.LogInformation("Category with id {id} is not deleted", id);
                return Problem("Category not deleted", statusCode: StatusCodes.Status400BadRequest);
            }

            if (categoryInfo.category.SoftDeletedAt.Add(TimeSpan.FromDays(1)) < _timeProvider.GetUtcNow())
            {
                _logger.LogInformation("Category with id {id} is not restorable", id);
                return Problem("Can not restore category over time", statusCode: StatusCodes.Status400BadRequest);
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(categoryInfo.category, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }
    }
}
