using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record BasicSignUpInfo([Required(AllowEmptyStrings = false), AccountIdValidate] string Id, [Required(AllowEmptyStrings = false), AccountPasswordValidate] string Password, [Required(AllowEmptyStrings = false), EmailAddress] string Email, [Required(AllowEmptyStrings = false), UserNameValidate] string Name);
}
