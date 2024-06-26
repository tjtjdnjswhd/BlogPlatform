using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record PasswordModel([Required, AccountPasswordValidate] string Password);
}
