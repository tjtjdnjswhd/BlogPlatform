namespace BlogPlatform.Api.Identity.Models
{
    public enum ESignUpResult
    {
        Success,
        IdDuplicate,
        NameDuplicate,
        EmailDuplicate,
        ProviderNotFound,
        AlreadyExists
    }
}
