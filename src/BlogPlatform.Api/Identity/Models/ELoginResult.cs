using BlogPlatform.Api.Identity.Services.Interfaces;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// 로그인 결과를 나타내는 열거형. <see cref="IIdentityService.LoginAsync"/>의 반환값
    /// </summary>
    public enum ELoginResult
    {
        /// <summary>
        /// 로그인 성공
        /// </summary>
        Success,

        /// <summary>
        /// 사용자를 찾을 수 없음
        /// </summary>
        NotFound,

        /// <summary>
        /// 잘못된 비밀번호
        /// </summary>
        WrongPassword
    }
}
