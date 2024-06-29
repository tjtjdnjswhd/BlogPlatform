using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AdminAuthorize]
    public class AdminController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly IMailSender _mailSender;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AdminController> _logger;

        public AdminController(BlogPlatformDbContext dbContext, ICascadeSoftDeleteService softDeleteService, IMailSender mailSender, TimeProvider timeProvider, ILogger<AdminController> logger)
        {
            _dbContext = dbContext;
            _softDeleteService = softDeleteService;
            _mailSender = mailSender;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [HttpPost("send-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SendEmails([FromBody] SendMailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending emails to {userIds}", model.UserIds);

            List<string> addresses = model.UserIds is null ? await _dbContext.Users.Select(u => u.Email).ToListAsync(cancellationToken) : await _dbContext.Users.Where(u => model.UserIds.Contains(u.Id)).Select(u => u.Email).ToListAsync(cancellationToken);

            addresses.AsParallel().ForAll(address =>
            {
                MailSendContext context = new("admin", "user", address, model.Subject, model.Body);
                _mailSender.Send(context, cancellationToken);
            });

            return Ok();
        }

        [HttpGet("user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> GetUserAsync([FromQuery] SearchUser search, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Searching for user with {search}", search);

            IQueryable<User> users = search.IsRemoved ? _dbContext.Users.IgnoreSoftDeleteFilter().Where(u => u.SoftDeleteLevel > 0) : _dbContext.Users;
            if (search.Id is not null)
            {
                users = users.Where(u => u.BasicAccounts.Any(b => b.AccountId == search.Id));
            }
            else if (search.Email is not null)
            {
                users = users.Where(u => u.Email == search.Email);
            }
            else if (search.Name is not null)
            {
                users = users.Where(u => u.Name == search.Name);
            }

            UserRead? userRead = await users.Select(u => new UserRead(u.Id, u.BasicAccounts.First().AccountId, u.Name, u.Email, u.CreatedAt, u.Blog.First().Id, u.Roles.Select(r => r.Name), u.OAuthAccounts.Select(o => o.Provider.Name))).FirstOrDefaultAsync(cancellationToken);
            if (userRead is null)
            {
                return NotFound(new Error("존재하지 않는 유저입니다"));
            }

            if (userRead.BlogId is not null)
            {
                userRead.BlogUri = Url.ActionLink("Get", "Blog", new { id = userRead.BlogId });
            }

            return Ok(userRead);
        }

        [HttpDelete("user")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteUserAsync([FromBody] EmailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting user with email {email}", model.Email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound(new Error("존재하지 않는 유저입니다"));
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(user, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("user/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RestoreUserAsync([FromBody] EmailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring user with email {Email}", model.Email);

            User? user = await _dbContext.Users.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound(new Error("존재하지 않는 유저입니다"));
            }

            if (user.IsSoftDeletedAtDefault())
            {
                return BadRequest(new Error("삭제되지 않은 유저입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(user, true);
            _logger.LogStatusGeneric(status);

            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("user/ban")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BanUserAsync([FromBody] UserBanModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Banning user with email {Email}", model.Email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound(new Error("존재하지 않는 유저입니다"));
            }

            user.BanExpiresAt = _timeProvider.GetUtcNow().Add(model.BanDuration);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpPost("user/unban")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> UnbanUserAsync([FromBody] EmailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Unbanning user with email {Email}", model.Email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound(new Error("존재하지 않는 유저입니다"));
            }

            user.BanExpiresAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpDelete("post/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePostAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting post with id {postId}", id);

            Post? post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post is null)
            {
                return NotFound(new Error("존재하지 않는 게시글입니다"));
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("post/{id:int}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestorePostAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring post with id {postId}", id);

            Post? post = await _dbContext.Posts.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post is null)
            {
                return NotFound(new Error("존재하지 않는 게시글입니다"));
            }

            if (post.IsSoftDeletedAtDefault())
            {
                return BadRequest(new Error("삭제되지 않은 게시글입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpDelete("comment/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteCommentAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting comment with id {commentId}", id);

            Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (comment is null)
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(comment, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }

        [HttpPost("comment/{id:int}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RestoreCommentAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring comment with id {commentId}", id);

            Comment? comment = await _dbContext.Comments.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (comment is null)
            {
                return NotFound(new Error("존재하지 않는 댓글입니다"));
            }

            if (comment.IsSoftDeletedAtDefault())
            {
                return BadRequest(new Error("삭제되지 않은 댓글입니다"));
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(comment, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? StatusCode(StatusCodes.Status500InternalServerError, new Error(status.Message)) : NoContent();
        }
    }
}
