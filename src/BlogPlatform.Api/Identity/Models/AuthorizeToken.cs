using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// JWT 토큰 발급의 결과값
    /// </summary>
    /// <param name="AccessToken"></param>
    /// <param name="RefreshToken"></param>
    public record AuthorizeToken([Required] string AccessToken, [Required] string RefreshToken);
}
