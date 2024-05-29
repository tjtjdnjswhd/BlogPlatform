using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.interfaces;
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
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IIdentityService identityService, IVerifyEmailService verifyEmailService, ILogger<IdentityController> logger)
        {
            _identityService = identityService;
            _verifyEmailService = verifyEmailService;
            _logger = logger;
        }

        [HttpPost("login/basic")]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, CancellationToken cancellationToken, [TokenSetCookie] bool setCookie = false)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, setCookie);
        }

        [HttpPost("signup/basic")]
        [CheckEmailVerifyFilter(nameof(signUpInfo))]
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
        public async Task<IActionResult> OAuthLoginCallbackAsync(OAuthInfo loginInfo, [FromQuery] bool setCookie, CancellationToken cancellationToken)
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
        public async Task<IActionResult> AddOAuthCallbackAsync(OAuthInfo info, CancellationToken cancellationToken)
        {
            EAddOAuthResult addOAuthResult = await _identityService.AddOAuthAsync(HttpContext, info, cancellationToken);
            switch (addOAuthResult)
            {
                case EAddOAuthResult.Success:
                    return Ok();

                case EAddOAuthResult.UserNotFound:
                    return NotFound();

                case EAddOAuthResult.UserAlreadyHasOAuth:
                    return Conflict(new Error("동일한 OAuth 제공자를 가지고 있습니다."));

                case EAddOAuthResult.OAuthAlreadyExists:
                    return Conflict(new Error("이미 사용하는 OAuth 계정입니다."));

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
                    return NotFound(new Error("잘못된 유저입니다."));

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
