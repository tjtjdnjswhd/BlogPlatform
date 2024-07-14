using BlogPlatform.Shared.Identity.Validations;

namespace BlogPlatform.Shared.Identity.Models
{
    public record PasswordChangeModel([UserNameValidate] string Id, [AccountPasswordValidate] string CurrentPassword, [AccountPasswordValidate] string NewPassword);
}
