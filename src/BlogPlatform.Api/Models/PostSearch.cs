using BlogPlatform.EFCore.Extensions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record PostSearch(int? BlogId, int? CategoryId, string? Title, string? Content, DateTimeOffset? CreatedAtStart, DateTimeOffset? CreatedAtEnd, DateTimeOffset? UpdatedAtStart, DateTimeOffset? UpdatedAtEnd, IEnumerable<string>? Tags) : IValidatableObject
    {
        public TagFilterOption TagFilter { get; init; } = TagFilterOption.All;

        public EPostSearchOrderBy OrderBy { get; init; } = EPostSearchOrderBy.CreatedAt;

        public ListSortDirection OrderDirection { get; init; } = ListSortDirection.Ascending;

        public int Page { get; init; } = 1;

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
