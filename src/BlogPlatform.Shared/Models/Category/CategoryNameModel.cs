using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Category
{
    public record CategoryNameModel([Required(AllowEmptyStrings = false)] string Name);
}
