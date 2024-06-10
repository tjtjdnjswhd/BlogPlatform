using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SoftDeleteServices.Concrete;

namespace BlogPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AdminAuthorize]
    public class AdminController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly SoftDeleteConfigure _softDeleteConfigure;
        private readonly IMailSender _mailSender;
        private readonly ILogger<AdminController> _logger;

        public AdminController(BlogPlatformDbContext dbContext, SoftDeleteConfigure softDeleteConfigure, IMailSender mailSender, ILogger<AdminController> logger)
        {
            _dbContext = dbContext;
            _softDeleteConfigure = softDeleteConfigure;
            _mailSender = mailSender;
            _logger = logger;
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmails([FromForm] string subject, [FromForm] string body, [FromForm] List<string>? addresses, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending emails to {Addresses}", addresses);

            addresses ??= await _dbContext.Users.Select(u => u.Email).ToListAsync(cancellationToken);
            addresses.AsParallel().ForAll(address =>
            {
                _mailSender.Send("no-reply", address, subject, body, cancellationToken);
            });

            return Ok();
        }

        [HttpGet("user")]
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

            UserRead? userRead = await users.Select(u => new UserRead(u.Id, u.BasicAccounts.First().AccountId, u.Name, u.Email, u.CreatedAt, u.Blog.First().Id)).FirstOrDefaultAsync(cancellationToken);
            if (userRead is null)
            {
                return NotFound();
            }

            if (userRead.BlogId is not null)
            {
                userRead.BlogUri = Url.ActionLink("Get", "Blog", new { id = userRead.BlogId });
            }

            return Ok(userRead);
        }

        [HttpDelete("user")]
        public async Task<IActionResult> DeleteUserAsync([FromForm] string email, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting user with email {email}", email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            var status = await softDelService.SetCascadeSoftDeleteAsync(user);
            _logger.LogSoftDeleteStatus(status);
            return status.HasErrors ? BadRequest(status.Message) : Ok();
        }

        [HttpPost("user/restore")]
        public async Task<IActionResult> RestoreUserAsync([FromForm] string email, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring user with email {Email}", email);

            User? user = await _dbContext.Users.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            var status = await softDelService.ResetCascadeSoftDeleteAsync(user);
            _logger.LogSoftDeleteStatus(status);
            return status.HasErrors ? BadRequest(status.Message) : Ok();
        }

        [HttpPost("user/ban")]
        public async Task<IActionResult> BanUserAsync([FromForm] string email, [FromForm] TimeSpan banDuration, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Banning user with email {Email}", email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            user.BanExpiresAt = DateTimeOffset.UtcNow.Add(banDuration);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpPost("user/unban")]
        public async Task<IActionResult> UnbanUserAsync([FromForm] string email, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Unbanning user with email {Email}", email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            user.BanExpiresAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpDelete("post/{id:int}")]
        public async Task<IActionResult> DeletePostAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting post with id {postId}", id);

            Post? post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post is null)
            {
                return NotFound();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            var status = await softDelService.SetCascadeSoftDeleteAsync(post);
            _logger.LogSoftDeleteStatus(status);
            return status.HasErrors ? BadRequest(status.Message) : Ok();
        }

        [HttpPost("post/{id:int}/restore")]
        public async Task<IActionResult> RestorePostAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring post with id {postId}", id);

            Post? post = await _dbContext.Posts.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post is null)
            {
                return NotFound();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            var status = await softDelService.ResetCascadeSoftDeleteAsync(post);
            _logger.LogSoftDeleteStatus(status);
            return status.HasErrors ? BadRequest(status.Message) : Ok();
        }

        [HttpDelete("comment/{id:int}")]
        public async Task<IActionResult> DeleteCommentAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting comment with id {commentId}", id);

            Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (comment is null)
            {
                return NotFound();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            var status = await softDelService.SetCascadeSoftDeleteAsync(comment);
            _logger.LogSoftDeleteStatus(status);
            return status.HasErrors ? BadRequest(status.Message) : Ok();
        }

        [HttpPost("comment/{id:int}/restore")]
        public async Task<IActionResult> RestoreCommentAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring comment with id {commentId}", id);

            Comment? comment = await _dbContext.Comments.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (comment is null)
            {
                return NotFound();
            }

            CascadeSoftDelServiceAsync<EntityBase> softDelService = new(_softDeleteConfigure);
            var status = await softDelService.ResetCascadeSoftDeleteAsync(comment);
            _logger.LogSoftDeleteStatus(status);
            return status.HasErrors ? BadRequest(status.Message) : Ok();
        }
    }
}
