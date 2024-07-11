using BlogPlatform.EFCore;

using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Helper
{
    public static class EFCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddDbServices(this IServiceCollection services, IConfiguration configuration)
        {
            string? blogPlatformConnectionString = configuration.GetConnectionString("BlogPlatform");
            string? blogPlatformImgConnectionString = configuration.GetConnectionString("BlogPlatformImg");

            ArgumentException.ThrowIfNullOrWhiteSpace(blogPlatformConnectionString, nameof(blogPlatformConnectionString));
            ArgumentException.ThrowIfNullOrWhiteSpace(blogPlatformImgConnectionString, nameof(blogPlatformImgConnectionString));

            services.AddDbContext<BlogPlatformDbContext>(options =>
            {
                options.UseMySql(blogPlatformConnectionString, MySqlServerVersion.LatestSupportedServerVersion);
            });

            services.AddDbContext<BlogPlatformImgDbContext>(options =>
            {
                options.UseMySql(blogPlatformImgConnectionString, MySqlServerVersion.LatestSupportedServerVersion);
            });

            services.AddScoped<ICascadeSoftDeleteService, CascadeSoftDeleteService>();

            return services;
        }
    }
}
