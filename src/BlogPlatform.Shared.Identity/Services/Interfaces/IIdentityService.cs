using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

using System.Security.Claims;

namespace BlogPlatform.Shared.Identity.Services.Interfaces
{
    /// <summary>
    /// DB의 유저 계정 관련 작업을 수행하는 서비스
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Id/pw 로그인 요청에 대한 결과를 반환합니다
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 로그인 성공 시 <see cref="ELoginResult.Success"/>, 유저를 반환합니다.
        /// 실패 시 유저는 null을 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<(ELoginResult, User?)> LoginAsync(BasicLoginInfo loginInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// OAuth 로그인 요청에 대한 결과를 반환합니다
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 로그인 성공 시 <see cref="ELoginResult.Success"/>, 유저를 반환합니다.
        /// 실패 시 유저는 null을 반환합니다.
        /// <see cref="ELoginResult.WrongPassword"/>를 반환하지 않습니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<(ELoginResult, User?)> LoginAsync(OAuthLoginInfo loginInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Id/pw 회원가입 요청에 대한 결과를 반환합니다
        /// </summary>
        /// <param name="signUpInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 가입 성공 시 <see cref="ESignUpResult.Success"/>, 유저를 반환합니다.
        /// 실패 시 유저는 null을 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<(ESignUpResult, User?)> SignUpAsync(BasicSignUpInfo signUpInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// OAuth 회원가입 요청에 대한 결과를 반환합니다
        /// </summary>
        /// <param name="signUpInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 가입 성공 시 <see cref="ESignUpResult.Success"/>, 유저를 반환합니다.
        /// 실패 시 유저는 null을 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<(ESignUpResult, User?)> SignUpAsync(OAuthSignUpInfo signUpInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// 현재 요청에 대한 유저의 계정에 OAuth 계정을 추가합니다
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oAuthInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 요청의 성공 여부를 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<EAddOAuthResult> AddOAuthAsync(int userId, OAuthLoginInfo oAuthInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// 현 사용자의 OAuth 계정을 제거합니다
        /// </summary>
        /// <param name="user"></param>
        /// <param name="provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 요청의 성공 여부를 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<ERemoveOAuthResult> RemoveOAuthAsync(ClaimsPrincipal user, string provider, CancellationToken cancellationToken = default);

        /// <summary>
        /// 현 사용자의 비밀번호를 변경합니다
        /// </summary>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 유저가 존재하지 않을 경우 false를 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<bool> ChangePasswordAsync(ClaimsPrincipal user, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 현 사용자의 이름을 변경합니다
        /// </summary>
        /// <param name="user"></param>
        /// <param name="newName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 유저가 존재하지 않을 시 false를 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<bool> ChangeNameAsync(ClaimsPrincipal user, string newName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 <paramref name="email"/>을 가진 유저의 이메일을 초기화합니다
        /// </summary>
        /// <param name="email">비밀번호를 초기화 할 유저의 이메일</param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 해당 계정이 존재할 시 초기화된 비밀번호를 반환합니다.
        /// 계정이 존재하지 않을 경우 null을 반환합니다
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<string?> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 <paramref name="email"/>을 가진 유저의 Id를 반환합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 해당 계정이 존재할 시 Id를 반환합니다.
        /// 존재하지 않거나, OAuth 계정인 경우 null을 반환합니다
        /// </returns>
        Task<string?> FindAccountIdAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 유저의 계정을 삭제합니다
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<EWithDrawResult> WithDrawAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 유저의 계정 삭제를 취소합니다
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ECancelWithDrawResult> CancelWithDrawAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 유저의 이메일을 변경합니다
        /// </summary>
        /// <param name="user"></param>
        /// <param name="newEmail"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 
        /// </returns>
        Task<bool> ChangeEmailAsync(ClaimsPrincipal user, string newEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// 유저 Id를 반환합니다.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        bool TryGetUserId(ClaimsPrincipal user, out int userId);
    }
}