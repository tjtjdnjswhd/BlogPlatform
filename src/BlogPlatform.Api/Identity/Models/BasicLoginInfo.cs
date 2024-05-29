using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record BasicLoginInfo([Required(AllowEmptyStrings = false), AccountIdValidate] string Id, [Required(AllowEmptyStrings = false), AccountPasswordValidate] string Password);
}
