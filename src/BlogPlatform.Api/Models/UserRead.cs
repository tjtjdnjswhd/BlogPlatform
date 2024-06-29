using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Models
{
    public record UserRead(int Id, string? AccountId, [Required] string Name, [Required] string Email, DateTimeOffset CreatedAt, int? BlogId, IEnumerable<string> RoleNames, IEnumerable<string> OAuthProviders)
    {
        public string? BlogUri { get; set; }
    }
}
