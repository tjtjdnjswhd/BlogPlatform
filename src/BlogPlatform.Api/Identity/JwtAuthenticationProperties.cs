using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Authentication;

namespace BlogPlatform.Api.Identity
{
    public class JwtAuthenticationProperties : AuthenticationProperties
    {
        public JwtAuthenticationProperties(string? redirectUri)
        {
            RedirectUri = redirectUri;
            IsSignInCookie = redirectUri != null;
        }

        public JwtAuthenticationProperties(bool isSignInCookie)
        {
            IsSignInCookie = isSignInCookie;
        }

        public bool IsSignInCookie
        {
            get => GetParameter<bool>(AuthenticationPropertiesParameterKeys.IsSignInCookie);
            private set => SetParameter(AuthenticationPropertiesParameterKeys.IsSignInCookie, value);
        }
    }
}
