namespace BlogPlatform.Api.Services.Interfaces
{
    public interface IMailSender
    {
        void Send(string from, string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
