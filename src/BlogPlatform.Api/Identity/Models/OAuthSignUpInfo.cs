using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Models
{
    [ModelBinder<OAuthSignUpInfoModelBinder>]
    public class OAuthSignUpInfo : OAuthInfo
    {
        [UserNameValidate, Required(AllowEmptyStrings = false)]
        public string Name { get; private set; }

        [Required(AllowEmptyStrings = false)]
        public string Email { get; private set; }

        public OAuthSignUpInfo(AuthenticateResult authenticateResult) : base(authenticateResult)
        {
            Debug.Assert(authenticateResult is not null);
            Debug.Assert(authenticateResult.Ticket is not null);
            Debug.Assert(authenticateResult.Principal is not null);
            Debug.Assert(authenticateResult.Properties is not null);

            string? name = authenticateResult.Properties.Items["name"];
            Debug.Assert(name is not null); // OAuthSignUpChallengeResult에서 name을 전달해야 함

            string? email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            Debug.Assert(email is not null); // 인증된 사용자는 Email 클레임을 가져야 함

            Name = name;
            Email = email;
        }
    }
}