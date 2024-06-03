using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// OAuth 인증을 통한 사용자의 로그인 정보 
    /// </summary>
    [ModelBinder<OAuthInfoModelBinder>]
    public class OAuthLoginInfo
    {
        /// <summary>
        /// OAuth 제공자
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string Provider { get; private set; }

        /// <summary>
        /// OAuth 제공자에서 제공하는 유저 식별자
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string NameIdentifier { get; private set; }

        public OAuthLoginInfo(string provider, string nameIdentifier)
        {
            Provider = provider;
            NameIdentifier = nameIdentifier;
        }
    }
}
