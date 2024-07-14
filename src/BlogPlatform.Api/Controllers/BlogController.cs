using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Models.Blog;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation("해당 Id의 블로그를 반환합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "블로그 반환 성공", typeof(BlogRead))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 블로그 없음")]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound();
            }

            BlogRead blogDto = new(blog.Id, blog.Name, blog.Description, blog.UserId);
            return Ok(blogDto);
        }

        [UserAuthorize]
        [HttpPost]
        [SwaggerOperation("새 블로그를 생성합니다")]
        [SwaggerResponse(StatusCodes.Status201Created, "블로그 생성 성공")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Blog already exists: 기존의 블로그 있음\r\nBlog name already exists: 이름 중복됨")]
        public async Task<IActionResult> CreateAsync(BlogCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            bool isUserHasBlog = await _dbContext.Blogs.AnyAsync(b => b.UserId == userId, cancellationToken);
            if (isUserHasBlog)
            {
                _logger.LogInformation("User {userId} already has a blog", userId);
                return Problem("Blog already exists", statusCode: StatusCodes.Status409Conflict);
            }

            bool isSameNameExist = await _dbContext.Blogs.AnyAsync(b => b.Name == model.BlogName, cancellationToken);
            if (isSameNameExist)
            {
                _logger.LogInformation("Blog with name {name} already exists", model.BlogName);
                return Problem("Blog name already exists", statusCode: StatusCodes.Status409Conflict);
            }

            Blog blog = new(model.BlogName, model.Description, userId);
            _dbContext.Blogs.Add(blog);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created blog with id {id}", blog.Id);

            return CreatedAtAction("Get", "Blog", routeValues: new { id = blog.Id }, null);
        }

        [UserAuthorize]
        [HttpPut("{id:int}")]
        [SwaggerOperation("해당 Id의 블로그를 수정합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "블로그 수정 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 블로그 없음")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "이름 중복됨")]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, BlogCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound();
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
                return Conflict();
            }

            blog.Name = model.BlogName;
            blog.Description = model.Description;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [UserAuthorize]
        [HttpDelete("{id:int}")]
        [SwaggerOperation("해당 Id의 블로그를 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "블로그 삭제 성공")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "블로그의 권한 없음")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 블로그 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "블로그 삭제 실패")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.FindAsync([id], cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound();
            }

            if (blog.UserId != userId)
            {
                _logger.LogInformation("User {userId} is not authorized to delete blog with id {id}", userId, id);
                return Forbid();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(blog, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [UserAuthorize]
        [HttpPost("restore/{id:int}")]
        [SwaggerOperation("해당 Id의 블로그를 복원합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "블로그 복원 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Blog not deleted: 삭제되지 않은 블로그\r\nCan not restore blog over time: 복구 시간 만료됨")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 블로그 없음")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Blog already exist: 새로 생성한 블로그 있음\r\nBlog name already exist: 블로그 이름 중복")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "블로그 복원 실패")]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [FromBody] BlogCreate? model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Blog? blog = await _dbContext.Blogs.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
            if (blog == null)
            {
                _logger.LogInformation("Blog with id {id} not found", id);
                return NotFound();
            }

            if (blog.IsSoftDeletedAtDefault())
            {
                _logger.LogInformation("Blog with id {id} is not deleted", id);
                return Problem("Blog not deleted", statusCode: StatusCodes.Status400BadRequest);
            }

            if (blog.UserId != userId)
            {
                _logger.LogInformation("User {userId} is not authorized to restore blog with id {id}", userId, id);
                return Forbid();
            }

            if (_dbContext.Blogs.Any(b => b.UserId == userId))
            {
                _logger.LogInformation("User {userId} already has a blog", userId);
                return Problem("Blog already exist", statusCode: StatusCodes.Status409Conflict);
            }

            if (blog.SoftDeletedAt.Add(TimeSpan.FromDays(1)) < _timeProvider.GetUtcNow())
            {
                _logger.LogInformation("Blog with id {id} is not restorable", id);
                return Problem("Can not restore blog over time", statusCode: StatusCodes.Status400BadRequest);
            }

            blog.Name = model?.BlogName ?? blog.Name;
            blog.Description = model?.Description ?? blog.Description;

            if (await _dbContext.Blogs.AnyAsync(b => b.Name == blog.Name, cancellationToken))
            {
                _logger.LogInformation("Blog with name {name} already exists", blog.Name);
                return Problem("Blog name already exists", statusCode: StatusCodes.Status409Conflict);
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(blog, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }
    }
}
