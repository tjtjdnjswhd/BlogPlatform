namespace BlogPlatform.Api.Identity.Models
{
    public enum EAddOAuthResult
    {
        Success,
        UserNotFound,
        UserAlreadyHasOAuth,
        OAuthAlreadyExists,
        ProviderNotFound
    }
}
