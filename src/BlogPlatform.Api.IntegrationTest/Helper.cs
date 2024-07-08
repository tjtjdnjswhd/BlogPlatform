using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Identity.Services.Options;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Linq.Expressions;
using System.Security.Claims;

namespace BlogPlatform.Api.IntegrationTest
{
    public static class Helper
    {
        public static void AddEntity<T>(WebApplicationFactory<Program> applicationFactory, T entity)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            dbContext.Add(entity);
            dbContext.SaveChanges();
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

        public static T? GetFirstEntityOrDefault<T>(WebApplicationFactory<Program> applicationFactory, bool ignoreSoftDelete = false)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            return ignoreSoftDelete ? dbContext.Set<T>().IgnoreSoftDeleteFilter().FirstOrDefault() : dbContext.Set<T>().FirstOrDefault();
        }

        public static T? GetFirstEntityOrDefault<T>(WebApplicationFactory<Program> applicationFactory, Expression<Func<T, bool>> predicate, bool ignoreSoftDelete = false)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            return ignoreSoftDelete ? dbContext.Set<T>().IgnoreSoftDeleteFilter().FirstOrDefault(predicate) : dbContext.Set<T>().FirstOrDefault(predicate);
        }

        public static bool IsExist<T>(WebApplicationFactory<Program> applicationFactory, Expression<Func<T, bool>> predicate)
            where T : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            return dbContext.Set<T>().Any(predicate);
        }

        public static async Task<AuthorizeToken> GetAuthorizeTokenAsync(WebApplicationFactory<Program> applicationFactory, User user, bool setCookie = false)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            IUserClaimsPrincipalFactory<User> userClaimsPrincipalFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
            ClaimsPrincipal claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
            return authorizeTokenService.GenerateToken(claimsPrincipal, setCookie);
        }

        public static void SetAuthorizationHeader(HttpClient client, AuthorizeToken token)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        }

        public static void SetAuthorizeTokenCookie(WebApplicationFactory<Program> applicationFactory, HttpClient client, AuthorizeToken token)
        {
            using var scope = applicationFactory.Services.CreateScope();
            AuthorizeTokenOptions authorizeTokenOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthorizeTokenOptions>>().Value;
            client.DefaultRequestHeaders.Add("cookie", $"{authorizeTokenOptions.AccessTokenName}={token.AccessToken}; {authorizeTokenOptions.RefreshTokenName}={token.RefreshToken}");
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
            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            await authorizeTokenService.CacheTokenAsync(authorizeToken, CancellationToken.None);
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
            await emailVerifyService.SetSignUpVerifyCodeAsync(email, code, CancellationToken.None);
        }

        public static async Task SetVerifiedEmailAsync(WebApplicationFactory<Program> applicationFactory, string email)
        {
            using var scope = applicationFactory.Services.CreateScope();
            IEmailVerifyService emailVerifyService = scope.ServiceProvider.GetRequiredService<IEmailVerifyService>();
            await emailVerifyService.SetSignUpVerifyCodeAsync(email, "code", CancellationToken.None);
            await emailVerifyService.VerifySignUpEmailCodeAsync("code", CancellationToken.None);
        }

        public static void LoadCollection<T, V>(WebApplicationFactory<Program> applicationFactory, T entity, Expression<Func<T, IEnumerable<V>>> navigationExp, bool ignoreSoftDelete = false)
            where T : EntityBase
            where V : EntityBase
        {
            using var scope = applicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();

            CollectionEntry<T, V> collectionEntry = dbContext.Set<T>().Entry(entity).Collection(navigationExp);
            if (ignoreSoftDelete)
            {
                collectionEntry.Query().IgnoreSoftDeleteFilter().Load();
            }
            else
            {
                collectionEntry.Load();
            }
        }

        public static TContext GetNotLoggingDbContext<TContext>(IServiceProvider services)
            where TContext : DbContext
        {
            return CreateDbContext<TContext>(services, builder => builder.UseLoggerFactory(LoggerFactory.Create(lb => lb.ClearProviders())));
        }

        public static TContext CreateDbContext<TContext>(IServiceProvider services, Action<DbContextOptionsBuilder<TContext>> builder) where TContext : DbContext
        {
            DbContextOptions<TContext> dbContextOptions = services.GetRequiredService<DbContextOptions<TContext>>();
            DbContextOptionsBuilder<TContext> dbContextOptionsBuilder = new(dbContextOptions);
            builder(dbContextOptionsBuilder);

            TContext dbContext = (TContext?)ActivatorUtilities.CreateInstance(services, typeof(TContext), dbContextOptionsBuilder.Options) ?? throw new Exception();
            return dbContext;
        }
    }
}
