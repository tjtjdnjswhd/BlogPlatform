using BlogPlatform.Api.Identity.Options;

using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class AccountPasswordValidateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string password)
            {
                Debug.Assert(false);
                throw new ArgumentException("string 타입이 아닙니다.", nameof(value));
            }

            AccountOptions accountOptions = validationContext.GetRequiredService<IOptionsMonitor<AccountOptions>>().CurrentValue;

            if (password.Length < accountOptions.MinPasswordLength || password.Length > accountOptions.MaxPasswordLength)
            {
                return new ValidationResult($"비밀번호의 길이는 {accountOptions.MinPasswordLength}에서 {accountOptions.MaxPasswordLength} 사이여야 합니다.", [validationContext.MemberName]);
            }

            return base.IsValid(value, validationContext);
        }
    }
}