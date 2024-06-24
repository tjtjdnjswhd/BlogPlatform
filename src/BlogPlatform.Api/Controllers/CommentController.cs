using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<CommentController> _logger;

        public CommentController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, TimeProvider timeProvider, ILogger<CommentController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CommentRead), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            Comment? comment = await _dbContext.Comments.FindAsync([id], cancellationToken);
            if (comment == null)
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            return Ok(new CommentRead(comment.Id, comment.Content, comment.CreatedAt, comment.LastUpdatedAt, comment.PostId, comment.UserId, comment.ParentCommentId));
        }

        [HttpGet("post/{postId:int}")]
        public IAsyncEnumerable<CommentRead> GetByPost([FromRoute] int postId, [FromQuery] int page)
        {
            IAsyncEnumerable<CommentRead> queryResult = _dbContext.Comments
                .Where(c => c.PostId == postId)
                .Skip((page - 1) * 100)
                .Take(100)
                .Select(c => new CommentRead(c.Id, c.Content, c.CreatedAt, c.LastUpdatedAt, c.PostId, c.UserId, c.ParentCommentId))
                .AsAsyncEnumerable();

            return queryResult;
        }

        [HttpGet]
        public IAsyncEnumerable<CommentSearchResult> GetAsync([FromQuery] CommentSearch commentSearch)
        {
            IQueryable<Comment> query = _dbContext.Comments;

            if (commentSearch.Content is not null)
            {
                query = query.Where(c => c.Content.Contains(commentSearch.Content));
            }

            if (commentSearch.PostId.HasValue)
            {
                query = query.Where(c => c.PostId == commentSearch.PostId);
            }

            if (commentSearch.UserId.HasValue)
            {
                query = query.Where(c => c.UserId == commentSearch.UserId);
            }

            IAsyncEnumerable<CommentSearchResult> queryResult = query
                .Skip((commentSearch.Page - 1) * 100)
                .Take(100)
                .Select(c => new CommentSearchResult(c.Id, c.Content, c.CreatedAt, c.PostId, c.UserId))
                .AsAsyncEnumerable();

            return queryResult;
        }

        [HttpPost]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CreateAsync([FromForm] string content, [FromForm] int postId, [FromForm] int? parentCommentId, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            if (!await _dbContext.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
            {
                return NotFound(new Error("존재하지 않는 게시글입니다"));
            }

            if (parentCommentId.HasValue && !await _dbContext.Comments.AnyAsync(c => c.Id == parentCommentId && c.PostId != postId, cancellationToken))
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            Comment comment = new(content, postId, userId, parentCommentId);
            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetAsync), "Comment", new { comment.Id }, null);
        }

        [HttpPut("{id:int}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromForm] string content, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Comment? comment = await _dbContext.Comments.FindAsync([id], cancellationToken);
            if (comment == null)
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            if (comment.UserId != userId)
            {
                return Forbid();
            }

            comment.Content = content;
            comment.LastUpdatedAt = _timeProvider.GetUtcNow();
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
            Comment? comment = await _dbContext.Comments.FindAsync([id], cancellationToken);
            if (comment == null)
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            if (comment.UserId != userId)
            {
                return Forbid();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(comment, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }
    }
}
