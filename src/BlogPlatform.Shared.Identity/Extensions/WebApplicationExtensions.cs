using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Constants;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPlatform.Shared.Identity.Extensions
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

        public static void SeedRoleData(this WebApplication webApplication)
        {
            using var scope = webApplication.Services.CreateScope();

            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            if (!dbContext.Roles.Any())
            {
                dbContext.Roles.Add(new Role(PolicyConstants.UserPolicy, 1));
                dbContext.Roles.Add(new Role(PolicyConstants.AdminPolicy, 0));
            }

            dbContext.SaveChanges();
        }
    }
}
