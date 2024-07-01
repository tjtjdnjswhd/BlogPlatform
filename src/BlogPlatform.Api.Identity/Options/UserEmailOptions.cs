using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BlogPlatform.Api.Identity.Options
{
    /// <summary>
    /// 유저 이메일 서비스 옵션
    /// </summary>
    public class UserEmailOptions
    {
        /// <summary>
        /// 비밀번호 초기화 이메일 제목
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string PasswordResetSubject { get; set; }

        /// <summary>
        /// 비밀번호 초기화 이메일 본문
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        public required string PasswordResetBody { get; set; }

        /// <summary>
        /// 계정 ID 찾기 이메일 제목
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string AccountIdSubject { get; set; }

        /// <summary>
        /// 계정 ID 찾기 이메일 본문
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        public required string AccountIdBody { get; set; }

        /// <summary>
        /// 메일 인증 이메일 제목
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string EmailVerifySubject { get; set; }

        /// <summary>
        /// 메일 인증 이메일 본문
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        public required string EmailVerifyBody { get; set; }

        /// <summary>
        /// 메일 인증 만료 시간
        /// </summary>
        [Required]
        public required TimeSpan EmailVerifyExpiration { get; set; }
    }
}
