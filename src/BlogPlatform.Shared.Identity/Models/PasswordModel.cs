using BlogPlatform.Shared.Identity.Validations;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Models
{
    public record PasswordModel([Required, AccountPasswordValidate] string Password);
}
