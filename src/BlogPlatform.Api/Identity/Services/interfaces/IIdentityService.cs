using BlogPlatform.Api.Identity.Models;
using BlogPlatform.EFCore.Models;

namespace BlogPlatform.Api.Identity.Services.interfaces
{
    public interface IIdentityService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(ELoginResult, User?)> LoginAsync(BasicLoginInfo loginInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(ELoginResult, User?)> LoginAsync(OAuthLoginInfo loginInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signUpInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(ESignUpResult, User?)> SignUpAsync(BasicSignUpInfo signUpInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signUpInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(ESignUpResult, User?)> SignUpAsync(OAuthSignUpInfo signUpInfo, CancellationToken cancellationToken = default);
    }
}