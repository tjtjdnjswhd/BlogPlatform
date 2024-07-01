using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;

using Microsoft.Extensions.Options;

namespace BlogPlatform.Api.Identity.Services
{
    public class UserEmailService : IUserEmailService
    {
        private readonly IMailSender _mailSender;
        private readonly UserEmailOptions _options;
        private readonly ILogger<UserEmailService> _logger;

        public UserEmailService(IMailSender mailSender, IOptions<UserEmailOptions> options, ILogger<UserEmailService> logger)
        {
            _mailSender = mailSender;
            _options = options.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void SendPasswordResetMail(string email, string newPassword, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending password reset email for {email}: {password}", email, newPassword);
            MailSendContext context = new(null, "user", email, _options.PasswordResetSubject, string.Format(_options.PasswordResetSubject, newPassword));
            _mailSender.Send(context, cancellationToken);
            _logger.LogInformation("Password reset email for {email} is sent", email);
        }

        /// <inheritdoc/>
        public void SendAccountIdMail(string email, string accountId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Email for account ID {accountId} is sent to {email}", accountId, email);
            MailSendContext context = new(null, "user", email, _options.AccountIdSubject, string.Format(_options.AccountIdBody, accountId));
            _mailSender.Send(context, cancellationToken);
            _logger.LogInformation("Account ID email for {email} is sent", email);
        }

        /// <inheritdoc/>
        public void SendEmailVerifyMail(string email, string uri, CancellationToken cancellationToken = default)
        {
            MailSendContext context = new(null, "user", email, _options.EmailVerifySubject, string.Format(_options.EmailVerifyBody, uri));
            _logger.LogInformation("Sending email verification code for {email}", email);
            _mailSender.Send(context, cancellationToken);
        }
    }
}
