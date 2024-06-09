namespace BlogPlatform.Api.Models
{
    public record CommentSearchResult(int Id, string Content, DateTimeOffset CreatedAt, int PostId, int UserId);
}
