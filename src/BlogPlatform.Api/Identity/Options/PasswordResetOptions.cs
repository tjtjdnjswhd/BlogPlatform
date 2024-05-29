using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Options
{
    public class PasswordResetOptions
    {
        public delegate string EmailBodyGenerator(string newPassword);

        [Required(AllowEmptyStrings = false)]
        public required string From { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string Subject { get; set; }

        [Required]
        public required EmailBodyGenerator BodyFactory { get; set; }
    }
}
