namespace BlogPlatform.Api.Models
{
    public record SendMailModel(string Subject, string Body, List<int>? UserIds);
}
