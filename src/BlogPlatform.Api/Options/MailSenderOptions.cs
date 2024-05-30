using BlogPlatform.Api.Services.Interfaces;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Options
{
    /// <summary>
    /// 이메일 발송을 위한 옵션. <see cref="IMailSender"/> 참조
    /// </summary>
    public class MailSenderOptions
    {
        /// <summary>
        /// 이메일 발송을 위한 SMTP 서버 호스트 주소
        /// </summary>
        [Required]
        public required string? Host { get; set; }

        /// <summary>
        /// 이메일 발송을 위한 SMTP 서버 포트
        /// </summary>
        [Required]
        public required int Port { get; set; }

        /// <summary>
        /// SMTP 서버를 사용하기 위한 사용자 이름
        /// </summary>
        [Required]
        public required string Username { get; set; }

        /// <summary>
        /// SMTP 서버를 사용하기 위한 비밀번호
        /// </summary>
        [Required]
        public required string Password { get; set; }
    }
}
