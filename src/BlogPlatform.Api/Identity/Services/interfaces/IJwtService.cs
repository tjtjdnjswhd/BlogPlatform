using BlogPlatform.Api.Identity.Models;
using BlogPlatform.EFCore.Models;

namespace BlogPlatform.Api.Services.interfaces
{
    public interface IJwtService
    {
        /// <summary>
        /// <see cref="User"/>로 토큰을 생성합니다.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthorizeToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default);

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
        /// 캐시에서 토큰을 읽고 새 토큰을 반환합니다.
        /// 캐시에 해당 토큰이 없을 경우 null을 반환합니다.
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

        /// <summary>
        /// 응답 본문에 토큰을 설정합니다.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetBodyTokenAsync(HttpResponse response, AuthorizeToken token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 요청 바디에서 토큰을 가져옵니다.
        /// 해당 토큰이 없을 시 null을 반환합니다.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthorizeToken?> GetBodyTokenAsync(HttpRequest request, CancellationToken cancellationToken = default);
    }
}
