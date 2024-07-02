using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogPlatform.Api.Identity.ModelBinders
{
    public class RefreshAuthorizeTokenBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            CancellationToken cancellationToken = bindingContext.HttpContext.RequestAborted;
            using var scope = bindingContext.HttpContext.RequestServices.CreateScope();
            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            AuthorizeToken? authorizeToken = await authorizeTokenService.GetAsync(bindingContext.HttpContext.Request, true, cancellationToken) ?? await authorizeTokenService.GetAsync(bindingContext.HttpContext.Request, false, cancellationToken);
            bindingContext.Result = authorizeToken is null ? ModelBindingResult.Failed() : ModelBindingResult.Success(authorizeToken);
        }
    }
}
