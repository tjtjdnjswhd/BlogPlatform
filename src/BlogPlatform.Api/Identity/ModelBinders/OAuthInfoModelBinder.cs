using BlogPlatform.Api.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.ModelBinders
{
    public class OAuthInfoModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            IAuthenticateResultFeature? authenticateResultFeature = bindingContext.HttpContext.Features.Get<IAuthenticateResultFeature>();
            Debug.Assert(authenticateResultFeature is not null); // 인증 미들웨어에서 인증 결과를 전달해야 함

            AuthenticateResult? authenticateResult = authenticateResultFeature.AuthenticateResult;
            Debug.Assert(authenticateResult is not null);

            OAuthInfo oauthInfo = new(authenticateResult);

            bindingContext.Result = ModelBindingResult.Success(oauthInfo);
            return Task.CompletedTask;
        }
    }
}
