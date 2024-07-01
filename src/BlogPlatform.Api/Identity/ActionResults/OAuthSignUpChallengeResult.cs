using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// OAuth 방식으로 회원가입시 반환하는 <see cref="IActionResult"/>
    /// </summary>
    public class OAuthSignUpChallengeResult : ChallengeResult
    {
        public const string SignUpNameCookieKey = "OAuthSignUpName";

        private static readonly CookieOptions _cookieOptions = new()
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = true
        };

        private readonly OAuthSignUpModel _signUpModel;

        public OAuthSignUpChallengeResult(AuthenticationProperties properties, OAuthSignUpModel signUpModel) : base(signUpModel.Provider, properties)
        {
            _signUpModel = signUpModel;
        }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);
            context.HttpContext.Response.Cookies.Append(SignUpNameCookieKey, _signUpModel.Name, _cookieOptions);
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            ExecuteResult(context);
            return base.ExecuteResultAsync(context);
        }
    }
}
