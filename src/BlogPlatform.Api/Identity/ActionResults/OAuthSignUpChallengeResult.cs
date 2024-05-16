using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    public class OAuthSignUpChallengeResult : ChallengeResult
    {
        public OAuthSignUpChallengeResult(AuthenticationProperties properties, string scheme, string name) : base(scheme, properties)
        {
            Properties!.Items.Add("name", name);
        }
    }
}
