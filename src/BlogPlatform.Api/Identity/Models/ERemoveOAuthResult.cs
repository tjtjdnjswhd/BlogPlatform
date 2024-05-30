using BlogPlatform.Api.Identity.Services.Interfaces;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// 유저의 OAuth 연결을 제거한 결과. <see cref="IIdentityService.RemoveOAuthAsync"/>의 반환값
    /// </summary>
    public enum ERemoveOAuthResult
    {
        /// <summary>
        /// 삭제 성공
        /// </summary>
        Success,

        /// <summary>
        /// 유저를 찾을 수 없음
        /// </summary>
        UserNotFound,

        /// <summary>
        /// OAuth 정보를 찾을 수 없음
        /// </summary>
        OAuthNotFound
    }
}
