using BlogPlatform.Shared.Identity.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Shared.Identity.Validations
{
    /// <summary>
    /// 계정 Id의 유효성을 검사하는 Attribute.
    /// <seealso cref="AccountOptions"/> 참조
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class AccountIdValidateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string accountId)
            {
                Debug.Assert(false);
                throw new ArgumentException("BasicSignUpInfo 타입이 아닙니다.");
            }

            AccountOptions accountOptions = validationContext.GetRequiredService<IOptions<AccountOptions>>().Value;

            if (accountId.Length < accountOptions.MinIdLength || accountId.Length > accountOptions.MaxIdLength)
            {
                return new ValidationResult($"Id의 길이는 {accountOptions.MinIdLength}에서 {accountOptions.MaxIdLength} 사이여야 합니다.");
            }

            return null;
        }
    }
}
