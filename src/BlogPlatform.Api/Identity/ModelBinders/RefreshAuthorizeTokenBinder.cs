using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Services.Interfaces;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogPlatform.Api.Identity.ModelBinders
{
    public class RefreshAuthorizeTokenBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            using var scope = bindingContext.HttpContext.RequestServices.CreateScope();
            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            AuthorizeToken? authorizeToken = await jwtService.GetBodyTokenAsync(bindingContext.HttpContext.Request, bindingContext.HttpContext.RequestAborted);
            if (authorizeToken is null)
            {
                authorizeToken = jwtService.GetCookieToken(bindingContext.HttpContext.Request);
                bindingContext.Result = authorizeToken is null ? ModelBindingResult.Failed() : ModelBindingResult.Success(authorizeToken);
            }
        }
    }
}
