using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace BlogPlatform.Api.Identity.Extensions
{
    public static class WebApplicationExtensions
    {
        public static void SeedOAuthProviderData(this WebApplication webApplication)
        {
            using var scope = webApplication.Services.CreateScope();

            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            IAuthenticationSchemeProvider schemeProvider = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
            foreach (var scheme in schemeProvider.GetAllSchemesAsync().Result)
            {
                Type? handlerBaseType = scheme.HandlerType?.BaseType;
                if (handlerBaseType != null && handlerBaseType.IsGenericType && handlerBaseType.GetGenericTypeDefinition() == typeof(OAuthHandler<>))
                {
                    if (!dbContext.OAuthProviders.Any(p => p.Name == scheme.Name))
                    {
                        OAuthProvider provider = new(scheme.Name);
                        dbContext.OAuthProviders.Add(provider);
                    }
                }
            }

            dbContext.SaveChanges();
        }
    }
}
