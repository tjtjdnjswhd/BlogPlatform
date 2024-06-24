using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record CommentSearch(string? Content, int? PostId, int? UserId, [Range(1, int.MaxValue)] int Page = 1);
}
