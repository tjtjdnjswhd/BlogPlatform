using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BlogPlatform.Shared.Identity.Services
{
    public class EmailVerifyService : IEmailVerifyService
    {
        /// <summary>
        /// {0}: code value: email
        /// </summary>
        private const string SignUpVerifyCodeFormat = "SignUpVerifyCode_{0}";

        /// <summary>
        /// {0}: email value: empty
        /// </summary>
        private const string SignUpVerifiedEmailFormat = "SignUpVerifiedEmail_{0}";

        /// <summary>
        /// {0}: userId {1}: code value: email
        /// </summary>
        private const string ChangeVerifyCodePrefix = "ChangeVerifyCode_{0}_{1}";

        /// <summary>
        /// {0}: userId value: email
        /// </summary>
        private const string ChangeVerifiedEmailPrefix = "ChangeVerifiedEmail_{0}";

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
        public async Task SetSignUpVerifyCodeAsync(string email, string code, CancellationToken cancellationToken = default)
        {
            string cacheKey = GetSignUpVerifyCodeKey(code);
            _logger.LogInformation("Setting signup email verification code {code} for {email}", code, email);
            await _cache.SetStringAsync(cacheKey, email, _cacheOptions, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string?> VerifySignUpEmailCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Verifying signup email verification code {code}", code);
            string cacheKey = GetSignUpVerifyCodeKey(code);
            string? email = await _cache.GetStringAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Email verification code {code} is {status}", code, email is not null ? "valid" : "invalid");

            if (email is not null)
            {
                string verifiedEmailKey = GetSignUpVerifiedEmailKey(email);
                await _cache.SetStringAsync(verifiedEmailKey, string.Empty, _cacheOptions, cancellationToken);
                await _cache.RemoveAsync(cacheKey, cancellationToken);

                _logger.LogInformation("Email {email} is verified to sign up", email);
            }

            return email;
        }

        /// <inheritdoc/>
        public async Task<bool> IsSignUpEmailVerifiedAsync(string email, CancellationToken cancellationToken = default)
        {
            string verifiedEmailKey = GetSignUpVerifiedEmailKey(email);
            bool isExist = await _cache.GetStringAsync(verifiedEmailKey, cancellationToken) is not null;

            _logger.LogInformation("Email {email} is {status}", email, isExist ? "verified" : "not verified");
            return isExist;
        }

        public async Task SetChangeVerifyCodeAsync(int userId, string email, string code, CancellationToken cancellationToken = default)
        {
            string cacheKey = GetChangeVerifyCodeKey(userId, code);
            _logger.LogInformation("Setting change email verification code {code} for {email}", code, email);
            await _cache.SetStringAsync(cacheKey, email, _cacheOptions, cancellationToken);
        }

        public async Task<string?> VerifyChangeEmailCodeAsync(int userId, string code, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Verifying change email verification code {code} for user {userid}", code, userId);
            string cacheKey = GetChangeVerifyCodeKey(userId, code);
            string? storedEmail = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (storedEmail == null)
            {
                return null;
            }

            string verifiedEmailKey = GetChangeVerifiedEmailKey(userId);
            await _cache.SetStringAsync(verifiedEmailKey, storedEmail, _cacheOptions, cancellationToken);
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Change email verification code {code} for user {userid} is valid", code, userId);
            _logger.LogInformation("Email {email} is verified to change", storedEmail);
            return storedEmail;
        }

        private static string GetSignUpVerifyCodeKey(string code) => string.Format(SignUpVerifyCodeFormat, code);

        private static string GetSignUpVerifiedEmailKey(string email) => string.Format(SignUpVerifiedEmailFormat, email);

        private static string GetChangeVerifyCodeKey(int userId, string code) => string.Format(ChangeVerifyCodePrefix, userId, code);

        private static string GetChangeVerifiedEmailKey(int userId) => string.Format(ChangeVerifiedEmailPrefix, userId);
    }
}
