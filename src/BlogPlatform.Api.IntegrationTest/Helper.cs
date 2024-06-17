﻿using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest
{
    public static class Helper
    {
        public const string ACCESS_TOKEN_NAME = "access_token";

        public const string REFRESH_TOKEN_NAME = "refresh_token";

        public static User GetFirstUser(WebApplicationFactory<Program> applicationFactory)
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            return dbContext.Users.First();
        }

        public static async Task<AuthorizeToken> GetAuthorizeTokenAsync(WebApplicationFactory<Program> applicationFactory, User user)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            return await jwtService.GenerateTokenAsync(user);
        }

        public static void SetAuthorizationHeader(HttpClient client, AuthorizeToken token)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        }

        public static void SetAuthorizeTokenCookie(HttpClient client, AuthorizeToken token)
        {
            client.DefaultRequestHeaders.Add("cookie", $"{ACCESS_TOKEN_NAME}={token.AccessToken}; {REFRESH_TOKEN_NAME}={token.RefreshToken}");
        }

        public static async Task SetAuthorizeTokenCache(WebApplicationFactory<Program> applicationFactory, AuthorizeToken authorizeToken)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            await jwtService.SetCacheTokenAsync(authorizeToken, CancellationToken.None);
        }

        public static void SoftDelete<T>(WebApplicationFactory<Program> applicationFactory, T entity, ITestOutputHelper testOutputHelper)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            ICascadeSoftDeleteService cascadeSoftDeleteService = scope.ServiceProvider.GetRequiredService<ICascadeSoftDeleteService>();
            var status = cascadeSoftDeleteService.SetSoftDelete(entity, true);
            testOutputHelper.WriteLine(status.Message);
        }

        public static void HardDelete<T>(WebApplicationFactory<Program> applicationFactory, T entity)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Remove(entity);
            dbContext.SaveChanges();
        }

        public static void ReloadEntity<T>(WebApplicationFactory<Program> applicationFactory, T entity)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Entry(entity).Reload();
        }

        public static void SetEmailVerifyCode(WebApplicationFactory<Program> applicationFactory, string code, string email)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IDistributedCache cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            cache.SetString(UserEmailService.GetVerificationCodeKey(code), email);
        }

        public static void SetVerifiedEmail(WebApplicationFactory<Program> applicationFactory, string email)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IDistributedCache cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            cache.SetString(UserEmailService.GetVerifiedEmailKey(email), string.Empty);
        }

        public static void LoadCollection<T, V>(WebApplicationFactory<Program> applicationFactory, T entity, Expression<Func<T, IEnumerable<V>>> navigationExp)
            where T : EntityBase
            where V : class
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Set<T>().Entry(entity).Collection(navigationExp).Load();
        }
    }
}