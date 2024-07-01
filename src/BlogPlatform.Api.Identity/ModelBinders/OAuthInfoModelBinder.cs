using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Diagnostics;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.ModelBinders
{
    public class OAuthInfoModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Debug.Assert(bindingContext.ModelType == typeof(OAuthLoginInfo) || bindingContext.ModelType == typeof(OAuthSignUpInfo));

            IAuthenticateResultFeature? authenticateResultFeature = bindingContext.HttpContext.Features.Get<IAuthenticateResultFeature>();
            Debug.Assert(authenticateResultFeature is not null); // 인증 미들웨어에서 인증 결과를 전달해야 함

            AuthenticateResult? authenticateResult = authenticateResultFeature.AuthenticateResult;
            Debug.Assert(authenticateResult is not null);
            Debug.Assert(authenticateResult.Ticket is not null);
            Debug.Assert(authenticateResult.Principal is not null);
            Debug.Assert(authenticateResult.Principal.Identity is not null);

            string? provider = authenticateResult.Principal.Identity.AuthenticationType;
            Debug.Assert(provider is not null); // 인증된 사용자는 AuthenticationType을 가져야 함

            string? nameIdentifier = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Debug.Assert(nameIdentifier is not null); // 인증된 사용자는 NameIdentifier 클레임을 가져야 함

            if (bindingContext.ModelType == typeof(OAuthLoginInfo))
            {
                OAuthLoginInfo oAuthLoginInfo = new(provider, nameIdentifier);
                bindingContext.Result = ModelBindingResult.Success(oAuthLoginInfo);
                return Task.CompletedTask;
            }

            Debug.Assert(bindingContext.ModelType == typeof(OAuthSignUpInfo));

            string? name = bindingContext.HttpContext.Request.Cookies[OAuthSignUpChallengeResult.SignUpNameCookieKey];
            if (name is null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.AddModelError(nameof(OAuthSignUpInfo.Name), "Name is required.");
                return Task.CompletedTask;
            }
            bindingContext.HttpContext.Response.Cookies.Delete(OAuthSignUpChallengeResult.SignUpNameCookieKey);

            string? email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            Debug.Assert(email is not null); // 인증된 사용자는 Email 클레임을 가져야 함

            OAuthSignUpInfo oAuthSignUpInfo = new(name, email, provider, nameIdentifier);

            bindingContext.Result = ModelBindingResult.Success(oAuthSignUpInfo);
            return Task.CompletedTask;
        }
    }
}
