using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    public record BasicLoginInfo([property: Required] string Id, [property: Required] string Password);
}
