using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record BlogCreate([Required(AllowEmptyStrings = false)] string BlogName, [Required(AllowEmptyStrings = false)] string Description);
}
