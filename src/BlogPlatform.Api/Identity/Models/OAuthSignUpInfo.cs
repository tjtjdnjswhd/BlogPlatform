using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// OAuth 인증을 통한 사용자의 가입 정보
    /// </summary>
    [ModelBinder<OAuthInfoModelBinder>]
    public class OAuthSignUpInfo : OAuthLoginInfo
    {
        /// <summary>
        /// 가입할 사용자의 이름
        /// </summary>
        [UserNameValidate, Required(AllowEmptyStrings = false)]
        public string Name { get; private set; }

        /// <summary>
        /// 가입할 사용자의 이메일
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string Email { get; private set; }

        public OAuthSignUpInfo(string provider, string nameIdentifier, string name, string email) : base(provider, nameIdentifier)
        {
            Name = name;
            Email = email;
        }
    }
}
