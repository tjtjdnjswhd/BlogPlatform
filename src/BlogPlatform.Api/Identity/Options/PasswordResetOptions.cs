using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Options
{
    /// <summary>
    /// 비밀번호 재설정 설정
    /// </summary>
    public class PasswordResetOptions
    {
        /// <summary>
        /// 이메일 본문 생성 delegate
        /// </summary>
        /// <param name="newPassword">새로운 비밀번호</param>
        /// <returns></returns>
        public delegate string EmailBodyGenerator(string newPassword);

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
