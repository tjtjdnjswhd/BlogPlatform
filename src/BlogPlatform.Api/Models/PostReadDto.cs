namespace BlogPlatform.Api.Models
{
    public record PostReadDto(int Id, string Title, string Content, int CategoryId);
}
