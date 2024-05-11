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

            BasicAccountOptions basicAccountOptions = validationContext.GetRequiredService<IOptionsMonitor<BasicAccountOptions>>().CurrentValue;

            if (basicSignUpInfo.Id.Length < basicAccountOptions.MinIdLength || basicSignUpInfo.Id.Length > basicAccountOptions.MaxIdLength)
            {
                return new ValidationResult($"Id의 길이는 {basicAccountOptions.MinIdLength}에서 {basicAccountOptions.MaxIdLength} 사이여야 합니다.", [nameof(BasicSignUpInfo.Id)]);
            }

            if (basicSignUpInfo.Password.Length < basicAccountOptions.MinPasswordLength || basicSignUpInfo.Password.Length > basicAccountOptions.MaxPasswordLength)
            {
                return new ValidationResult($"비밀번호의 길이는 {basicAccountOptions.MinPasswordLength}에서 {basicAccountOptions.MaxPasswordLength} 사이여야 합니다.", [nameof(BasicSignUpInfo.Password)]);
            }

            if (basicSignUpInfo.Name.Length < basicAccountOptions.MinNameLength || basicSignUpInfo.Name.Length > basicAccountOptions.MaxNameLength)
            {
                return new ValidationResult($"이름의 길이는 {basicAccountOptions.MinNameLength}에서 {basicAccountOptions.MaxNameLength} 사이여야 합니다.", [nameof(BasicSignUpInfo.Name)]);
            }

            return base.IsValid(value, validationContext);
        }
    }
}
