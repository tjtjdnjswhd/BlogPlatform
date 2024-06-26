﻿using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;

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

        public static void SeedRoleData(this WebApplication webApplication)
        {
            using var scope = webApplication.Services.CreateScope();

            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            if (!dbContext.Roles.Any())
            {
                dbContext.Roles.Add(new Role("User", 1));
                dbContext.Roles.Add(new Role("Admin", 0));
            }

            dbContext.SaveChanges();
        }

        public static async void SeedDevelopmentAdminAccount(this WebApplication webApplication)
        {
            if (webApplication.Environment.IsDevelopment())
            {
                using var scope = webApplication.Services.CreateScope();

                BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
                if (!dbContext.Users.Any(u => u.Name == "Admin"))
                {
                    IIdentityService identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
                    (ESignUpResult signUpResult, User? admin) = await identityService.SignUpAsync(new BasicSignUpInfo("admin", "admin", "admin", "admin@user.com", null));
                    if (signUpResult != ESignUpResult.Success)
                    {
                        throw new InvalidOperationException("Failed to seed development admin account.");
                    }

                    Role adminRole = dbContext.Roles.First(r => r.Name == "Admin");
                    admin!.Roles.Add(adminRole);
                    dbContext.SaveChanges();
                }
            }
        }
    }
}