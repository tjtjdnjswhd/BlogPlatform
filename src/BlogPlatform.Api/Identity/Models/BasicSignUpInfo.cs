using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    [BasicSignUpValidate]
    public record BasicSignUpInfo([Required] string Id, [Required] string Password, [Required] string Email, [Required, UserNameValidate] string Name);
}
