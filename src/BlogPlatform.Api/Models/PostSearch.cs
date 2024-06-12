using BlogPlatform.EFCore.Extensions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record PostSearch(int? BlogId,
                             int? CategoryId,
                             string? Title,
                             string? Content,
                             DateTimeOffset? CreatedAtStart,
                             DateTimeOffset? CreatedAtEnd,
                             DateTimeOffset? UpdatedAtStart,
                             DateTimeOffset? UpdatedAtEnd,
                             IEnumerable<string>? Tags,
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
