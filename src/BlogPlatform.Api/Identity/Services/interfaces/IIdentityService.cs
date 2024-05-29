using BlogPlatform.Api.Identity.Models;
using BlogPlatform.EFCore.Models;

using System.Security.Claims;

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
        Task<(ELoginResult, User?)> LoginAsync(OAuthInfo loginInfo, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="oAuthInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<EAddOAuthResult> AddOAuthAsync(HttpContext httpContext, OAuthInfo oAuthInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ERemoveOAuthResult> RemoveOAuthAsync(ClaimsPrincipal user, string provider, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> ChangePasswordAsync(ClaimsPrincipal user, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="newName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> ChangeNameAsync(ClaimsPrincipal user, string newName, CancellationToken cancellationToken = default);
    }
}