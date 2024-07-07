using BlogPlatform.Api.Identity.Services.Interfaces;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class ReturnUrlWhiteListAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return ValidationResult.Success;
            }

            if (value is string returnUrl && !string.IsNullOrWhiteSpace(returnUrl))
            {
                IReturnUrlWhitelistService returnUrlWhitelistService = validationContext.GetRequiredService<IReturnUrlWhitelistService>();
                if (returnUrlWhitelistService.IsWhitelisted(returnUrl))
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult("Invalid return url");
        }
    }
}
