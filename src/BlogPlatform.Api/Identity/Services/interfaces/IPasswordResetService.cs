namespace BlogPlatform.Api.Identity.Services.interfaces
{
    public interface IPasswordResetService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string?> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        void SendResetPasswordEmail(string email, string newPassword, CancellationToken cancellationToken = default);
    }
}
