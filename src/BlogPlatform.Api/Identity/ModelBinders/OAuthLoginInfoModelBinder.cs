using BlogPlatform.Api.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Diagnostics;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.ModelBinders
{
    public class OAuthLoginInfoModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            IAuthenticateResultFeature? authenticateResultFeature = bindingContext.HttpContext.Features.Get<IAuthenticateResultFeature>();
            Debug.Assert(authenticateResultFeature is not null); // 인증 미들웨어에서 인증 결과를 전달해야 함

            AuthenticateResult? authenticateResult = authenticateResultFeature.AuthenticateResult;

            // 인증 실패 시 인증 미들웨어에서 short-circuiting 해야 함
            Debug.Assert(authenticateResult is not null);
            Debug.Assert(authenticateResult.Ticket is not null);
            Debug.Assert(authenticateResult.Principal is not null);

            string provider = authenticateResult.Ticket.AuthenticationScheme;
            string? nameIdentifier = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Debug.Assert(nameIdentifier is not null); // 인증된 사용자는 NameIdentifier 클레임을 가져야 함

            OAuthLoginInfo oauthLoginInfo = new(provider, nameIdentifier);

            bindingContext.Result = ModelBindingResult.Success(oauthLoginInfo);
            return Task.CompletedTask;
        }
    }
}
