using BlogPlatform.EFCore.Models;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Options
{
    public class UserClaimsIdentityFactoryOptions
    {
        [Required]
        public required Func<IServiceProvider, User, CancellationToken, Task<IEnumerable<Claim>>> ClaimsFactoryFunc { get; set; }

        public string? RoleClaimType { get; set; }

        public string? NameClaimType { get; set; }

        [Required]
        public required string AuthenticationType { get; set; }

        [Required]
        public required Func<IEnumerable<string>, Claim> ToRoleClaimFunc { get; set; }

        [Required]
        public required Func<Claim, IEnumerable<string>> FromRoleClaimFunc { get; set; }
    }
}
