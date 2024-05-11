using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record AuthorizeToken([property: Required] string AccessToken, [property: Required] string RefreshToken);
}
