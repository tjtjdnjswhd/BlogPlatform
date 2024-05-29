using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.interfaces;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly IMailSender _mailSender;
        private readonly IPasswordHasher<BasicAccount> _passwordHasher;
        private readonly PasswordResetOptions _passwordResetOptions;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(BlogPlatformDbContext blogPlatformDbContext, IMailSender mailSender, IPasswordHasher<BasicAccount> passwordHasher, IOptions<PasswordResetOptions> options, ILogger<PasswordResetService> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _mailSender = mailSender;
            _passwordHasher = passwordHasher;
            _passwordResetOptions = options.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string?> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            string newPassword = Guid.NewGuid().ToString("N");

            _logger.LogInformation("Resetting password for {email}", email);
            _logger.LogDebug("Resetting password for {email} to {password}", email, newPassword);
            string newPasswordHash = _passwordHasher.HashPassword(null, newPassword);

            int result = await _blogPlatformDbContext.BasicAccounts.Where(a => a.User.Email == email).ExecuteUpdateAsync(set => set.SetProperty(a => a.PasswordHash, newPasswordHash).SetProperty(a => a.IsPasswordChangeRequired, true), cancellationToken);
            Debug.Assert(result > 1);

            return result == 0 ? null : newPassword;
        }

        /// <inheritdoc/>
        public void SendResetPasswordEmail(string email, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending password reset email for {email}: {password}", email, newPassword);

            _mailSender.Send(_passwordResetOptions.From, email, _passwordResetOptions.Subject, _passwordResetOptions.BodyFactory(newPassword), cancellationToken);

            _logger.LogInformation("Password reset email for {email} is sent", email);
        }
    }
}
