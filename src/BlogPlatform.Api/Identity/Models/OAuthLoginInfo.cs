using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

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

        public OAuthLoginInfo(AuthenticateResult authenticateResult)
        {
            Debug.Assert(authenticateResult.Ticket is not null);
            Debug.Assert(authenticateResult.Principal is not null);

            string provider = authenticateResult.Ticket.AuthenticationScheme;
            string? nameIdentifier = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Debug.Assert(nameIdentifier is not null); // 인증된 사용자는 NameIdentifier 클레임을 가져야 함

            Provider = provider;
            NameIdentifier = nameIdentifier;
        }
    }
}
