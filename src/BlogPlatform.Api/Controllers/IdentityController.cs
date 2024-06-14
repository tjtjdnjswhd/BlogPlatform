﻿using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IUserEmailService _userEmailService;
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IIdentityService identityService, IUserEmailService userEmailService, ILogger<IdentityController> logger)
        {
            _identityService = identityService;
            _userEmailService = userEmailService;
            _logger = logger;
        }

        [HttpPost("login/basic")]
        [PasswordChangeRequiredFilter(nameof(loginInfo))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, CancellationToken cancellationToken, [TokenSetCookie] bool setCookie = false)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, setCookie);
        }

        [HttpPost("signup/basic")]
        [SignUpEmailVerificationFilter(nameof(signUpInfo))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BasicSignUpAsync([FromBody] BasicSignUpInfo signUpInfo, CancellationToken cancellationToken, [TokenSetCookie] bool setCookie = false)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            Debug.Assert(signUpResult != ESignUpResult.OAuthAlreadyExists); // OAuth 계정이 아닌 경우 OAuthAlreadyExists가 나올 수 없음
            return HandleSignUp(signUpResult, user, setCookie);
        }

        [HttpPost("signup/basic/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SendVerifyEmailAsync([FromBody, EmailAddress] string email, CancellationToken cancellationToken)
        {
            string? verifyUri = Url.ActionLink(nameof(VerifyEmailAsync), "Identity");
            Debug.Assert(verifyUri is not null); // VerifyEmailAsync가 존재하므로 null이 아니어야 함
            await _userEmailService.SendEmailVerificationAsync(email, code => $"{verifyUri}&code={code}", cancellationToken);
            return Ok();
        }

        [HttpGet("signup/basic/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> VerifyEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _userEmailService.VerifyEmailCodeAsync(code, cancellationToken);
            return email is not null ? Ok() : BadRequest(new Error("잘못된 코드입니다."));
        }

        [HttpPost("login/oauth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult OAuthLogin([FromForm] string provider, [TokenSetCookie] bool setCookie = false)
        {
            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{Url.Action(nameof(OAuthLoginCallbackAsync), "Identity")}?setcookie={setCookie}",
                IssuedUtc = DateTimeOffset.UtcNow,
            };

            _logger.LogDebug("Login with OAuth. provider: {provider} SetCookie: {setCookie}", provider, setCookie);
            return Challenge(authenticationProperties, provider);
        }

        [HttpGet("login/oauth")]
        [OAuthAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> OAuthLoginCallbackAsync([FromSpecial] OAuthLoginInfo loginInfo, [FromQuery] bool setCookie, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            Debug.Assert(loginResult != ELoginResult.WrongPassword); // OAuth 로그인 시 비밀번호가 틀릴 수 없음
            return HandleLogin(loginResult, user, setCookie);
        }

        [HttpPost("signup/oauth")]
        public IActionResult OAuthSignUp([FromForm] string provider, [FromForm] string name, [TokenSetCookie] bool setCookie = false)
        {
            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{Url.Action(nameof(OAuthSignUpCallbackAsync), "Identity")}?setcookie={setCookie}",
                IssuedUtc = DateTimeOffset.UtcNow,
            };

            return new OAuthSignUpChallengeResult(authenticationProperties, provider, name);
        }

        [HttpGet("signup/oauth")]
        [OAuthAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> OAuthSignUpCallbackAsync([FromSpecial] OAuthSignUpInfo signUpInfo, [FromQuery] bool setCookie, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            return HandleSignUp(signUpResult, user, setCookie);
        }

        [HttpGet("oauth")]
        [UserAuthorize]
        public IActionResult AddOAuth([FromQuery] string provider)
        {
            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                IsPersistent = false,
                RedirectUri = Url.Action(nameof(AddOAuthCallbackAsync), "Identity"),
                IssuedUtc = DateTimeOffset.UtcNow,
            };

            return Challenge(authenticationProperties, provider);
        }

        [HttpPost("oauth")]
        [OAuthAuthorize]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> AddOAuthCallbackAsync([FromSpecial] OAuthLoginInfo info, CancellationToken cancellationToken)
        {
            EAddOAuthResult addOAuthResult = await _identityService.AddOAuthAsync(HttpContext, info, cancellationToken);
            switch (addOAuthResult)
            {
                case EAddOAuthResult.Success:
                    return Ok();

                case EAddOAuthResult.UserNotFound:
                    return new AuthenticatedUserDataNotFoundResult();

                case EAddOAuthResult.UserAlreadyHasOAuth:
                    return Conflict(new Error("동일한 OAuth 제공자를 가지고 있습니다."));

                case EAddOAuthResult.OAuthAlreadyExists:
                    return Conflict(new Error("이미 사용하는 OAuth 계정입니다."));

                case EAddOAuthResult.ProviderNotFound:
                    return NotFound(new Error("잘못된 OAuth 제공자입니다."));

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(addOAuthResult), (int)addOAuthResult, typeof(EAddOAuthResult));
            }
        }

        [HttpDelete("oauth/{provider:alpha}")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RemoveOAuthAsync([FromRoute] string provider, CancellationToken cancellationToken)
        {
            ERemoveOAuthResult removeOAuthResult = await _identityService.RemoveOAuthAsync(User, provider, cancellationToken);
            switch (removeOAuthResult)
            {
                case ERemoveOAuthResult.Success:
                    return Ok();

                case ERemoveOAuthResult.HasSingleAccount:
                    return Conflict(new Error("연결 계정이 1개일 경우 삭제할 수 없습니다."));

                case ERemoveOAuthResult.UserNotFound:
                    return new AuthenticatedUserDataNotFoundResult();

                case ERemoveOAuthResult.OAuthNotFound:
                    return NotFound(new Error("해당 OAuth 제공자를 사용하고 있지 않습니다."));

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(removeOAuthResult), (int)removeOAuthResult, typeof(ERemoveOAuthResult));
            }
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesDefaultResponseType]
        public LogoutResult Logout() => new();

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public RefreshResult Refresh() => new();

        [HttpPost("password/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangePasswordAsync([FromForm, AccountPasswordValidate] string newPassword, CancellationToken cancellationToken)
        {
            bool isUserExist = await _identityService.ChangePasswordAsync(User, newPassword, cancellationToken);
            return isUserExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("password/reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ResetPasswordAsync(string email, CancellationToken cancellationToken)
        {
            string? newPassword = await _identityService.ResetPasswordAsync(email, cancellationToken);
            if (newPassword is null)
            {
                return NotFound(new Error("존재하지 않는 계정의 이메일입니다."));
            }

            _userEmailService.SendPasswordResetMail(email, newPassword, CancellationToken.None);
            return Ok();
        }

        [HttpPost("name")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangeNameAsync([FromForm, UserNameValidate] string name, CancellationToken cancellationToken)
        {
            bool isExist = await _identityService.ChangeNameAsync(User, name, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("id/find")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> FindIdAsync([FromForm, EmailAddress] string email, CancellationToken cancellationToken)
        {
            string? accountId = await _identityService.FindAccountIdAsync(email, cancellationToken);
            if (accountId is null)
            {
                return NotFound();
            }

            _userEmailService.SendAccountIdMail(email, accountId, cancellationToken);
            return Ok();
        }

        [HttpPost("withdraw")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> WithDrawAsync(CancellationToken cancellationToken)
        {
            bool isExist = await _identityService.WithDrawAsync(User, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("withdraw/cancel")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CancelWithDrawAsync(CancellationToken cancellationToken)
        {
            ECancelWithDrawResult result = await _identityService.CancelWithDrawAsync(User, cancellationToken);
            return result switch
            {
                ECancelWithDrawResult.Success => Ok(),
                ECancelWithDrawResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                ECancelWithDrawResult.Expired => BadRequest(new Error("탈퇴 요청이 만료되었습니다.")),
                ECancelWithDrawResult.WithDrawNotRequested => BadRequest(new Error("탈퇴하지 않은 계정입니다.")),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(ECancelWithDrawResult))
            };
        }

        [HttpPost("email/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangeEmailAsync([FromForm, EmailAddress] string newEmail, CancellationToken cancellationToken)
        {
            string? confirmUri = Url.ActionLink(nameof(ConfirmChangeEmailAsync), "Identity");
            Debug.Assert(confirmUri is not null); // ConfirmChangeEmailAsync가 존재하므로 null이 아니어야 함
            await _userEmailService.SendEmailVerificationAsync(newEmail, code => $"{confirmUri}&code={code}", cancellationToken);
            return Ok();
        }

        [HttpGet("email/change/confirm")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ConfirmChangeEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _userEmailService.VerifyEmailCodeAsync(code, cancellationToken);
            if (email is null)
            {
                return BadRequest(new Error("잘못된 코드입니다."));
            }

            bool isExist = await _identityService.ChangeEmailAsync(User, email, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        private IActionResult HandleLogin(ELoginResult loginResult, User? user, bool setCookie)
        {
            switch (loginResult)
            {
                case ELoginResult.Success:
                    Debug.Assert(user is not null); // 로그인 성공 시 user는 null이 아니어야 함
                    return new LoginResult(user, setCookie);

                case ELoginResult.NotFound:
                    return NotFound();

                case ELoginResult.WrongPassword:
                    return Unauthorized();

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult));
            }
        }

        private IActionResult HandleSignUp(ESignUpResult signUpResult, User? user, bool setCookie)
        {
            switch (signUpResult)
            {
                case ESignUpResult.Success:
                    Debug.Assert(user is not null); // 가입 성공 시 user는 null이 아니어야 함
                    return new LoginResult(user, setCookie);

                case ESignUpResult.UserIdAlreadyExists:
                    return Conflict(new Error("중복된 Id입니다."));

                case ESignUpResult.NameAlreadyExists:
                    return Conflict(new Error("중복된 이름입니다."));

                case ESignUpResult.EmailAlreadyExists:
                    return Conflict(new Error("중복된 이메일입니다."));

                case ESignUpResult.OAuthAlreadyExists:
                    return Conflict(new Error("이미 존재하는 계정입니다."));

                case ESignUpResult.ProviderNotFound:
                    return NotFound(new Error("잘못된 OAuth 제공자입니다."));

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(signUpResult), (int)signUpResult, typeof(ESignUpResult));
            }
        }
    }
}
