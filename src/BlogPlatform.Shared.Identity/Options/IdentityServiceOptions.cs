using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Options
{
    public class IdentityServiceOptions
    {
        [Required(AllowEmptyStrings = false)]
        public required string UserRoleName { get; set; }
    }
}
