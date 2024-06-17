using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record OAuthProvider([Required(AllowEmptyStrings = false)] string Provider);
}
