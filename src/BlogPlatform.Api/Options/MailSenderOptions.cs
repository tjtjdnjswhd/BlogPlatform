using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Options
{
    public class MailSenderOptions
    {
        [Required]
        public required string? Host { get; set; }

        [Required]
        public required int Port { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
