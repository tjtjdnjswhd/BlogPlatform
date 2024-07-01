namespace BlogPlatform.Shared.Models.Admin
{
    public record SendMailModel(string Subject, string Body, List<int>? UserIds);
}
