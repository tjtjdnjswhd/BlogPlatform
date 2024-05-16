using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record Error([property: Required] string Message);
}
