﻿using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.Admin;
using BlogPlatform.Shared.Models.User;
using BlogPlatform.Shared.Services;
using BlogPlatform.Shared.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Swashbuckle.AspNetCore.Annotations;

using System.Linq.Expressions;

namespace BlogPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AdminAuthorize]
    public class AdminController : ControllerBase
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly IIdentityService _identityService;
        private readonly ICascadeSoftDeleteService _softDeleteService;
        private readonly IMailSender _mailSender;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AdminController> _logger;

        public AdminController(BlogPlatformDbContext dbContext, IIdentityService IdentityService, ICascadeSoftDeleteService softDeleteService, IMailSender mailSender, TimeProvider timeProvider, ILogger<AdminController> logger)
        {
            _dbContext = dbContext;
            _identityService = IdentityService;
            _softDeleteService = softDeleteService;
            _mailSender = mailSender;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [HttpPost("send-email")]
        [SwaggerOperation("유저에게 이메일을 보냅니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "이메일 전송 성공")]
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
        [SwaggerOperation("유저를 검색합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "유저 검색 성공", typeof(UserRead))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 유저 없음")]
        public async Task<IActionResult> GetUserAsync([FromQuery] SearchUser search, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Searching for user with {search}", search);

            List<Expression<Func<User, bool>>> filters = [];
            if (search.Id is not null)
            {
                filters.Add(u => u.BasicAccounts.Any(b => b.AccountId == search.Id));
            }
            else if (search.Email is not null)
            {
                filters.Add(u => u.Email == search.Email);
            }
            else if (search.Name is not null)
            {
                filters.Add(u => u.Name == search.Name);
            }

            UserRead? userRead = await _identityService.GetFirstUserReadAsync(search.IsRemoved, filters, cancellationToken);
            if (userRead is null)
            {
                return NotFound();
            }

            return Ok(userRead);
        }

        [HttpDelete("user")]
        [SwaggerOperation("유저를 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "유저 삭제 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 유저 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "유저 삭제 실패")]
        public async Task<IActionResult> DeleteUserAsync([FromBody] EmailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting user with email {email}", model.Email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(user, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [HttpPost("user/restore")]
        [SwaggerOperation("유저를 복원합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "유저 복원 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "유저가 삭제되지 않았습니다")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 유저 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "유저 복원 실패")]
        public async Task<IActionResult> RestoreUserAsync([FromBody] EmailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring user with email {Email}", model.Email);

            User? user = await _dbContext.Users.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            if (user.IsSoftDeletedAtDefault())
            {
                return BadRequest();
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(user, true);
            _logger.LogStatusGeneric(status);

            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [HttpPost("user/ban")]
        [SwaggerOperation("유저를 밴합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "유저 밴 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 유저 없음")]
        public async Task<IActionResult> BanUserAsync([FromBody] UserBanModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Banning user with email {Email}", model.Email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            user.BanExpiresAt = _timeProvider.GetUtcNow().Add(model.BanDuration);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpPost("user/unban")]
        [SwaggerOperation("유저의 밴을 해제합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "유저 밴 해제 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 유저 없음")]
        public async Task<IActionResult> UnbanUserAsync([FromBody] EmailModel model, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Unbanning user with email {Email}", model.Email);

            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            user.BanExpiresAt = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpDelete("post/{id:int}")]
        [SwaggerOperation("해당 Id의 게시글을 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "게시글 삭제 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 게시글 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "게시글 삭제 실패")]
        public async Task<IActionResult> DeletePostAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting post with id {postId}", id);

            Post? post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post is null)
            {
                return NotFound();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [HttpPost("post/{id:int}/restore")]
        [SwaggerOperation("해당 Id의 게시글을 복원합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "게시글 복원 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "삭제되지 않은 게시글")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 게시글 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "게시글 복원 실패")]
        public async Task<IActionResult> RestorePostAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring post with id {postId}", id);

            Post? post = await _dbContext.Posts.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (post is null)
            {
                return NotFound();
            }

            if (post.IsSoftDeletedAtDefault())
            {
                return BadRequest();
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(post, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [HttpDelete("comment/{id:int}")]
        [SwaggerOperation("해당 Id의 댓글을 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "댓글 삭제 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 댓글 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "댓글 삭제 실패")]
        public async Task<IActionResult> DeleteCommentAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting comment with id {commentId}", id);

            Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (comment is null)
            {
                return NotFound();
            }

            var status = await _softDeleteService.SetSoftDeleteAsync(comment, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }

        [HttpPost("comment/{id:int}/restore")]
        [SwaggerOperation("해당 Id의 댓글을 복원합니다")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "댓글 복원 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "삭제되지 않은 댓글")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 댓글 없음")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "댓글 복원 실패")]
        public async Task<IActionResult> RestoreCommentAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Restoring comment with id {commentId}", id);

            Comment? comment = await _dbContext.Comments.IgnoreSoftDeleteFilter().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (comment is null)
            {
                return NotFound();
            }

            if (comment.IsSoftDeletedAtDefault())
            {
                return BadRequest();
            }

            var status = await _softDeleteService.ResetSoftDeleteAsync(comment, true);
            _logger.LogStatusGeneric(status);
            return status.HasErrors ? Problem(status.Message, statusCode: StatusCodes.Status500InternalServerError) : NoContent();
        }
    }
}
