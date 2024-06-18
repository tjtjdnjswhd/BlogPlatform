using BlogPlatform.Api.Identity.Services.Interfaces;

using Microsoft.Extensions.Caching.Distributed;

namespace BlogPlatform.Api.Identity.Services
{
    public class EmailVerifyService : IEmailVerifyService
    {
        private const string VerificationCodePrefix = "EmailVerificationCode";
        private const string VerifiedEmailPrefix = "VerifiedEmail";

        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions;
        private readonly ILogger<EmailVerifyService> _logger;

        public EmailVerifyService(IDistributedCache cache, ILogger<EmailVerifyService> logger)
        {
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            _logger = logger;
        }

        /// <inheritdoc/>
        public string GenerateVerificationCode() => Random.Shared.Next(0, 99999999).ToString("D8");

        /// <inheritdoc/>
        public async Task SetVerifyCodeAsync(string email, string code, CancellationToken cancellationToken = default)
        {
            string cacheKey = GetVerificationCodeKey(code);
            _logger.LogInformation("Setting email verification code {code} for {email}", code, email);
            await _cache.SetStringAsync(cacheKey, email, _cacheOptions, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string?> VerifyEmailCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Verifying email verification code {code}", code);
            string cacheKey = GetVerificationCodeKey(code);
            string? email = await _cache.GetStringAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Email verification code {code} is {status}", code, email is not null ? "valid" : "invalid");

            if (email is not null)
            {
                string verifiedEmailKey = GetVerifiedEmailKey(email);
                await _cache.SetStringAsync(verifiedEmailKey, string.Empty, _cacheOptions, cancellationToken);
                await _cache.RemoveAsync(cacheKey, cancellationToken);

                _logger.LogInformation("Email {email} is verified", email);
            }

            return email;
        }

        /// <inheritdoc/>
        public async Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken = default)
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
