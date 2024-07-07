namespace BlogPlatform.Shared.Models.User
{
    public record UserRead(int Id, string? AccountId, string Name, string Email, DateTimeOffset CreatedAt, int? BlogId, IEnumerable<string> RoleNames, IEnumerable<string> OAuthProviders);
}
