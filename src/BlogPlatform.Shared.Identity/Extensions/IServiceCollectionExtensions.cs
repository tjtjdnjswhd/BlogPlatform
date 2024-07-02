using BlogPlatform.Shared.Identity.Options;
using BlogPlatform.Shared.Identity.Services;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPlatform.Shared.Identity.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentityService(this IServiceCollection services, IConfigurationSection identityServiceOptionsSection)
        {
            services.AddScoped<IIdentityService, IdentityService>();
            services.Configure<IdentityServiceOptions>(identityServiceOptionsSection);
            return services;
        }

        public static IServiceCollection AddUserEmailService(this IServiceCollection services, IConfigurationSection userEmailOptionsSection)
        {
            services.AddScoped<IUserEmailService, UserEmailService>();
            services.Configure<UserEmailOptions>(userEmailOptionsSection);
            return services;
        }

        public static IServiceCollection AddEmailVerifyService(this IServiceCollection services)
        {
            services.AddScoped<IEmailVerifyService, EmailVerifyService>();
            return services;
        }

        public static IServiceCollection AddAccountOptions(this IServiceCollection services, IConfigurationSection accountOptionsSection)
        {
            services.Configure<AccountOptions>(accountOptionsSection);
            return services;
        }
    }
}
