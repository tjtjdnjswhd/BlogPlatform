using BlogPlatform.Shared.Identity.Services.Interfaces;

namespace BlogPlatform.Shared.Identity.Models
{
    /// <summary>
    /// OAuth 추가의 결과. <see cref="IIdentityService.AddOAuthAsync"/>의 반환값
    /// </summary>
    public enum EAddOAuthResult
    {
        /// <summary>
        /// 추가 성공
        /// </summary>
        Success,

        /// <summary>
        /// 사용자를 찾을 수 없음
        /// </summary>
        UserNotFound,

        /// <summary>
        /// 사용자가 이미 OAuth를 가지고 있음
        /// </summary>
        UserAlreadyHasOAuth,

        /// <summary>
        /// 이미 존재하는 OAuth
        /// </summary>
        OAuthAlreadyExists,

        /// <summary>
        /// 지원하지 않는 OAuth 제공자
        /// </summary>
        ProviderNotFound
    }
}
