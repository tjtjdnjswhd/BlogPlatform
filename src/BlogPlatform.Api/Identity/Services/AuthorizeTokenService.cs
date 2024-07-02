using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Identity.Services.Options;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlogPlatform.Api.Identity.Services
{
    public class AuthorizeTokenService : IAuthorizeTokenService
    {
        private const string CacheKeyPrefix = "RefreshToken_";

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new(JsonSerializerDefaults.Web);

        private static readonly DistributedCacheEntryOptions DefaultCacheOptions = new();

        private static readonly CookieOptions DefaultCookieOptions = new()
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
        };

        private readonly JsonWebTokenHandler _tokenHandler;
        private readonly IDistributedCache _cache;
        private readonly TimeProvider _timeProvider;
        private readonly AuthorizeTokenOptions _jwtOptions;
        private readonly ILogger<AuthorizeTokenService> _logger;

        public AuthorizeTokenService(JsonWebTokenHandler tokenHandler, IDistributedCache cache, TimeProvider timeProvider, IOptions<AuthorizeTokenOptions> jwtOptions, ILogger<AuthorizeTokenService> logger)
        {
            _tokenHandler = tokenHandler;
            _cache = cache;
            _timeProvider = timeProvider;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public AuthorizeToken GenerateToken(ClaimsPrincipal user, bool setCookie)
        {
            ClaimsIdentity claimsIdentity = new(user.Identity);
            claimsIdentity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, setCookie ? JwtClaimValues.AuthenticationMethodCookie : JwtClaimValues.AuthenticationMethodBearer));

            SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            SigningCredentials signingCredentials = new(securityKey, _jwtOptions.Algorithm);

            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
            SecurityTokenDescriptor securityTokenDescriptor = new()
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                NotBefore = now,
                Expires = now.Add(_jwtOptions.AccessTokenExpiration),
                SigningCredentials = signingCredentials,
                Subject = claimsIdentity
            };

            string accessToken = _tokenHandler.CreateToken(securityTokenDescriptor);
            string refreshToken = GenerateRefreshToken();
            AuthorizeToken authorizeToken = new(accessToken, refreshToken);
            _logger.LogInformation("Authorize token generated: {token}", authorizeToken);

            return authorizeToken;
        }

        public async Task WriteAsync(HttpResponse response, AuthorizeToken token, bool setCookie, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Writing token to {dest}. token: {token}", setCookie ? "Cookie" : "Body", token);

            if (setCookie)
            {
                CookieOptions cookieOptions = new(DefaultCookieOptions) { Expires = _timeProvider.GetUtcNow().UtcDateTime.Add(_jwtOptions.RefreshTokenExpiration) };
                response.Cookies.Append(_jwtOptions.AccessTokenName, token.AccessToken, cookieOptions);
                response.Cookies.Append(_jwtOptions.RefreshTokenName, token.RefreshToken, cookieOptions);
            }
            else
            {
                await response.WriteAsJsonAsync(token, cancellationToken);
            }
        }

        public async Task<AuthorizeToken?> GetAsync(HttpRequest request, bool fromCookie, CancellationToken cancellationToken = default)
        {
            if (fromCookie)
            {
                string? accessToken = request.Cookies[_jwtOptions.AccessTokenName];
                string? refreshToken = request.Cookies[_jwtOptions.RefreshTokenName];
                if (accessToken is not null && refreshToken is not null)
                {
                    _logger.LogInformation("Token found in cookies. accessToken: {accessToken}, refreshToken: {refreshToken}", accessToken, refreshToken);
                    return new AuthorizeToken(accessToken, refreshToken);
                }
                else
                {
                    _logger.LogInformation("No token found in cookies");
                    return null;
                }
            }
            else
            {
                try
                {
                    AuthorizeToken? authorizeToken = await JsonSerializer.DeserializeAsync<AuthorizeToken>(request.Body, DefaultJsonSerializerOptions, cancellationToken);
                    if (authorizeToken is null)
                    {
                        _logger.LogInformation("No token found in request body");
                        return null;
                    }

                    _logger.LogInformation("Token found in request body. accessToken: {accessToken}, refreshToken: {refreshToken}", authorizeToken.AccessToken, authorizeToken.RefreshToken);
                    return authorizeToken;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to deserialize token from request body");
                    return null;
                }
            }
        }

        public async Task CacheTokenAsync(AuthorizeToken token, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Adding token to cache: {token}", token);
            // 요청마다 생성 대신 속성만 갱신해 재사용
            DefaultCacheOptions.AbsoluteExpirationRelativeToNow = _jwtOptions.RefreshTokenExpiration;
            await _cache.SetStringAsync($"{CacheKeyPrefix}{token.RefreshToken}", token.AccessToken, DefaultCacheOptions, cancellationToken);
        }

        public async Task<string?> GetCachedTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            string? accessToken = await _cache.GetStringAsync($"{CacheKeyPrefix}{refreshToken}", cancellationToken);
            if (accessToken is not null)
            {
                _logger.LogDebug("Token found in cache: {accessToken}", accessToken);
            }
            else
            {
                _logger.LogDebug("No token found in cache");
            }

            return accessToken;
        }

        public async Task RemoveTokenAsync(HttpResponse response, string? refreshToken, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Removing token: {refresh}", refreshToken);

            response.Cookies.Delete(_jwtOptions.RefreshTokenName);
            response.Cookies.Delete(_jwtOptions.AccessTokenName);

            if (refreshToken is not null)
            {
                await _cache.RemoveAsync($"{CacheKeyPrefix}{refreshToken}", cancellationToken);
            }
        }

        private static string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
