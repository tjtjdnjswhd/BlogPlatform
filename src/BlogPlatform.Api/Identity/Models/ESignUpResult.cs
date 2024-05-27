namespace BlogPlatform.Api.Identity.Models
{
    public enum ESignUpResult
    {
        Success,
        UserIdAlreadyExists,
        NameAlreadyExists,
        EmailAlreadyExists,
        ProviderNotFound,
        OAuthAlreadyExists
    }
}
