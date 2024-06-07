using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Ganss.Xss;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

using SoftDeleteServices.Concrete;

using System.Diagnostics;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly IIdentityService _identityService;
        private readonly SoftDeleteConfigure _softDeleteConfigure;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<PostController> _logger;

        public PostController(BlogPlatformDbContext dbContext, IIdentityService identityService, IDistributedCache distributedCache, ILogger<PostController> logger)
        {
            _dbContext = dbContext;
            _identityService = identityService;
            _softDeleteConfigure = new(_dbContext);
            _distributedCache = distributedCache;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            HtmlSanitizer htmlSanitizer = new();
            Post? post = await _dbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
            {
                _logger.LogInformation("Post with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 포스트입니다"));
            }

            string sanitizedContent = htmlSanitizer.Sanitize(post.Content);
            PostReadDto postDto = new(post.Id, post.Title, sanitizedContent, post.CategoryId);
            return Ok(postDto);
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [UserAuthorize]
        public async Task<IActionResult> CreateAsync([FromForm] string title, [FromForm] string content, [FromForm] string categoryName, [FromForm] List<string> tags, CancellationToken cancellationToken)
        {
            if (!_identityService.TryGetUserId(User, out int userId))
            {
                Debug.Assert(false);
                throw new Exception("Invalid User");
            }

            var categoryInfo = await _dbContext.Categories.Where(c => c.Name == categoryName).Select(c => new { c.Id, c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo == null)
            {
                _logger.LogInformation("Category with name {categoryName} not found or does not belong to user with id {userId}", categoryName, userId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.UserId != userId)
            {
                _logger.LogInformation("Category with name {categoryName} does not belong to user with id {userId}", categoryName, userId);
                return Forbid();
            }

            Post post = new(title, content, categoryInfo.Id);
            post.Tags.AddRange(tags);

            _dbContext.Posts.Add(post);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetAsync), new { id = post.Id }, post);
        }

        [HttpPut("{id:int}")]
        [UserAuthorize]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromForm] string title, [FromForm] string content, [FromForm] string categoryName, [FromForm] List<string> tags, CancellationToken cancellationToken)
        {
            if (_identityService.TryGetUserId(User, out int userId))
            {
                Debug.Assert(false);
                throw new Exception("Invalid User");
            }

            var categoryInfo = await _dbContext.Categories.Where(c => c.Name == categoryName).Select(c => new { c.Id, c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo == null)
            {
                _logger.LogInformation("Category with name {categoryName} not found or does not belong to user with id {userId}", categoryName, userId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.UserId != userId)
            {
                _logger.LogInformation("Category with name {categoryName} does not belong to user with id {userId}", categoryName, userId);
                return Forbid();
            }

            Post? post = await _dbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
            {
                _logger.LogInformation("Post with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 포스트입니다"));
            }

            post.Title = title;
            post.Content = content;
            post.CategoryId = categoryInfo.Id;
            post.Tags.Clear();
            post.Tags.AddRange(tags);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok();
        }

        [HttpDelete("{id:int}")]
        [UserAuthorize]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            if (!_identityService.TryGetUserId(User, out int userId))
            {
                Debug.Assert(false);
                throw new Exception("Invalid User");
            }

            Post? post = await _dbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
            {
                _logger.LogInformation("Post with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 포스트입니다"));
            }

            int blogUserId = await _dbContext.Categories.Where(c => c.Id == post.CategoryId).Select(c => c.Blog.UserId).FirstOrDefaultAsync(cancellationToken);
            if (blogUserId != userId)
            {
                _logger.LogInformation("Post with id {id} does not belong to user with id {userId}", id, userId);
                return Forbid();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            await softDelService.SetCascadeSoftDeleteAsync(post);
            return NoContent();
        }

        [HttpPost("restore/{id:int}")]
        [UserAuthorize]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            if (!_identityService.TryGetUserId(User, out int userId))
            {
                Debug.Assert(false);
                throw new Exception("Invalid User");
            }

            Post? post = await _dbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
            {
                _logger.LogInformation("Post with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 포스트입니다"));
            }

            if (post.SoftDeleteLevel == 0)
            {
                _logger.LogInformation("Post with id {id} is not deleted", id);
                return BadRequest(new Error("삭제되지 않은 포스트입니다"));
            }

            int blogUserId = await _dbContext.Categories.Where(c => c.Id == post.CategoryId).Select(c => c.Blog.UserId).FirstOrDefaultAsync(cancellationToken);
            if (blogUserId != userId)
            {
                _logger.LogInformation("Post with id {id} does not belong to user with id {userId}", id, userId);
                return Forbid();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            await softDelService.ResetCascadeSoftDeleteAsync(post);
            return NoContent();
        }
    }
}
