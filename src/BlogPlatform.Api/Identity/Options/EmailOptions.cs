using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Options
{
    /// <summary>
    /// 이메일 서비스 옵션
    /// </summary>
    public abstract class EmailOptions
    {
        /// <summary>
        /// 이메일 본문 설정 delegate
        /// </summary>
        /// <param name="value">유저에게 전달할 값</param>
        /// <returns></returns>
        public delegate string EmailBodyGenerator(string value);

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
    }
}
