namespace BlogPlatform.Api.Identity.Services.Interfaces
{
    public interface IReturnUrlWhitelistService
    {
        bool IsWhitelisted(string returnUrl);
    }
}