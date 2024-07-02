using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BlogPlatform.Api.Identity.Extensions
{
    public static class JwtSignInExtensions
    {
        public static AuthenticationBuilder AddJwtSignIn(this AuthenticationBuilder builder, Action<JwtBearerOptions> bearerOptions)
        {
            return builder.AddScheme<JwtBearerOptions, JwtSignInHandler>(JwtSignInHandler.AuthenticationScheme, bearerOptions);
        }
    }
}
