using BlogPlatform.EFCore.Extensions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Post
{
    /// <summary>
    /// <paramref name="BlogId"/> 혹은 <paramref name="CategoryId"/> 값이 필요합니다
    /// </summary>
    /// <param name="BlogId"></param>
    /// <param name="CategoryId"></param>
    /// <param name="Title"></param>
    /// <param name="Content"></param>
    /// <param name="CreatedAtStart"></param>
    /// <param name="CreatedAtEnd"></param>
    /// <param name="UpdatedAtStart"></param>
    /// <param name="UpdatedAtEnd"></param>
    /// <param name="Tags"></param>
    /// <param name="TagFilterOption"></param>
    /// <param name="OrderBy"></param>
    /// <param name="OrderDirection"></param>
    /// <param name="Page"></param>
    /// <param name="PageSize">최소 1, 최대 100까지 가능합니다</param>
    public record PostSearch(int? BlogId,
                             int? CategoryId,
                             string? Title,
                             string? Content,
                             DateTimeOffset? CreatedAtStart,
                             DateTimeOffset? CreatedAtEnd,
                             DateTimeOffset? UpdatedAtStart,
                             DateTimeOffset? UpdatedAtEnd,
                             List<string>? Tags,
                             TagFilterOption TagFilterOption = TagFilterOption.All,
                             EPostSearchOrderBy OrderBy = EPostSearchOrderBy.CreatedAt,
                             ListSortDirection OrderDirection = ListSortDirection.Ascending,
                             int Page = 1,
                             [Range(1, 100)]
                             int PageSize = 50)
        : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CategoryId is null && BlogId == default)
            {
                yield return new ValidationResult("BlogId 혹은 CategoryId 값이 필요합니다");
            }
        }
    }

    public enum EPostSearchOrderBy
    {
        CreatedAt,
        UpdatedAt,
        Title,
    }
}
