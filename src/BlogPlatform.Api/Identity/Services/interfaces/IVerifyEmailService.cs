namespace BlogPlatform.Api.Identity.Services.interfaces
{
    public interface IVerifyEmailService
    {
        Task SendEmailVerificationAsync(string email, CancellationToken cancellationToken = default);

        Task<string?> VerifyEmailCodeAsync(string code, CancellationToken cancellationToken = default);

        Task<bool> IsVerifyAsync(string email, CancellationToken cancellationToken = default);
    }
}
