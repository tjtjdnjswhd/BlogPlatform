using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Options;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlogPlatform.Shared.Identity.Services
{
    public class JwtService : IJwtService
    {
        private static readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new();
        private static readonly JsonSerializerOptions _bodyTokenSerializeOption = new(JsonSerializerDefaults.Web);

        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly JwtOptions _jwtOptions;
        private readonly IDistributedCache _cache;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<JwtService> _logger;

        public JwtService(BlogPlatformDbContext blogPlatformDbContext, IOptions<JwtOptions> jwtOptions, IDistributedCache cache, TimeProvider timeProvider, ILogger<JwtService> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _jwtOptions = jwtOptions.Value;
            _cache = cache;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<AuthorizeToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Generating token for {user}", user);

            List<string> roleNames = await _blogPlatformDbContext.Roles
                .Where(r => r.Users.Any(u => u.Id == user.Id))
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found user roles for {user}: {roles}", user, roleNames);

            Claim[] claims = [
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Name, user.Name),
                new(ClaimTypes.Role, string.Join(',', roleNames)),
                new(JwtRegisteredClaimNames.Iss, _jwtOptions.Issuer),
                new(JwtRegisteredClaimNames.Aud, _jwtOptions.Audience)
            ];

            _logger.LogDebug("Created claims for {user}: {claims}", user, claims);

            ClaimsIdentity claimsIdentity = new(claims, _jwtOptions.AuthenticationType, JwtRegisteredClaimNames.Name, ClaimTypes.Role);
            AuthorizeToken token = GenerateToken(claimsIdentity);

            _logger.LogInformation("Generated token: {token}", token);
            return token;
        }

        /// <inheritdoc/>
        public async Task SetCacheTokenAsync(AuthorizeToken token, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Setting cache token: {token}", token);
            await _cache.SetStringAsync(GetRefreshTokenCacheName(token.RefreshToken), token.AccessToken, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _jwtOptions.RefreshTokenExpiration,
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RemoveCachedTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Removing cached token: {refreshToken}", refreshToken);
            await _cache.RemoveAsync(GetRefreshTokenCacheName(refreshToken), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string?> GetCachedTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cache token: {refreshToken}", refreshToken);
            return await _cache.GetStringAsync(GetRefreshTokenCacheName(refreshToken), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AuthorizeToken?> RefreshAsync(AuthorizeToken oldToken, CancellationToken cancellationToken = default)
        {
            string? oldAccessToken = await GetCachedTokenAsync(oldToken.RefreshToken, cancellationToken);
            if (oldAccessToken != oldToken.AccessToken || !_jwtSecurityTokenHandler.CanReadToken(oldAccessToken))
            {
                _logger.LogInformation("Invalid refresh token or access token. access token: {accessToken}, refreshToken: {refreshToken}", oldAccessToken, oldToken.RefreshToken);
                return null;
            }

            ClaimsPrincipal? claimsPrincipal = ValidateOldAccessToken(oldAccessToken);
            if (claimsPrincipal is null)
            {
                return null;
            }

            ClaimsIdentity claimsIdentity = claimsPrincipal.Identity as ClaimsIdentity ?? throw new InvalidOperationException("ClaimsIdentity is null");
            AuthorizeToken newToken = GenerateToken(claimsIdentity);

            await RemoveCachedTokenAsync(oldToken.RefreshToken, cancellationToken);
            return newToken;
        }

        /// <inheritdoc/>
        public void SetCookieToken(HttpResponse response, AuthorizeToken token)
        {
            CookieOptions tokenCookieOptions = new()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = _jwtOptions.AccessTokenExpiration,
            };

            _logger.LogInformation("Setting cookie token for {refreshToken}: {accessToken}", token.RefreshToken, token.AccessToken);
            response.Cookies.Append(_jwtOptions.AccessTokenName, token.AccessToken, tokenCookieOptions);
            response.Cookies.Append(_jwtOptions.RefreshTokenName, token.RefreshToken, tokenCookieOptions);
        }

        /// <inheritdoc/>
        public AuthorizeToken? GetCookieToken(HttpRequest request)
        {
            string? accessToken = request.Cookies[_jwtOptions.AccessTokenName];
            string? refreshToken = request.Cookies[_jwtOptions.RefreshTokenName];

            if (accessToken == null || refreshToken == null)
            {
                _logger.LogInformation("No cookie token found");
                return null;
            }

            AuthorizeToken token = new(accessToken, refreshToken);
            _logger.LogInformation("Found cookie token: {token}", token);
            return token;
        }

        /// <inheritdoc/>
        public AuthorizeToken? RemoveCookieToken(HttpRequest request, HttpResponse response)
        {
            AuthorizeToken? authorizeToken = GetCookieToken(request);
            if (authorizeToken == null)
            {
                return null;
            }

            _logger.LogInformation("Removing cookie token: {token}", authorizeToken);
            response.Cookies.Delete(_jwtOptions.AccessTokenName);
            response.Cookies.Delete(_jwtOptions.RefreshTokenName);
            return authorizeToken;
        }

        /// <inheritdoc/>
        public async Task SetBodyTokenAsync(HttpResponse response, AuthorizeToken token, CancellationToken cancellationToken = default)
        {
            await response.WriteAsync(JsonSerializer.Serialize(token, _bodyTokenSerializeOption), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AuthorizeToken?> GetBodyTokenAsync(HttpRequest request, CancellationToken cancellationToken = default)
        {
            StreamReader reader = new(request.Body);
            string bodyToken = await reader.ReadToEndAsync(cancellationToken);

            try
            {
                AuthorizeToken? authorizeToken = JsonSerializer.Deserialize<AuthorizeToken>(bodyToken, _bodyTokenSerializeOption);
                return authorizeToken;
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Invalid json: {bodyToken}", bodyToken);
                return null;
            }
        }

        /// <inheritdoc/>
        public bool TryGetUserId(ClaimsPrincipal principal, out int userId) => int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);

        private AuthorizeToken GenerateToken(ClaimsIdentity claimsIdentity)
        {
            SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            SigningCredentials signingCredentials = new(securityKey, _jwtOptions.Algorithm);

            JwtSecurityToken securityToken = new(
                _jwtOptions.Issuer,
                _jwtOptions.Audience,
                claimsIdentity.Claims,
                _timeProvider.GetUtcNow().UtcDateTime,
                _timeProvider.GetUtcNow().UtcDateTime.Add(_jwtOptions.AccessTokenExpiration),
                signingCredentials);

            string accessToken = _jwtSecurityTokenHandler.WriteToken(securityToken);

            string refreshToken = Guid.NewGuid().ToString();
            AuthorizeToken token = new(accessToken, refreshToken);
            return token;
        }

        private string GetRefreshTokenCacheName(string refreshToken) => $"{_jwtOptions.RefreshTokenName}_{refreshToken}";

        private ClaimsPrincipal? ValidateOldAccessToken(string accessToken)
        {
            var validationParameter = new TokenValidationParameters
            {
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                // 만료된 토큰일 수 있기 때문에 토큰의 유효기간을 검증하지 않습니다.
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            };

            ClaimsPrincipal principal;
            try
            {
                principal = _jwtSecurityTokenHandler.ValidateToken(accessToken, validationParameter, out SecurityToken _);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Invalid access token: {accessToken}", accessToken);
                return null;
            }

            return principal;
        }
    }
}
