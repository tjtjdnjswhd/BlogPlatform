using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Comment
{
    public record CommentUpdate([Required(AllowEmptyStrings = false)] string Content);
}
