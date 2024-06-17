using BlogPlatform.Api.Identity.Attributes;

namespace BlogPlatform.Api.Models
{
    public record UserNameModel([UserNameValidate] string Name);
}
