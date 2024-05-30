using BlogPlatform.Api.Identity.Services.Interfaces;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// 회원가입 결과. <see cref="IIdentityService.SignUpAsync"/>의 반환값
    /// </summary>
    public enum ESignUpResult
    {
        /// <summary>
        /// 가입 성공
        /// </summary>
        Success,

        /// <summary>
        /// 이미 존재하는 사용자 ID
        /// </summary>
        UserIdAlreadyExists,

        /// <summary>
        /// 이미 존재하는 사용자 이름
        /// </summary>
        NameAlreadyExists,

        /// <summary>
        /// 이미 존재하는 이메일
        /// </summary>
        EmailAlreadyExists,

        /// <summary>
        /// 지원하지 않는 OAuth 제공자
        /// </summary>
        ProviderNotFound,

        /// <summary>
        /// 이미 존재하는 OAuth 식별자
        /// </summary>
        OAuthAlreadyExists
    }
}
