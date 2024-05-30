using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// Id/pw로 회원가입할 때 필요한 정보
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Password"></param>
    /// <param name="Email"></param>
    /// <param name="Name"></param>
    public record BasicSignUpInfo([Required(AllowEmptyStrings = false), AccountIdValidate] string Id, [Required(AllowEmptyStrings = false), AccountPasswordValidate] string Password, [Required(AllowEmptyStrings = false), EmailAddress] string Email, [Required(AllowEmptyStrings = false), UserNameValidate] string Name);
}
