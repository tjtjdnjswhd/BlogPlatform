using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Blog
{
    public record BlogCreate([Required(AllowEmptyStrings = false)] string BlogName, [Required(AllowEmptyStrings = false)] string Description);
}
