using BlogPlatform.Shared.Identity.Validations;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Models
{
    public record OAuthSignUpModel([Required(AllowEmptyStrings = false)] string Provider, [UserNameValidate] string Name);
}
