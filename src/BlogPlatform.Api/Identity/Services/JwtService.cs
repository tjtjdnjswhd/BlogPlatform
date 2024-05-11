using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Services.interfaces;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlogPlatform.Api.Services
{
    public class JwtService : IJwtService
    {
        private static readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new();

        private readonly JwtOptions _jwtOptions;
        private readonly IDistributedCache _cache;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IOptions<JwtOptions> jwtOptions, IDistributedCache cache, ILogger<JwtService> logger)
        {
            _jwtOptions = jwtOptions.Value;
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public AuthorizeToken GenerateToken(ClaimsIdentity claimsIdentity)
        {
            _logger.LogDebug("Generating token for {claimsIdentity}", claimsIdentity);

            SecurityKey securityKey = _jwtOptions.SecurityKeyFunc(_jwtOptions.SecretKey);
            SigningCredentials signingCredentials = new(securityKey, _jwtOptions.Algorithm);

            string accessToken = _jwtSecurityTokenHandler.CreateJwtSecurityToken(
                _jwtOptions.Issuer,
                _jwtOptions.Audience,
                claimsIdentity,
                DateTime.UtcNow,
                DateTime.UtcNow.Add(_jwtOptions.AccessTokenExpiration),
                DateTime.UtcNow,
                signingCredentials).ToString();

            string refreshToken = Guid.NewGuid().ToString();
            AuthorizeToken token = new(accessToken, refreshToken);

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

        private string GetRefreshTokenCacheName(string refreshToken) => $"{_jwtOptions.RefreshTokenName}_{refreshToken}";

        private ClaimsPrincipal? ValidateOldAccessToken(string accessToken)
        {
            var validationParameter = new TokenValidationParameters
            {
                ValidIssuer = _jwtOptions.Issuer,
                ValidAudience = _jwtOptions.Audience,
                // 만료된 토큰일 수 있기 때문에 토큰의 유효기간을 검증하지 않습니다.
                ValidateLifetime = false,
                TokenDecryptionKey = _jwtOptions.SecurityKeyFunc(_jwtOptions.SecretKey),
            };

            ClaimsPrincipal principal;
            try
            {
                principal = _jwtSecurityTokenHandler.ValidateToken(accessToken, validationParameter, out SecurityToken _);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Invalid access token: {accessToken}", accessToken);
                return null;
            }

            return principal;
        }
    }
}
