namespace BlogPlatform.Shared.Models.Post
{
    public record PostCreate(string Title, string Content, List<string> Tags, int CategoryId);
}
