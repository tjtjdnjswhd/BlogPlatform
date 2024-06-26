using BlogPlatform.Api.Identity.Attributes;

namespace BlogPlatform.Api.Identity.Models
{
    public record UserNameModel([UserNameValidate] string Name);
}
