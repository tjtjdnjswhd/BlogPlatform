using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public class PostSearch : IValidatableObject
    {
        public int? BlogId { get; set; }

        public int? CategoryId { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public DateTimeOffset? CreatedAtStart { get; set; }

        public DateTimeOffset? CreatedAtEnd { get; set; }

        public DateTimeOffset? UpdatedAtStart { get; set; }

        public DateTimeOffset? UpdatedAtEnd { get; set; }

        public IEnumerable<string>? Tags { get; set; }

        public EPostSearchOrderBy OrderBy { get; set; } = EPostSearchOrderBy.CreatedAt;

        public ListSortDirection OrderDirection { get; set; } = ListSortDirection.Ascending;

        public int Page { get; set; } = 1;

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
