using BlogPlatform.Api.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// OAuth 방식으로 회원가입시 반환하는 <see cref="IActionResult"/>
    /// </summary>
    public class OAuthSignUpChallengeResult : ChallengeResult
    {
        public OAuthSignUpChallengeResult(AuthenticationProperties properties, OAuthSignUpModel signUpModel) : base(signUpModel.Provider.ToLower(), properties)
        {
            Properties!.Items.Add("name", signUpModel.Name);
        }
    }
}
