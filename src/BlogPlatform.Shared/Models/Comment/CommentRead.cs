namespace BlogPlatform.Shared.Models.Comment
{
    public record CommentRead(int Id, string Content, DateTimeOffset CreatedAt, DateTimeOffset? LastUpdatedAt, int PostId, int UserId, int? ParentCommentId);
}
