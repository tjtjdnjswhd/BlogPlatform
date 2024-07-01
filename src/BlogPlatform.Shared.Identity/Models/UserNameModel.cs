using BlogPlatform.Shared.Identity.Attributes;

namespace BlogPlatform.Shared.Identity.Models
{
    public record UserNameModel([UserNameValidate] string Name);
}
