using BlogPlatform.Api.Identity.Options;

namespace BlogPlatform.Api.Identity.Extensions
{
    public static class IServiceCollectionService
    {
        public static IServiceCollection AddJwtIdentity(this IServiceCollection services, Action<JwtOptions> jwtOptions, Action<UserClaimsIdentityFactoryOptions> userClaimsIdentityFactoryOptions, Action<BasicAccountOptions> basicAccountOptions)
        {

        }
    }
}
