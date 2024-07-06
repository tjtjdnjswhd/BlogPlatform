using BlogPlatform.Shared.Identity.Models;

using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Services.Interfaces
{
    public interface IAuthorizeTokenService
    {
        AuthorizeToken GenerateToken(ClaimsPrincipal user, bool setCookie);

        Task WriteAsync(HttpResponse response, AuthorizeToken token, bool setCookie, CancellationToken cancellationToken = default);

        Task<AuthorizeToken?> GetAsync(HttpRequest request, bool fromCookie, CancellationToken cancellationToken = default);

        Task CacheTokenAsync(AuthorizeToken token, CancellationToken cancellationToken = default);

        Task<string?> GetCachedTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task RemoveTokenAsync(HttpRequest request, HttpResponse response, string? refreshToken, CancellationToken cancellationToken = default);
    }
}
