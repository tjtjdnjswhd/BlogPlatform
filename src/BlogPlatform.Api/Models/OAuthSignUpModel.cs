using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record OAuthSignUpModel([Required(AllowEmptyStrings = false)] string Provider, [UserNameValidate] string Name) : OAuthProvider(Provider);
}
