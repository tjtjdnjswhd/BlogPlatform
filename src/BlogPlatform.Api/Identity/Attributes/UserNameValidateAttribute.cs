using BlogPlatform.Api.Identity.Options;

using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Attributes
{
    /// <summary>
    /// 유저 이름의 유효성을 검사하는 Attribute.
    /// <seealso cref="AccountOptions"/> 참조
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class UserNameValidateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return new ValidationResult("이름이 입력되지 않았습니다.", [validationContext.MemberName]);
            }

            if (value is not string userName)
            {
                Debug.Assert(false);
                throw new ArgumentException("string 타입이 아닙니다.", nameof(value));
            }

            AccountOptions accountOptions = validationContext.GetRequiredService<IOptionsMonitor<AccountOptions>>().CurrentValue;

            if (userName.Length < accountOptions.MinNameLength || userName.Length > accountOptions.MaxNameLength)
            {
                return new ValidationResult($"이름의 길이는 {accountOptions.MinNameLength}에서 {accountOptions.MaxNameLength} 사이여야 합니다.", [validationContext.MemberName]);
            }

            return null;
        }
    }
}
