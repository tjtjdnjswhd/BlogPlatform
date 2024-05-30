using BlogPlatform.Api.Identity.Services.Interfaces;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Options
{
    /// <summary>
    /// 이메일 인증 설정. <see cref="IVerifyEmailService"/> 참조
    /// </summary>
    public class VerifyEmailOptions : EmailOptions
    {
        /// <summary>
        /// 인증 코드 만료 시간
        /// </summary>
        [Required]
        public required TimeSpan CacheExpiration { get; set; }
    }
}
