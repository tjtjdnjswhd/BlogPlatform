using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Options;

using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BasicSignUpValidateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not BasicSignUpInfo basicSignUpInfo)
            {
                Debug.Assert(false);
                throw new ArgumentException("BasicSignUpInfo 타입이 아닙니다.");
            }

            AccountOptions accountOptions = validationContext.GetRequiredService<IOptionsMonitor<AccountOptions>>().CurrentValue;

            if (basicSignUpInfo.Id.Length < accountOptions.MinIdLength || basicSignUpInfo.Id.Length > accountOptions.MaxIdLength)
            {
                return new ValidationResult($"Id의 길이는 {accountOptions.MinIdLength}에서 {accountOptions.MaxIdLength} 사이여야 합니다.", [nameof(BasicSignUpInfo.Id)]);
            }

            if (basicSignUpInfo.Password.Length < accountOptions.MinPasswordLength || basicSignUpInfo.Password.Length > accountOptions.MaxPasswordLength)
            {
                return new ValidationResult($"비밀번호의 길이는 {accountOptions.MinPasswordLength}에서 {accountOptions.MaxPasswordLength} 사이여야 합니다.", [nameof(BasicSignUpInfo.Password)]);
            }

            return base.IsValid(value, validationContext);
        }
    }
}
