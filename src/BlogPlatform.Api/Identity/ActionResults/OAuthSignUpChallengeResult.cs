using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// OAuth 방식으로 회원가입시 반환하는 <see cref="IActionResult"/>
    /// </summary>
    public class OAuthSignUpChallengeResult : ChallengeResult
    {
        public OAuthSignUpChallengeResult(AuthenticationProperties properties, string scheme, string name) : base(scheme, properties)
        {
            Properties!.Items.Add("name", name);
        }
    }
}
