using BlogPlatform.Shared.Options;
using BlogPlatform.Shared.Services;
using BlogPlatform.Shared.Services.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlogPlatform.Shared.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMailSender(this IServiceCollection services, IConfigurationSection mailSenderOptions)
        {
            services.AddScoped<IMailSender, MailSender>();
            services.Configure<MailSenderOptions>(mailSenderOptions);
            return services;
        }

        public static IServiceCollection AddPostImageService(this IServiceCollection services)
        {
            services.AddScoped<IPostImageService, PostImageService>();
            return services;
        }
    }
}
