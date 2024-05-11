using BlogPlatform.Api.Identity.Models;

using System.Security.Claims;

namespace BlogPlatform.Api.Services.interfaces
{
    public interface IJwtService
    {
        /// <summary>
        /// <paramref name="claimsIdentity"/>으로 토큰을 생성합니다.
        /// </summary>
        /// <param name="claimsIdentity"></param>
        /// <returns></returns>
        AuthorizeToken GenerateToken(ClaimsIdentity claimsIdentity);

        /// <summary>
        /// 캐시에 토큰을 저장합니다.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetCacheTokenAsync(AuthorizeToken token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 캐시에서 토큰을 제거합니다.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RemoveCachedTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// 캐시에서 토큰을 가져옵니다.
        /// 해당 토큰이 없을 경우 null을 반환합니다.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string?> GetCachedTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// 캐시에서 토큰을 읽고 갱신합니다.
        /// 해당 토큰이 없을 경우 null을 반환합니다.
        /// </summary>
        /// <param name="oldToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthorizeToken?> RefreshAsync(AuthorizeToken oldToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// 쿠키에서 토큰을 가져옵니다.
        /// 해당 토큰이 없을 시 null을 반환합니다.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        AuthorizeToken? GetCookieToken(HttpRequest request);

        /// <summary>
        /// 쿠키에서 토큰을 제거합니다.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        AuthorizeToken? RemoveCookieToken(HttpRequest request, HttpResponse response);

        /// <summary>
        /// 쿠키에 토큰을 설정합니다.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="token"></param>
        void SetCookieToken(HttpResponse response, AuthorizeToken token);
    }
}