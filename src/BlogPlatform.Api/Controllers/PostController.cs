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
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<PostController> _logger;

        public PostController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, IPostImageService imageService, IDistributedCache distributedCache, ILogger<PostController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _imageService = imageService;
            _distributedCache = distributedCache;
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

            postQuery = search.OrderBy switch
            {
                EPostSearchOrderBy.Title => search.OrderDirection switch
                {
                    ListSortDirection.Ascending => postQuery.OrderBy(p => p.Title),
                    ListSortDirection.Descending => postQuery.OrderByDescending(p => p.Title),
                    _ => throw new InvalidEnumArgumentException(nameof(search.OrderDirection), (int)search.OrderDirection, typeof(ListSortDirection))
                },
                EPostSearchOrderBy.CreatedAt => search.OrderDirection switch
                {
                    ListSortDirection.Ascending => postQuery.OrderBy(p => p.CreatedAt),
                    ListSortDirection.Descending => postQuery.OrderByDescending(p => p.CreatedAt),
                    _ => throw new InvalidEnumArgumentException(nameof(search.OrderDirection), (int)search.OrderDirection, typeof(ListSortDirection))
                },
                EPostSearchOrderBy.UpdatedAt => search.OrderDirection switch
                {
                    ListSortDirection.Ascending => postQuery.OrderBy(p => p.LastUpdatedAt),
                    ListSortDirection.Descending => postQuery.OrderByDescending(p => p.LastUpdatedAt),
                    _ => throw new InvalidEnumArgumentException(nameof(search.OrderDirection), (int)search.OrderDirection, typeof(ListSortDirection))
                },
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
        public async Task<IActionResult> CreateAsync([FromForm] string title, [FromForm] string content, [FromForm] string categoryName, [FromForm] List<string> tags, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
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

            IEnumerable<string> serverImages = await GetServerImgUrlsAsync(content, cancellationToken);

            using var transaction = _dbContext.Database.BeginTransaction();
            bool isAllImageSaved = await _imageService.WithTransaction(transaction).CacheImagesToDatabaseAsync(serverImages, cancellationToken);
            if (!isAllImageSaved)
            {
                _logger.LogWarning("Some images not found in cache. Post creation aborted.");
                transaction.Rollback();
                return StatusCode(StatusCodes.Status500InternalServerError, new Error("이미지를 저장하는데 실패했습니다"));
            }

            Post post = new(title, content, categoryInfo.Id);
            post.Tags.AddRange(tags);

            _dbContext.Posts.Add(post);
            await _dbContext.SaveChangesAsync(cancellationToken);
            transaction.Commit();

            return CreatedAtAction(nameof(GetAsync), "Post", new { id = post.Id }, null);
        }

        [HttpPut("{id:int}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromForm] string title, [FromForm] string content, [FromForm] string categoryName, [FromForm] List<string> tags, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
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

            IEnumerable<string> oldImgSrcs = await GetServerImgUrlsAsync(post.Content, cancellationToken);
            IEnumerable<string> newImgSrcs = await GetServerImgUrlsAsync(content, cancellationToken);
            IEnumerable<string> addedImgSrcs = newImgSrcs.Except(oldImgSrcs);
            IEnumerable<string> deletedImgSrcs = oldImgSrcs.Except(newImgSrcs);

            using var transaction = _dbContext.Database.BeginTransaction();
            IPostImageService imageServiceWithTransaction = _imageService.WithTransaction(transaction);
            bool isAllImageSaved = await imageServiceWithTransaction.CacheImagesToDatabaseAsync(addedImgSrcs, cancellationToken);
            if (!isAllImageSaved)
            {
                _logger.LogWarning("Some images not found in cache. Post update aborted.");
                transaction.Rollback();
                return StatusCode(StatusCodes.Status500InternalServerError, new Error("이미지를 저장하는데 실패했습니다"));
            }

            await imageServiceWithTransaction.RemoveImageFromDatabaseAsync(deletedImgSrcs, cancellationToken);

            post.Title = title;
            post.Content = content;
            post.CategoryId = categoryInfo.Id;
            post.Tags.Clear();
            post.Tags.AddRange(tags);

            await _dbContext.SaveChangesAsync(cancellationToken);
            transaction.Commit();

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

            var status = await _softDeleteService.ResetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("image")]
        [UserAuthorize]
        [PostImageFilter(nameof(images))]
        public async Task<IEnumerable<string>> CacheImagesAsync([FromForm] List<IFormFile> images, CancellationToken cancellationToken)
        {
            DistributedCacheEntryOptions imageCacheOptions = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            };

            string[] fileNames = await Task.WhenAll(images.AsParallel().WithCancellation(cancellationToken).Select(async file =>
            {
                string fileName = Guid.NewGuid().ToString();
                int length = checked((int)file.Length);
                using MemoryStream memoryStream = new(length);

                await file.CopyToAsync(memoryStream);
                ImageInfo imageInfo = new(file.ContentType, memoryStream.ToArray());
                await _imageService.CacheImageAsync(fileName, imageInfo, imageCacheOptions, cancellationToken);
                return fileName;
            }));

            _logger.LogInformation("Caching images. file names: [{fileNames}]", fileNames);
            return fileNames;
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

        private async Task<IEnumerable<string>> GetServerImgUrlsAsync(string content, CancellationToken cancellationToken = default)
        {
            IBrowsingContext browsingContext = BrowsingContext.New(Configuration.Default);
            IDocument document = await browsingContext.OpenAsync(req => req.Content(content), cancellationToken);
            IEnumerable<string?> imageSrcs = document.Images.Select(i => i.ActualSource);
            string baseUrl = HttpContext.GetServerVariable("baseUri") ?? throw new InvalidOperationException("baseUri not found in server variables");
            IEnumerable<string> serverImages = imageSrcs.Where(src => src is not null && src.StartsWith(baseUrl))!;
            return serverImages;
        }
    }
}
