using BlogPlatform.Shared.Identity.Validations;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Models
{
    /// <summary>
    /// Id/pw로 회원가입할 때 필요한 정보
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Password"></param>
    /// <param name="Name"></param>
    /// <param name="Email"></param>
    /// 
    public record BasicSignUpInfo([Required(AllowEmptyStrings = false), AccountIdValidate] string Id, [Required(AllowEmptyStrings = false), AccountPasswordValidate] string Password, [Required(AllowEmptyStrings = false), UserNameValidate] string Name, [Required(AllowEmptyStrings = false), EmailAddress] string Email);
}
