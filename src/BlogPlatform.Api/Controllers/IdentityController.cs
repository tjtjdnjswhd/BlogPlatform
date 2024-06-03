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
        private readonly IVerifyEmailService _verifyEmailService;
        private readonly IPasswordResetMailService _passwordResetService;
        private readonly IFindAccountIdMailService _findAccountIdMailService;
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IIdentityService identityService, IVerifyEmailService verifyEmailService, IPasswordResetMailService passwordResetService, IFindAccountIdMailService findAccountIdMailService, ILogger<IdentityController> logger)
        {
            _identityService = identityService;
            _verifyEmailService = verifyEmailService;
            _passwordResetService = passwordResetService;
            _findAccountIdMailService = findAccountIdMailService;
            _logger = logger;
        }

        [HttpPost("login/basic")]
        [PasswordChangeRequiredFilter(nameof(loginInfo))]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, CancellationToken cancellationToken, [TokenSetCookie] bool setCookie = false)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, setCookie);
        }

        [HttpPost("signup/basic")]
        [SignUpEmailVerificationFilter(nameof(signUpInfo))]
        public async Task<IActionResult> BasicSignUpAsync([FromBody] BasicSignUpInfo signUpInfo, CancellationToken cancellationToken, [TokenSetCookie] bool setCookie = false)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            Debug.Assert(signUpResult != ESignUpResult.OAuthAlreadyExists); // OAuth 계정이 아닌 경우 OAuthAlreadyExists가 나올 수 없음
            return HandleSignUp(signUpResult, user, setCookie);
        }

        [HttpPost("signup/basic/email")]
        public async Task<IActionResult> SendVerifyEmailAsync([FromForm, EmailAddress] string email, CancellationToken cancellationToken)
        {
            await _verifyEmailService.SendEmailVerificationAsync(email, cancellationToken);
            return Ok();
        }

        [HttpGet("signup/basic/email")]
        public async Task<IActionResult> VerifyEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _verifyEmailService.VerifyEmailCodeAsync(code, cancellationToken);
            return email is not null ? Ok() : BadRequest(new Error("잘못된 코드입니다."));
        }

        [HttpPost("login/oauth")]
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
        public async Task<IActionResult> OAuthLoginCallbackAsync(OAuthLoginInfo loginInfo, [FromQuery] bool setCookie, CancellationToken cancellationToken)
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
        public async Task<IActionResult> OAuthSignUpCallbackAsync(OAuthSignUpInfo signUpInfo, [FromQuery] bool setCookie, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            return HandleSignUp(signUpResult, user, setCookie);
        }

        [HttpGet("oauth")]
        [UserAuthorize]
        public IActionResult AddOAuth([FromForm] string provider)
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
        public async Task<IActionResult> AddOAuthCallbackAsync(OAuthLoginInfo info, CancellationToken cancellationToken)
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

        [HttpDelete("oauth")]
        [UserAuthorize]
        public async Task<IActionResult> RemoveOAuthAsync([FromForm] string provider, CancellationToken cancellationToken)
        {
            ERemoveOAuthResult removeOAuthResult = await _identityService.RemoveOAuthAsync(User, provider, cancellationToken);
            switch (removeOAuthResult)
            {
                case ERemoveOAuthResult.Success:
                    return Ok();

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
        public LogoutResult Logout() => new();

        [HttpPost("refresh")]
        public RefreshResult Refresh() => new();

        [HttpPost("password/change")]
        [UserAuthorize]
        public async Task<IActionResult> ChangePasswordAsync([FromForm, AccountPasswordValidate] string newPassword, CancellationToken cancellationToken)
        {
            bool isUserExist = await _identityService.ChangePasswordAsync(User, newPassword, cancellationToken);
            return isUserExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPasswordAsync(string email, CancellationToken cancellationToken)
        {
            string? newPassword = await _identityService.ResetPasswordAsync(email, cancellationToken);
            if (newPassword is null)
            {
                return NotFound(new Error("존재하지 않는 계정의 이메일입니다."));
            }

            _passwordResetService.SendResetPasswordEmail(email, newPassword);
            return Ok();
        }

        [HttpPost("name")]
        [UserAuthorize]
        public async Task<IActionResult> ChangeNameAsync([FromForm, UserNameValidate] string name, CancellationToken cancellationToken)
        {
            bool isExist = await _identityService.ChangeNameAsync(User, name, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("id/find")]
        public async Task<IActionResult> FindIdAsync([FromForm, EmailAddress] string email, CancellationToken cancellationToken)
        {
            string? accountId = await _identityService.FindAccountIdAsync(email, cancellationToken);
            if (accountId is null)
            {
                return NotFound();
            }

            _findAccountIdMailService.SendMail(email, accountId);
            return Ok();
        }

        [HttpPost("withdraw")]
        [UserAuthorize]
        public async Task<IActionResult> WithDrawAsync(CancellationToken cancellationToken)
        {
            bool isExist = await _identityService.WithDrawAsync(User, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("withdraw/cancel")]
        [UserAuthorize]
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
        public async Task<IActionResult> ChangeEmailAsync([FromForm, EmailAddress] string newEmail, CancellationToken cancellationToken)
        {
            await _verifyEmailService.SendEmailVerificationAsync(newEmail, cancellationToken);
            return Ok();
        }

        [HttpGet("email/change/confirm")]
        [UserAuthorize]
        public async Task<IActionResult> ConfirmChangeEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _verifyEmailService.VerifyEmailCodeAsync(code, cancellationToken);
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
