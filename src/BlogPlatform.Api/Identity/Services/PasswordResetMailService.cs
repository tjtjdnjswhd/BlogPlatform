using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BlogPlatform.Api.Identity.Services
{
    public class PasswordResetMailService : IPasswordResetMailService
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly IMailSender _mailSender;
        private readonly IPasswordHasher<BasicAccount> _passwordHasher;
        private readonly PasswordResetOptions _passwordResetOptions;
        private readonly ILogger<PasswordResetMailService> _logger;

        public PasswordResetMailService(BlogPlatformDbContext blogPlatformDbContext, IMailSender mailSender, IPasswordHasher<BasicAccount> passwordHasher, IOptions<PasswordResetOptions> options, ILogger<PasswordResetMailService> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _mailSender = mailSender;
            _passwordHasher = passwordHasher;
            _passwordResetOptions = options.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void SendResetPasswordEmail(string email, string newPassword)
        {
            _logger.LogDebug("Sending password reset email for {email}: {password}", email, newPassword);

            _mailSender.Send(_passwordResetOptions.From, email, _passwordResetOptions.Subject, _passwordResetOptions.BodyFactory(newPassword));

            _logger.LogInformation("Password reset email for {email} is sent", email);
        }
    }
}
