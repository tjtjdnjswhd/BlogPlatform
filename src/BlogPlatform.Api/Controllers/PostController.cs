using AngleSharp;
using AngleSharp.Dom;

using BlogPlatform.Api.Filters;
using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Ganss.Xss;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

using System.ComponentModel;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly IPostImageService _imageService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PostController> _logger;

        public PostController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, IPostImageService imageService, TimeProvider timeProvider, ILogger<PostController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _imageService = imageService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PostRead), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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
            PostRead postDto = new(post.Id, post.Title, sanitizedContent, post.CategoryId);
            return Ok(postDto);
        }

        [HttpGet]
        public IAsyncEnumerable<PostSearchResult> Get([FromQuery] PostSearch search)
        {
            IQueryable<Post> postQuery = _dbContext.Posts;
            if (search.BlogId is not null)
            {
                postQuery = postQuery.Where(p => p.Category.BlogId == search.BlogId);
            }
            else
            {
                postQuery = postQuery.Where(p => p.CategoryId == search.CategoryId);
            }

            if (search.Title is not null)
            {
                postQuery = postQuery.Where(p => p.Title.Contains(search.Title));
            }

            if (search.Content is not null)
            {
                postQuery = postQuery.Where(p => p.Content.Contains(search.Content));
            }

            if (search.CreatedAtStart is not null)
            {
                postQuery = postQuery.Where(p => p.CreatedAt >= search.CreatedAtStart);
            }

            if (search.CreatedAtEnd is not null)
            {
                postQuery = postQuery.Where(p => p.CreatedAt <= search.CreatedAtEnd);
            }

            if (search.UpdatedAtStart is not null)
            {
                postQuery = postQuery.Where(p => p.LastUpdatedAt >= search.UpdatedAtStart);
            }

            if (search.UpdatedAtEnd is not null)
            {
                postQuery = postQuery.Where(p => p.LastUpdatedAt <= search.UpdatedAtEnd);
            }

            if (search.Tags is not null)
            {
                postQuery = postQuery.FilterTag(search.Tags, search.TagFilterOption);
            }

            postQuery = (search.OrderBy, search.OrderDirection) switch
            {
                (EPostSearchOrderBy.Title, ListSortDirection.Ascending) => postQuery.OrderBy(p => p.Title),
                (EPostSearchOrderBy.Title, ListSortDirection.Descending) => postQuery.OrderByDescending(p => p.Title),
                (EPostSearchOrderBy.CreatedAt, ListSortDirection.Ascending) => postQuery.OrderBy(p => p.CreatedAt),
                (EPostSearchOrderBy.CreatedAt, ListSortDirection.Descending) => postQuery.OrderByDescending(p => p.CreatedAt),
                (EPostSearchOrderBy.UpdatedAt, ListSortDirection.Ascending) => postQuery.OrderBy(p => p.LastUpdatedAt),
                (EPostSearchOrderBy.UpdatedAt, ListSortDirection.Descending) => postQuery.OrderByDescending(p => p.LastUpdatedAt),
                _ => throw new InvalidEnumArgumentException(nameof(search.OrderBy), (int)search.OrderBy, typeof(EPostSearchOrderBy))
            };

            postQuery = postQuery.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize);

            IQueryable<PostSearchResult> result = postQuery.Select(p => new PostSearchResult(p.Id, p.Title, p.CategoryId));

            return result.AsAsyncEnumerable();
        }

        [HttpPost]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CreateAsync([FromBody] PostCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.Where(c => c.Id == model.CategoryId).Select(c => new { c.Id, c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo == null)
            {
                _logger.LogInformation("Category with id {categoryId} not found", model.CategoryId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.UserId != userId)
            {
                _logger.LogInformation("Category with id {categoryId} does not belong to user with id {userId}", model.CategoryId, userId);
                return Forbid();
            }

            IEnumerable<string> serverImages = await GetServerImgNamesAsync(model.Content, cancellationToken);
            bool isImageSaved = await _imageService.CacheImagesToDatabaseAsync(serverImages, cancellationToken);
            if (!isImageSaved)
            {
                _logger.LogWarning("Some images not found in cache. Post creation aborted.");
                return StatusCode(StatusCodes.Status500InternalServerError, new Error("이미지를 저장하는데 실패했습니다"));
            }

            Post post = new(model.Title, model.Content, categoryInfo.Id);
            post.Tags.AddRange(model.Tags);

            _dbContext.Posts.Add(post);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while saving post to database");
                await _imageService.RemoveImageFromDatabaseAsync(serverImages, CancellationToken.None);
                throw;
            }

            return CreatedAtAction("Get", "Post", new { id = post.Id }, null);
        }

        [HttpPut("{id:int}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] PostCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            var categoryInfo = await _dbContext.Categories.Where(c => c.Id == model.CategoryId).Select(c => new { c.Id, c.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (categoryInfo == null)
            {
                _logger.LogInformation("Category with id {categoryId} not found", model.CategoryId);
                return NotFound(new Error("존재하지 않는 카테고리입니다"));
            }

            if (categoryInfo.UserId != userId)
            {
                _logger.LogInformation("Category with id {categoryId} does not belong to user with id {userId}", model.CategoryId, userId);
                return Forbid();
            }

            var postInfo = await _dbContext.Posts.Where(p => p.Id == id).Select(p => new { post = p, userId = p.Category.Blog.UserId }).FirstOrDefaultAsync(cancellationToken);
            if (postInfo == null)
            {
                _logger.LogInformation("Post with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 포스트입니다"));
            }

            if (postInfo.userId != userId)
            {
                _logger.LogInformation("Post with id {id} does not belong to user with id {userId}", id, userId);
                return Forbid();
            }

            IEnumerable<string> oldImgSrcs = await GetServerImgNamesAsync(postInfo.post.Content, cancellationToken);
            IEnumerable<string> newImgSrcs = await GetServerImgNamesAsync(model.Content, cancellationToken);
            IEnumerable<string> addedImgSrcs = newImgSrcs.Except(oldImgSrcs);
            IEnumerable<string> deletedImgSrcs = oldImgSrcs.Except(newImgSrcs);

            bool isImageSaved = await _imageService.CacheImagesToDatabaseAsync(addedImgSrcs, cancellationToken);
            if (!isImageSaved)
            {
                _logger.LogWarning("Some images not found in cache. Post update aborted.");
                return StatusCode(StatusCodes.Status500InternalServerError, new Error("이미지를 저장하는데 실패했습니다"));
            }

            await _imageService.RemoveImageFromDatabaseAsync(deletedImgSrcs, cancellationToken);

            postInfo.post.Title = model.Title;
            postInfo.post.Content = model.Content;
            postInfo.post.CategoryId = categoryInfo.Id;
            postInfo.post.Tags.Clear();
            postInfo.post.Tags.AddRange(model.Tags);

            _dbContext.Posts.Update(postInfo.post);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
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

            var status = await _softDeleteService.SetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("restore/{id:int}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RestoreAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Post? post = await _dbContext.Posts.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (post == null)
            {
                _logger.LogInformation("Post with id {id} not found", id);
                return NotFound(new Error("존재하지 않는 포스트입니다"));
            }

            if (post.IsSoftDeletedAtDefault())
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

            if (post.SoftDeletedAt.Add(TimeSpan.FromDays(1)) < _timeProvider.GetUtcNow())
            {
                _logger.LogInformation("Post with id {id} is not restorable", id);
                return BadRequest(new Error("복원할 수 없는 게시글입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("image")]
        [UserAuthorize]
        [PostImageFilter(nameof(image))]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<CreatedAtActionResult> CacheImageAsync(IFormFile image, CancellationToken cancellationToken)
        {
            DistributedCacheEntryOptions imageCacheOptions = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            };

            string fileName = Guid.NewGuid().ToString();

            int length = checked((int)image.Length);
            using MemoryStream memoryStream = new(length);

            _logger.LogInformation("Caching image. file name: [{fileName}]", fileName);
            await image.CopyToAsync(memoryStream, cancellationToken);
            ImageInfo imageInfo = new(image.ContentType, memoryStream.ToArray());
            await _imageService.CacheImageAsync(fileName, imageInfo, imageCacheOptions, cancellationToken);

            return CreatedAtAction("GetImage", "Post", new { fileName }, null);
        }

        [HttpGet("image/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetImageAsync([FromRoute] string fileName, CancellationToken cancellationToken)
        {
            ImageInfo? image = await _imageService.GetImageAsync(fileName, EGetImageMode.CacheThenDatabase, cancellationToken);
            return image is null ? NotFound() : File(image.Data, image.ContentType);
        }

        private async Task<IEnumerable<string>> GetServerImgNamesAsync(string content, CancellationToken cancellationToken = default)
        {
            IBrowsingContext browsingContext = BrowsingContext.New(Configuration.Default);
            IDocument document = await browsingContext.OpenAsync(req => req.Content(content), cancellationToken);
            IEnumerable<string?> imageSrcs = document.Images.Select(i => i.Source);

            string baseUrl = Url.ActionLink("GetImage", "Post", new { fileName = "s" })?.TrimEnd('s') ?? throw new Exception();
            IEnumerable<string> serverImages = imageSrcs.Where(src => src?.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase) ?? false).Select(src => src![baseUrl.Length..]);
            return serverImages;
        }
    }
}
