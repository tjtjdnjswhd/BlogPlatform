namespace BlogPlatform.Shared.Models.Comment
{
    public record CommentSearchResult(int Id, string Content, DateTimeOffset CreatedAt, int PostId, int UserId);
}
