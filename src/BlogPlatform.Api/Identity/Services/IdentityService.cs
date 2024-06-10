using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using SoftDeleteServices.Concrete;

using System.Diagnostics;
using System.Security.Claims;

namespace BlogPlatform.Api.Services
{
    public class IdentityService : IIdentityService
    {
        private static readonly TimeSpan UserRestoreDuration = TimeSpan.FromDays(1);

        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher<BasicAccount> _passwordHasher;
        private readonly CascadeSoftDelService<EntityBase> _softDeleteService;
        private readonly IAuthenticationService _authenticationService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(BlogPlatformDbContext blogPlatformDbContext, IJwtService jwtService, IPasswordHasher<BasicAccount> passwordHasher, IAuthenticationService authenticationService, TimeProvider timeProvider, ILogger<IdentityService> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _authenticationService = authenticationService;
            _logger = logger;
            _timeProvider = timeProvider;
            _authenticationService = authenticationService;
            SoftDeleteConfigure softDeleteConf = new(_blogPlatformDbContext);
            _softDeleteService = new(softDeleteConf);
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

            IQueryable<User> users = _blogPlatformDbContext.Users.Union(GetRestorableUsers());
            IQueryable<BasicAccount> accounts = users.SelectMany(u => u.BasicAccounts);

            bool isAccountIdExist = await accounts.AnyAsync(b => b.AccountId == signUpInfo.Id, cancellationToken);
            if (isAccountIdExist)
            {
                _logger.LogInformation("Account id already exists: {signUpInfo}", signUpInfo);
                return (ESignUpResult.UserIdAlreadyExists, null);
            }

            bool isNameExist = await users.AnyAsync(u => u.Name == signUpInfo.Name, cancellationToken);
            if (isNameExist)
            {
                _logger.LogInformation("Name already exists: {signUpInfo}", signUpInfo);
                return (ESignUpResult.NameAlreadyExists, null);
            }

            bool isEmailExist = await users.AnyAsync(u => u.Email == signUpInfo.Email, cancellationToken);
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
                    User user = new(signUpInfo.Name, signUpInfo.Email);
                    _blogPlatformDbContext.Users.Add(user);
                    await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

                    BasicAccount basicAccount = new(signUpInfo.Id, passwordHash, user.Id);
                    _blogPlatformDbContext.BasicAccounts.Add(basicAccount);
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
        public async Task<(ELoginResult, User?)> LoginAsync(OAuthLoginInfo loginInfo, CancellationToken cancellationToken = default)
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

            IQueryable<User> users = _blogPlatformDbContext.Users.Union(GetRestorableUsers());
            IQueryable<OAuthAccount> oAuthAccounts = users.SelectMany(u => u.OAuthAccounts);

            bool isAccountExist = await oAuthAccounts.AnyAsync(o => o.Provider.Name == signUpInfo.Provider && o.NameIdentifier == signUpInfo.NameIdentifier, cancellationToken);
            if (isAccountExist)
            {
                return (ESignUpResult.OAuthAlreadyExists, null);
            }

            bool isEmailExist = await users.AnyAsync(u => u.Email == signUpInfo.Email, cancellationToken);
            if (isEmailExist)
            {
                return (ESignUpResult.EmailAlreadyExists, null);
            }

            bool isNameExist = await users.AnyAsync(u => u.Name == signUpInfo.Name, cancellationToken);
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
                    User user = new(signUpInfo.Name, signUpInfo.Email);
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
        public async Task<EAddOAuthResult> AddOAuthAsync(HttpContext httpContext, OAuthLoginInfo oAuthInfo, CancellationToken cancellationToken = default)
        {
            AuthenticateResult authenticateResult = await _authenticationService.AuthenticateAsync(httpContext, null);
            Debug.Assert(authenticateResult.Succeeded); // 인증이 성공해야 함

            if (!TryGetUserId(authenticateResult.Principal, out int userId))
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

            IQueryable<User> users = _blogPlatformDbContext.Users.Union(GetRestorableUsers());
            IQueryable<OAuthAccount> oAuthAccounts = users.SelectMany(u => u.OAuthAccounts);

            bool isOAuthAlreadyExist = await oAuthAccounts.Where(o => o.NameIdentifier == oAuthInfo.NameIdentifier && o.ProviderId == providerId).AnyAsync(cancellationToken);
            if (isOAuthAlreadyExist)
            {
                return EAddOAuthResult.OAuthAlreadyExists;
            }

            bool isUserHasOAuth = await oAuthAccounts.AnyAsync(o => o.UserId == userId && o.ProviderId == providerId, cancellationToken);
            if (isUserHasOAuth)
            {
                return EAddOAuthResult.UserAlreadyHasOAuth;
            }

            OAuthAccount oAuthAccount = new(oAuthInfo.NameIdentifier, providerId, userId);
            _blogPlatformDbContext.OAuthAccounts.Add(oAuthAccount);
            await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

            return EAddOAuthResult.Success;
        }

        /// <inheritdoc/>
        public async Task<ERemoveOAuthResult> RemoveOAuthAsync(ClaimsPrincipal user, string provider, CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(user, out int userId))
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

            int accountCount = await _blogPlatformDbContext.Users.Where(u => u.Id == userId).Select(u => u.BasicAccounts.Count + u.OAuthAccounts.Count).FirstOrDefaultAsync(cancellationToken);
            if (accountCount == 1)
            {
                return ERemoveOAuthResult.HasSingleAccount;
            }

            var status = _softDeleteService.SetCascadeSoftDelete(oAuthAccount, false);
            _logger.LogSoftDeleteStatus(status);

            await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);

            return ERemoveOAuthResult.Success;
        }

        /// <inheritdoc/>
        public async Task<bool> ChangePasswordAsync(ClaimsPrincipal user, string newPassword, CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(user, out int userId))
            {
                Debug.Assert(false);
            }

            _logger.LogDebug("Changing password. user id: {userId}, new password: {newPassword}", userId, newPassword);
            string newPasswordHash = _passwordHasher.HashPassword(null, newPassword);

            int result = await _blogPlatformDbContext.BasicAccounts.Where(b => b.User.Id == userId).ExecuteUpdateAsync(set => set.SetProperty(b => b.PasswordHash, newPasswordHash).SetProperty(b => b.IsPasswordChangeRequired, false), cancellationToken);
            Debug.Assert(result <= 1);

            _logger.LogInformation("Password changed. user id: {userId}", userId);
            return result == 1;
        }

        /// <inheritdoc/>
        public async Task<bool> ChangeNameAsync(ClaimsPrincipal user, string newName, CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(user, out int userId))
            {
                Debug.Assert(false);
            }

            _logger.LogDebug("Changing user name. user id: {userId}. newName: {newName}", userId, newName);

            if (await _blogPlatformDbContext.Users.AnyAsync(u => u.Name == newName, cancellationToken))
            {
                return false;
            }

            int result = await _blogPlatformDbContext.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(set => set.SetProperty(u => u.Name, newName), cancellationToken);
            Debug.Assert(result <= 1);

            return result != 0;
        }

        /// <inheritdoc/>
        public async Task<string?> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            string newPassword = Guid.NewGuid().ToString("N");

            _logger.LogInformation("Resetting password for {email}", email);
            _logger.LogDebug("Resetting password for {email} to {password}", email, newPassword);
            string newPasswordHash = _passwordHasher.HashPassword(null, newPassword);

            int result = await _blogPlatformDbContext.BasicAccounts.Where(a => a.User.Email == email).ExecuteUpdateAsync(set => set.SetProperty(a => a.PasswordHash, newPasswordHash).SetProperty(a => a.IsPasswordChangeRequired, true), cancellationToken);
            Debug.Assert(result <= 1);

            return result == 0 ? null : newPassword;
        }

        /// <inheritdoc/>
        public async Task<string?> FindAccountIdAsync(string email, CancellationToken cancellationToken = default)
        {
            string? accountId = await _blogPlatformDbContext.BasicAccounts
                .Where(b => b.User.Email == email)
                .Select(b => b.AccountId)
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogDebug("Finding account id for {email}. Account id: {accountId}", email, accountId);

            return accountId;
        }

        /// <inheritdoc/>
        public async Task<bool> WithDrawAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(user, out int userId))
            {
                Debug.Assert(false);
            }

            _logger.LogInformation("Withdrawing user. user id: {userId}", userId);

            User? userData = await _blogPlatformDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (userData is null)
            {
                return false;
            }

            var status = _softDeleteService.SetCascadeSoftDelete(userData, false);
            _logger.LogSoftDeleteStatus(status);
            Debug.Assert(status.IsValid);
            await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <inheritdoc/>
        public async Task<ECancelWithDrawResult> CancelWithDrawAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(user, out int userId))
            {
                Debug.Assert(false);
            }

            _logger.LogInformation("Canceling withdrawal. user id: {userId}", userId);

            User? userData = _blogPlatformDbContext.Users.IgnoreSoftDeleteFilter().FirstOrDefault(u => u.Id == userId);
            if (userData is null)
            {
                return ECancelWithDrawResult.UserNotFound;
            }

            if (userData.SoftDeleteLevel == 0)
            {
                return ECancelWithDrawResult.WithDrawNotRequested;
            }

            if (userData.SoftDeletedAt.Add(UserRestoreDuration) < _timeProvider.GetUtcNow())
            {
                return ECancelWithDrawResult.Expired;
            }

            var status = _softDeleteService.ResetCascadeSoftDelete(userData, false);
            _logger.LogSoftDeleteStatus(status);
            Debug.Assert(status.IsValid);

            await _blogPlatformDbContext.SaveChangesAsync(cancellationToken);
            return ECancelWithDrawResult.Success;
        }

        /// <inheritdoc/>
        public async Task<bool> ChangeEmailAsync(ClaimsPrincipal user, string newEmail, CancellationToken cancellationToken = default)
        {
            if (!TryGetUserId(user, out int userId))
            {
                Debug.Assert(false);
            }

            if (await _blogPlatformDbContext.Users.AnyAsync(u => u.Email == newEmail, cancellationToken))
            {
                return false;
            }

            _logger.LogInformation("Changing email. user id: {userId}, new email: {newEmail}", userId, newEmail);

            int result = await _blogPlatformDbContext.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(set => set.SetProperty(u => u.Email, newEmail), cancellationToken);
            Debug.Assert(result <= 1);

            return result != 0;
        }

        public bool TryGetUserId(ClaimsPrincipal user, out int userId)
        {
            return _jwtService.TryGetUserId(user, out userId);
        }

        private IQueryable<User> GetRestorableUsers()
        {
            return _blogPlatformDbContext.Users.FilterBySoftDeletedAt(_timeProvider.GetUtcNow(), UserRestoreDuration);
        }
    }
}