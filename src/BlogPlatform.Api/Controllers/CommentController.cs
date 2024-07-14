using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Models.Comment;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation("해당 댓글을 반환합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(CommentRead))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 댓글 없음")]
        public async Task<IActionResult> GetAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            Comment? comment = await _dbContext.Comments.FindAsync([id], cancellationToken);
            if (comment == null)
            {
                return NotFound();
            }

            return Ok(new CommentRead(comment.Id, comment.Content, comment.CreatedAt, comment.LastUpdatedAt, comment.PostId, comment.UserId, comment.ParentCommentId));
        }

        [HttpGet("post/{postId:int}")]
        [SwaggerOperation("해당 게시글의 댓글을 최대 100개까지 반환합니다. 해당 게시글이 없을 경우 빈 결과를 반환합니다.")]
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
        [SwaggerOperation("검색 조건에 맞는 댓글을 최대 100개 검색합니다")]
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
        [SwaggerOperation("댓글을 생성합니다")]
        [SwaggerResponse(StatusCodes.Status201Created, "댓글 생성 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Post not found: 게시글 없음\r\nComment not found: 댓글 없음")]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CreateAsync([FromBody] CommentCreate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Debug.Assert(model.PostId.HasValue ^ model.ParentCommentId.HasValue);

            if (model.PostId.HasValue && !await _dbContext.Posts.AnyAsync(p => p.Id == model.PostId, cancellationToken))
            {
                return Problem(detail: "Post not found", statusCode: StatusCodes.Status404NotFound);
            }

            var commentInfo = model.PostId.HasValue ? new { PostId = model.PostId.Value, Level = 0 } :
                await _dbContext.Comments
                .Where(c => c.ParentCommentId == model.ParentCommentId)
                .Select(c => new { c.PostId, Level = c.Level + 1 })
                .FirstOrDefaultAsync(cancellationToken);
            if (commentInfo is null)
            {
                return Problem(detail: "Comment not found", statusCode: StatusCodes.Status404NotFound);
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
        [SwaggerOperation("해당 댓글을 수정합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "댓글 수정 성공")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "댓글 작성자가 아님")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 댓글 없음")]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CommentUpdate model, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Comment? comment = await _dbContext.Comments.FindAsync([id], cancellationToken);
            if (comment == null)
            {
                return NotFound();
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
        [SwaggerOperation("해당 댓글을 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "댓글 삭제 성공")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "댓글 작성자가 아님")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 댓글 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "댓글 삭제 실패")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            Comment? comment = await _dbContext.Comments.FindAsync([id], cancellationToken);
            if (comment == null)
            {
                return NotFound();
            }

            if (comment.UserId != userId)
            {
                return Forbid();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(comment, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(detail: status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }
    }
}
