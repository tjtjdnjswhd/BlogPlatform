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
        private readonly ILogger<CommentController> _logger;

        public CommentController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, ILogger<CommentController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync([FromRoute] int id)
        {
            Comment? comment = await _dbContext.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            return Ok(new CommentRead(comment.Id, comment.Content, comment.CreatedAt, comment.LastUpdatedAt, comment.PostId, comment.UserId, comment.ParentCommentId));
        }

        [HttpGet("post/{postId:int}")]
        public IAsyncEnumerable<CommentRead> GetByPostAsync([FromRoute] int postId, [FromQuery] int page)
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
            if (!string.IsNullOrWhiteSpace(commentSearch.Content))
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
        public async Task<IActionResult> CreateAsync([FromForm] string content, [FromForm] int postId, [FromForm] int? parentCommentId, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            if (await _dbContext.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
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
            comment.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpDelete("{id:int}")]
        [UserAuthorize]
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
            return status.HasErrors ? BadRequest(status.Message) : NoContent();
        }
    }
}
