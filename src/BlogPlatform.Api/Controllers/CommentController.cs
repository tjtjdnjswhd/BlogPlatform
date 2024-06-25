using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

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
        public IAsyncEnumerable<CommentRead> GetByPost([FromRoute] int postId, [FromQuery, Range(1, int.MaxValue)] int page = 1)
        {
            IAsyncEnumerable<CommentRead> queryResult = _dbContext.Comments
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.Id)
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

            query = (commentSearch.OrderBy, commentSearch.OrderDirection) switch
            {
                (ECommentSearchOrderBy.CreatedAt, ListSortDirection.Ascending) => query.OrderBy(c => c.CreatedAt),
                (ECommentSearchOrderBy.CreatedAt, ListSortDirection.Descending) => query.OrderByDescending(c => c.CreatedAt),
                (ECommentSearchOrderBy.UpdatedAt, ListSortDirection.Ascending) => query.OrderBy(c => c.LastUpdatedAt),
                (ECommentSearchOrderBy.UpdatedAt, ListSortDirection.Descending) => query.OrderByDescending(c => c.LastUpdatedAt),
                (ECommentSearchOrderBy.Content, ListSortDirection.Ascending) => query.OrderBy(c => c.Content),
                (ECommentSearchOrderBy.Content, ListSortDirection.Descending) => query.OrderByDescending(c => c.Content),
                (ECommentSearchOrderBy.Post, ListSortDirection.Ascending) => query.OrderBy(c => c.PostId),
                (ECommentSearchOrderBy.Post, ListSortDirection.Descending) => query.OrderByDescending(c => c.PostId),
                (ECommentSearchOrderBy.User, ListSortDirection.Ascending) => query.OrderBy(c => c.UserId),
                (ECommentSearchOrderBy.User, ListSortDirection.Descending) => query.OrderByDescending(c => c.UserId),
                _ => throw new InvalidEnumArgumentException(nameof(commentSearch.OrderBy), (int)commentSearch.OrderBy, typeof(ECommentSearchOrderBy))
            };

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
        public async Task<IActionResult> CreateAsync([FromBody] CommentCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Debug.Assert(model.PostId.HasValue ^ model.ParentCommentId.HasValue);

            if (model.PostId.HasValue && !await _dbContext.Posts.AnyAsync(p => p.Id == model.PostId, cancellationToken))
            {
                return NotFound(new Error("존재하지 않는 게시글입니다"));
            }

            var commentInfo = model.PostId.HasValue ? new { PostId = model.PostId.Value, Level = 0 } :
                await _dbContext.Comments
                .Where(c => c.ParentCommentId == model.ParentCommentId)
                .Select(c => new { c.PostId, Level = c.Level + 1 })
                .FirstOrDefaultAsync(cancellationToken);
            if (commentInfo is null)
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            Comment comment = new(model.Content, commentInfo.PostId, userId, model.ParentCommentId)
            {
                Level = commentInfo.Level
            };

            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction("Get", "Comment", new { comment.Id }, null);
        }

        [HttpPut("{id:int}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CommentUpdate model, [UserIdBind] int userId, CancellationToken cancellationToken)
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

            comment.Content = model.Content;
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
