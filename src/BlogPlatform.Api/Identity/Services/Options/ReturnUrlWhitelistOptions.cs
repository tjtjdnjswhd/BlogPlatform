using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Services.Options
{
    public class ReturnUrlWhitelistOptions : List<string>, IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            for (int i = 0; i < Count; i++)
            {
                string? uri = this[i];
                if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                {
                    yield return new ValidationResult($"Return URL must be a absolute URL. index:{i} value: {uri}");
                }
                else if (uri.EndsWith('/'))
                {
                    yield return new ValidationResult($"Return URL must not end with a slash. index:{i} value: {uri}");
                }
                else if (!uri.StartsWith("https://") && !uri.StartsWith("http://"))
                {
                    yield return new ValidationResult($"Return URL must start with http:// or https://. index:{i} value: {uri}");
                }
            }
        }
    }
}