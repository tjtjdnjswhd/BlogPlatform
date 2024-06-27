using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record OAuthSignUpModel([Required(AllowEmptyStrings = false)] string Provider, [UserNameValidate] string Name);
}
