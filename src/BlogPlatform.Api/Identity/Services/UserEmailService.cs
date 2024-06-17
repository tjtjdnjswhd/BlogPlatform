using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BlogPlatform.Api.Identity.Services
{
    public class UserEmailService : IUserEmailService
    {
        internal const string VerificationCodePrefix = "EmailVerificationCode";
        internal const string VerifiedEmailPrefix = "VerifiedEmail";

        private readonly IMailSender _mailSender;
        private readonly IDistributedCache _cache;
        private readonly UserEmailOptions _options;
        private readonly ILogger<UserEmailService> _logger;
        private readonly DistributedCacheEntryOptions _verifyExpiration;

        public UserEmailService(IMailSender mailSender, IDistributedCache cache, IOptions<UserEmailOptions> options, ILogger<UserEmailService> logger)
        {
            _mailSender = mailSender;
            _cache = cache;
            _options = options.Value;
            _logger = logger;

            _verifyExpiration = new()
            {
                AbsoluteExpirationRelativeToNow = options.Value.EmailVerifyExpiration,
            };
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
        public async Task SendEmailVerificationAsync(string email, Func<string, string> verifyUriFunc, CancellationToken cancellationToken = default)
        {
            string code = Random.Shared.Next(0, 99999999).ToString("D8");
            string cacheKey = GetVerificationCodeKey(code);
            _logger.LogDebug("Sending email verification code {code} to {email}", code, email);
            await _cache.SetStringAsync(cacheKey, email, _verifyExpiration, cancellationToken);

            string uri = verifyUriFunc(code);
            MailSendContext context = new(null, "user", email, _options.EmailVerifySubject, string.Format(_options.EmailVerifyBody, uri));
            _mailSender.Send(context, cancellationToken);

            _logger.LogInformation("Email verification code {code} for {email} is sent", code, email);
        }

        /// <inheritdoc/>
        public async Task<string?> VerifyEmailCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            string cacheKey = GetVerificationCodeKey(code);
            string? email = await _cache.GetStringAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Email verification code {code} is {status}", code, email is not null ? "valid" : "invalid");

            if (email is not null)
            {
                string verifiedEmailKey = GetVerifiedEmailKey(email);
                await _cache.SetStringAsync(verifiedEmailKey, string.Empty, _verifyExpiration, cancellationToken);
                await _cache.RemoveAsync(cacheKey, cancellationToken);

                _logger.LogInformation("Email {email} is verified", email);
            }

            return email;
        }

        /// <inheritdoc/>
        public async Task<bool> IsVerifyAsync(string email, CancellationToken cancellationToken = default)
        {
            string verifiedEmailKey = GetVerifiedEmailKey(email);
            bool isExist = (await _cache.GetStringAsync(verifiedEmailKey, cancellationToken)) is not null;

            _logger.LogInformation("Email {email} is {status}", email, isExist ? "verified" : "not verified");
            return isExist;
        }

        internal static string GetVerificationCodeKey(string code) => $"{VerificationCodePrefix}_{code}";

        internal static string GetVerifiedEmailKey(string email) => $"{VerifiedEmailPrefix}_{email}";
    }
}
