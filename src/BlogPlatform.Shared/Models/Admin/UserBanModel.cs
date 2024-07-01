using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Admin
{
    public record UserBanModel([EmailAddress] string Email, [Required] TimeSpan BanDuration) : EmailModel(Email);
}
