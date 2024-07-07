using Microsoft.AspNetCore.Authentication;

namespace BlogPlatform.Api.Identity
{
    public class JwtAuthenticationProperties : AuthenticationProperties
    {
        public const string IsSignInCookieKey = "SignInCookie";

        public JwtAuthenticationProperties(string? redirectUri)
        {
            RedirectUri = redirectUri;
            IsSignInCookie = redirectUri != null;
        }

        public bool IsSignInCookie
        {
            get => GetParameter<bool>(IsSignInCookieKey);
            private set => SetParameter(IsSignInCookieKey, value);
        }
    }
}
