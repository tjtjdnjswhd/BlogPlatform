using BlogPlatform.Shared.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Models
{
    public record OAuthSignUpModel([Required(AllowEmptyStrings = false)] string Provider, [UserNameValidate] string Name);
}
