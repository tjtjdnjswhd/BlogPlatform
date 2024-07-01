using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models
{
    public record CategoryNameModel([Required(AllowEmptyStrings = false)] string Name);
}
