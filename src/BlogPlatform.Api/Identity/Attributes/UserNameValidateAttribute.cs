using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Options;

using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class UserNameValidateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string userName)
            {
                Debug.Assert(false);
                throw new ArgumentException("string 타입이 아닙니다.");
            }

            AccountOptions accountOptions = validationContext.GetRequiredService<IOptionsMonitor<AccountOptions>>().CurrentValue;

            if (userName.Length < accountOptions.MinNameLength || userName.Length > accountOptions.MaxNameLength)
            {
                return new ValidationResult($"이름의 길이는 {accountOptions.MinNameLength}에서 {accountOptions.MaxNameLength} 사이여야 합니다.", [nameof(BasicSignUpInfo.Name)]);
            }

            return base.IsValid(value, validationContext);
        }
    }
}
