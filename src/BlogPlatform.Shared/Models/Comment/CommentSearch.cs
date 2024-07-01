using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Comment
{
    public record CommentSearch(string? Content, int? PostId, int? UserId, ECommentSearchOrderBy OrderBy = ECommentSearchOrderBy.CreatedAt, ListSortDirection OrderDirection = ListSortDirection.Ascending, [Range(1, int.MaxValue)] int Page = 1);

    public enum ECommentSearchOrderBy
    {
        CreatedAt,
        UpdatedAt,
        Content,
        Post,
        User
    }
}
