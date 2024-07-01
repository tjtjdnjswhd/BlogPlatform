using BlogPlatform.Shared.Identity.Validations;

namespace BlogPlatform.Shared.Identity.Models
{
    public record UserNameModel([UserNameValidate] string Name);
}
