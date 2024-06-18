using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;

namespace BlogPlatform.Api.IntegrationTest
{
    public static class Helper
    {
        public const string ACCESS_TOKEN_NAME = "access_token";

        public const string REFRESH_TOKEN_NAME = "refresh_token";

        public static T AddEntity<T>(WebApplicationFactory<Program> applicationFactory, T entity)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Add(entity);
            dbContext.SaveChanges();
            return entity;
        }

        public static T GetFirstEntity<T>(WebApplicationFactory<Program> applicationFactory, bool ignoreSoftDelete = false)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            return ignoreSoftDelete ? dbContext.Set<T>().IgnoreSoftDeleteFilter().First() : dbContext.Set<T>().First();
        }

        public static T GetFirstEntity<T>(WebApplicationFactory<Program> applicationFactory, Expression<Func<T, bool>> predicate, bool ignoreSoftDelete = false)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            return ignoreSoftDelete ? dbContext.Set<T>().IgnoreSoftDeleteFilter().First(predicate) : dbContext.Set<T>().First(predicate);
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

        public static void ResetAuthorizationHeader(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = null;
        }

        public static void ResetAuthorizeTokenCookie(HttpClient client)
        {
            client.DefaultRequestHeaders.Remove("cookie");
        }

        public static async Task SetAuthorizeTokenCache(WebApplicationFactory<Program> applicationFactory, AuthorizeToken authorizeToken)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            await jwtService.SetCacheTokenAsync(authorizeToken, CancellationToken.None);
        }

        public static void SoftDelete<T>(WebApplicationFactory<Program> applicationFactory, T entity)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            ICascadeSoftDeleteService cascadeSoftDeleteService = scope.ServiceProvider.GetRequiredService<ICascadeSoftDeleteService>();
            cascadeSoftDeleteService.SetSoftDelete(entity, true);
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

        public static void UpdateEntity<T>(WebApplicationFactory<Program> webApplicationFactory, T entity)
            where T : EntityBase
        {
            using var scope = webApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Update(entity);
            dbContext.SaveChanges();
        }

        public static async Task SetEmailVerifyCodeAsync(WebApplicationFactory<Program> applicationFactory, string code, string email)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IEmailVerifyService emailVerifyService = scope.ServiceProvider.GetRequiredService<IEmailVerifyService>();
            await emailVerifyService.SetVerifyCodeAsync(email, code, CancellationToken.None);
        }

        public static async Task SetVerifiedEmailAsync(WebApplicationFactory<Program> applicationFactory, string email)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IEmailVerifyService emailVerifyService = scope.ServiceProvider.GetRequiredService<IEmailVerifyService>();
            await emailVerifyService.SetVerifyCodeAsync(email, "code", CancellationToken.None);
            await emailVerifyService.VerifyEmailCodeAsync("code", CancellationToken.None);
        }

        public static void LoadCollection<T, V>(WebApplicationFactory<Program> applicationFactory, T entity, Expression<Func<T, IEnumerable<V>>> navigationExp)
            where T : EntityBase
            where V : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Set<T>().Entry(entity).Collection(navigationExp).Query().IgnoreSoftDeleteFilter().Load();
        }
    }
}
