using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services.Interfaces;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BlogPlatform.Api.Identity.Services
{
    public class VerifyEmailService : IVerifyEmailService
    {
        private static readonly DistributedCacheEntryOptions CacheExipirationOption = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        };

        private const string VerificationCodePrefix = "EmailVerificationCode";
        private const string VerifiedEmailPrefix = "VerifiedEmail";

        private readonly IMailSender _mailSender;
        private readonly IDistributedCache _cache;
        private readonly VerifyEmailOptions _verifyEmailOptions;
        private readonly ILogger<VerifyEmailService> _logger;

        public VerifyEmailService(IMailSender mailSender, IDistributedCache cache, IOptions<VerifyEmailOptions> verifyEmailOptions, ILogger<VerifyEmailService> logger)
        {
            _mailSender = mailSender;
            _cache = cache;
            _verifyEmailOptions = verifyEmailOptions.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task SendEmailVerificationAsync(string email, CancellationToken cancellationToken = default)
        {
            string code = Random.Shared.Next(0, 99999999).ToString("D8");
            string cacheKey = GetVerificationCodeKey(code);
            await _cache.SetStringAsync(cacheKey, email, CacheExipirationOption, cancellationToken);
            _mailSender.Send(_verifyEmailOptions.From, email, _verifyEmailOptions.Subject, _verifyEmailOptions.BodyFactory(code), CancellationToken.None);

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
                await _cache.SetStringAsync(verifiedEmailKey, string.Empty, CacheExipirationOption, cancellationToken);
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

        private static string GetVerificationCodeKey(string code) => $"{VerificationCodePrefix}_{code}";

        private static string GetVerifiedEmailKey(string email) => $"{VerifiedEmailPrefix}_{email}";
    }
}
