namespace BlogPlatform.Api.Models
{
    public record PostCreate(string Title, string Content, List<string> Tags, int CategoryId);
}
