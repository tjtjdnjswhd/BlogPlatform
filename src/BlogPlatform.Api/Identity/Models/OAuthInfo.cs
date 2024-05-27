using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Models
{
    [ModelBinder<OAuthInfoModelBinder>]
    public class OAuthInfo
    {
        [Required(AllowEmptyStrings = false)]
        public string Provider { get; private set; }

        [Required(AllowEmptyStrings = false)]
        public string NameIdentifier { get; private set; }

        public OAuthInfo(AuthenticateResult authenticateResult)
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
