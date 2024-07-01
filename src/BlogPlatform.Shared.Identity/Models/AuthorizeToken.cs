using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlogPlatform.Shared.Identity.Models
{
    /// <summary>
    /// JWT 토큰 발급의 결과값
    /// </summary>
    /// <param name="AccessToken"></param>
    /// <param name="RefreshToken"></param>
    [method: JsonConstructor]
    public record AuthorizeToken([Required(AllowEmptyStrings = false)] string AccessToken, [Required(AllowEmptyStrings = false)] string RefreshToken);
}
