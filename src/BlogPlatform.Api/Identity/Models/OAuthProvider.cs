using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record OAuthProvider([Required(AllowEmptyStrings = false)] string Provider);
}
