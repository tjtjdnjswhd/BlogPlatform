using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record SearchUser([Required] bool IsRemoved, string? Id, string? Email, string? Name) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Id is null && Email is null && Name is null)
            {
                yield return new ValidationResult("Id, Email, Name 중 하나는 필요합니다");
            }

            if (!(Id is not null && Email is null && Name is null || Id is null && Email is not null && Name is null || Id is null && Email is null && Name is not null))
            {
                yield return new ValidationResult("Id, Email, Name 중 하나만 사용할 수 있습니다");
            }
        }
    }
}
