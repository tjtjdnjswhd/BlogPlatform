namespace BlogPlatform.Api.Models
{
    public record CommentSearch(string? Content, int? PostId, int? UserId)
    {
        public int Page { get; init; } = 1;
    }
}
