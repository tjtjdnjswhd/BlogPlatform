using BlogPlatform.Shared.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Models
{
    public record PasswordModel([Required, AccountPasswordValidate] string Password);
}
