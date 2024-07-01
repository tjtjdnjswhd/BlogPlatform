namespace BlogPlatform.Shared.Services
{
    public class MailSendContext
    {
        public MailSendContext(string? fromId, string receiverName, string to, string subject, string body)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(receiverName, nameof(receiverName));
            ArgumentException.ThrowIfNullOrWhiteSpace(to, nameof(to));
            ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
            ArgumentException.ThrowIfNullOrWhiteSpace(body, nameof(body));

            FromId = fromId;
            ReceiverName = receiverName;
            To = to;
            Subject = subject;
            Body = body;
        }

        public string? FromId { get; init; }

        public string ReceiverName { get; init; }

        public string To { get; init; }

        public string Subject { get; init; }

        public string Body { get; init; }
    }
}
