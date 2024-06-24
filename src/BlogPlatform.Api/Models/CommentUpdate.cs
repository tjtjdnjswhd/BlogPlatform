using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record CommentUpdate([Required(AllowEmptyStrings = false)] string Content);
}
