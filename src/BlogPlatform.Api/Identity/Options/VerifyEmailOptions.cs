using BlogPlatform.Api.Identity.Services.Interfaces;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Options
{
    /// <summary>
    /// 이메일 인증 설정. <see cref="IVerifyEmailService"/> 참조
    /// </summary>
    public class VerifyEmailOptions
    {
        /// <summary>
        /// 이메일 본문 설정 delegate
        /// </summary>
        /// <param name="code">인증 코드</param>
        /// <returns></returns>
        public delegate string EmailBodyGenerator(string code);

        /// <summary>
        /// 발신자 이메일 주소
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string From { get; set; }

        /// <summary>
        /// 이메일 제목
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string Subject { get; set; }

        /// <summary>
        /// 이메일 본문 생성 delegate
        /// </summary>
        [Required]
        public required EmailBodyGenerator BodyFactory { get; set; }

        /// <summary>
        /// 인증 코드 만료 시간
        /// </summary>
        [Required]
        public required TimeSpan CacheExpiration { get; set; }
    }
}
