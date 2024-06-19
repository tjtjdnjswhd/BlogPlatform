using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record CategoryNameModel([Required(AllowEmptyStrings = false)] string Name);
}
