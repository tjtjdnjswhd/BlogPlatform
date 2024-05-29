using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.interfaces;
using BlogPlatform.Api.Services.interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
using System.Security.Claims;

namespace BlogPlatform.Api.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher<BasicAccount> _passwordHasher;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(BlogPlatformDbContext blogPlatformDbContext, IJwtService jwtService, IPasswordHasher<BasicAccount> passwordHasher, ILogger<IdentityService> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<(ELoginResult, User?)> LoginAsync(BasicLoginInfo loginInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Logging in with {loginInfo}", loginInfo);
            var account = await _blogPlatformDbContext.BasicAccounts.Where(b => b.AccountId == loginInfo.Id).Select(b => new { b.PasswordHash, b.User }).FirstOrDefaultAsync(cancellationToken);
            if (account == null)
            {
                _logger.LogInformation("Account not found: {loginInfo}", loginInfo);
                return (ELoginResult.NotFound, null);
            }

            PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(null, account.PasswordHash, loginInfo.Password);
            if (result != PasswordVerificationResult.Success)
            {
                _logger.LogInformation("Password mismatch: {loginInfo}", loginInfo);
                return (ELoginResult.WrongPassword, null);
            }

            Debug.Assert(account.User is not null); // 쿼리 시 User를 포함해야 함
            _logger.LogInformation("Logged in: {loginInfo}", loginInfo);
            return (ELoginResult.Success, account.User);
        }

        /// <inheritdoc/>
        public async Task<(ESignUpResult, User?)> SignUpAsync(BasicSignUpInfo signUpInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Signing up with {signUpInfo}", signUpInfo);
            bool isAccountIdExist = await _blogPlatformDbContext.BasicAccounts.AnyAsync(b => b.AccountId == signUpInfo.Id, cancellationToken);
            if (isAccountIdExist)
            {
                _logger.LogInformation("Account id already exists: {signUpInfo}", signUpInfo);
                return (ESignUpResult.UserIdAlreadyExists, null);
            }

            bool isNameExist = await _blogPlatformDbContext.Users.AnyAsync(u => u.Name == signUpInfo.Name, cancellationToken);
            if (isNameExist)
            {
                _logger.LogInformation("Name already exists: {signUpInfo}", signUpInfo);
                return (ESignUpResult.NameAlreadyExists, null);
            }

            bool isEmailExist = await _blogPlatformDbContext.Users.AnyAsync(u => u.Email == signUpInfo.Email, cancellationToken);
            if (isEmailExist)
            {
                _logger.LogInformation("Email already exists: {signUpInfo}", signUpInfo);
                return (ESignUpResult.EmailAlreadyExists, null);
            }

            string passwordHash = _passwordHasher.HashPassword(null, signUpInfo.Password);
            _logger.LogDebug("passwordHash: {passwordHash}", passwordHash);

            var strategy = _blogPlatformDbContext.Database.CreateExecutionStrategy();
            User user;
            try
            {
                user = await strategy.ExecuteAsync(AddUserAsync, cancellationToken);
                return (ESignUpResult.Success, user);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to sign up: {signUpInfo}", signUpInfo);
                throw;
            }

            async Task<User> AddUserAsync(CancellationToken token)
            {
                using var transaction = await _blogPlatformDbContext.Database.BeginTransactionAsync(token);
                try
                {
                    BasicAccount basicAccount = new(signUpInfo.Id, passwordHash);
                    _blogPlatformDbContext.BasicAccounts.Add(basicAccount);
                    await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

                    User user = new(signUpInfo.Name, signUpInfo.Email, basicAccount.Id);
                    _blogPlatformDbContext.Users.Add(user);
                    await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

                    transaction.Commit();

                    return user;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to sign up: {signUpInfo}", signUpInfo);
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<(ELoginResult, User?)> LoginAsync(OAuthInfo loginInfo, CancellationToken cancellationToken = default)
        {
            User? user = await _blogPlatformDbContext.OAuthAccounts
                .Where(o => o.Provider.Name == loginInfo.Provider && o.NameIdentifier == loginInfo.NameIdentifier)
                .Select(o => o.User)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return (ELoginResult.NotFound, null);
            }

            return (ELoginResult.Success, user);
        }

        /// <inheritdoc/>
        public async Task<(ESignUpResult, User?)> SignUpAsync(OAuthSignUpInfo signUpInfo, CancellationToken cancellationToken = default)
        {
            int providerId = await _blogPlatformDbContext.OAuthProviders
                .Where(p => p.Name == signUpInfo.Provider)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (providerId == 0)
            {
                return (ESignUpResult.ProviderNotFound, null);
            }

            bool isAccountExist = await _blogPlatformDbContext.OAuthAccounts
                .AnyAsync(o => o.Provider.Name == signUpInfo.Provider && o.NameIdentifier == signUpInfo.NameIdentifier, cancellationToken);
            if (isAccountExist)
            {
                return (ESignUpResult.OAuthAlreadyExists, null);
            }

            bool isEmailExist = await _blogPlatformDbContext.Users
                .AnyAsync(u => u.Email == signUpInfo.Email, cancellationToken);
            if (isEmailExist)
            {
                return (ESignUpResult.EmailAlreadyExists, null);
            }

            bool isNameExist = await _blogPlatformDbContext.Users
                .AnyAsync(u => u.Name == signUpInfo.Name, cancellationToken);
            if (isNameExist)
            {
                return (ESignUpResult.NameAlreadyExists, null);
            }

            var strategy = _blogPlatformDbContext.Database.CreateExecutionStrategy();
            User user;
            try
            {
                user = await strategy.ExecuteAsync(AddUserAsync, cancellationToken);
                return (ESignUpResult.Success, user);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to sign up: {signUpInfo}", signUpInfo);
                throw;
            }

            async Task<User> AddUserAsync(CancellationToken token)
            {
                using var transaction = await _blogPlatformDbContext.Database.BeginTransactionAsync(token);
                try
                {
                    User user = new(signUpInfo.Name, signUpInfo.Email, null);
                    _blogPlatformDbContext.Users.Add(user);
                    await _blogPlatformDbContext.SaveChangesAsync(token);

                    OAuthAccount oAuthAccount = new(signUpInfo.NameIdentifier, providerId, user.Id);
                    _blogPlatformDbContext.OAuthAccounts.Add(oAuthAccount);
                    await _blogPlatformDbContext.SaveChangesAsync(token);

                    await transaction.CommitAsync(token);
                    return user;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to sign up: {signUpInfo}", signUpInfo);
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<EAddOAuthResult> AddOAuthAsync(HttpContext httpContext, OAuthInfo oAuthInfo, CancellationToken cancellationToken = default)
        {
            AuthenticateResult authenticateResult = await httpContext.AuthenticateAsync();
            Debug.Assert(authenticateResult.Succeeded); // 인증이 성공해야 함

            if (!_jwtService.TryGetUserId(authenticateResult.Principal, out int userId))
            {
                Debug.Assert(false);
            }

            bool isUserExist = await _blogPlatformDbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!isUserExist)
            {
                return EAddOAuthResult.UserNotFound;
            }

            int providerId = await _blogPlatformDbContext.OAuthProviders
                .Where(p => p.Name == oAuthInfo.Provider)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (providerId == default)
            {
                return EAddOAuthResult.ProviderNotFound;
            }

            bool isUserHasOAuth = await _blogPlatformDbContext.OAuthAccounts.AnyAsync(o => o.UserId == userId && o.ProviderId == providerId, cancellationToken);
            if (isUserHasOAuth)
            {
                return EAddOAuthResult.UserAlreadyHasOAuth;
            }

            bool isOAuthAlreadyExist = await _blogPlatformDbContext.OAuthAccounts.Where(o => o.NameIdentifier == oAuthInfo.NameIdentifier && o.ProviderId == providerId).AnyAsync(cancellationToken);
            if (isOAuthAlreadyExist)
            {
                return EAddOAuthResult.OAuthAlreadyExists;
            }

            OAuthAccount oAuthAccount = new(oAuthInfo.NameIdentifier, providerId, userId);
            _blogPlatformDbContext.OAuthAccounts.Add(oAuthAccount);
            await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

            return EAddOAuthResult.Success;
        }

        /// <inheritdoc/>
        public async Task<ERemoveOAuthResult> RemoveOAuthAsync(ClaimsPrincipal user, string provider, CancellationToken cancellationToken = default)
        {
            if (!_jwtService.TryGetUserId(user, out int userId))
            {
                Debug.Assert(false);
            }

            bool isUserExist = await _blogPlatformDbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!isUserExist)
            {
                return ERemoveOAuthResult.UserNotFound;
            }

            OAuthAccount? oAuthAccount = await _blogPlatformDbContext.OAuthAccounts
                .Where(o => o.UserId == userId && o.Provider.Name == provider)
                .FirstOrDefaultAsync(cancellationToken);

            if (oAuthAccount is null)
            {
                return ERemoveOAuthResult.OAuthNotFound;
            }

            _blogPlatformDbContext.OAuthAccounts.Remove(oAuthAccount);
            await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

            return ERemoveOAuthResult.Success;
        }
    }
}
