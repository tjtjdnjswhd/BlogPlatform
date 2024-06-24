using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record UserBanModel([EmailAddress] string Email, [Required] TimeSpan BanDuration) : EmailModel(Email);
}
