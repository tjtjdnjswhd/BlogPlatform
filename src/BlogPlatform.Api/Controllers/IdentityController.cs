using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.interfaces;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel;
using System.Diagnostics;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IIdentityService identityService, ILogger<IdentityController> logger)
        {
            _identityService = identityService;
            _logger = logger;
        }

        [HttpPost("login/basic")]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, CancellationToken cancellationToken, [FromHeader] bool setCookie = false)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, setCookie);
        }

        [HttpPost("signup/basic")]
        public async Task<IActionResult> BasicSignUpAsync([FromBody] BasicSignUpInfo signUpInfo, CancellationToken cancellationToken, [FromHeader] bool setCookie = false)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            return HandleSignUp(signUpResult, user, setCookie);
        }

        [HttpPost("login/oauth")]
        public IActionResult OAuthLogin([FromForm] string provider, [FromHeader] bool setCookie = false)
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
        public async Task<IActionResult> OAuthLoginCallbackAsync(OAuthLoginInfo loginInfo, [FromQuery] bool setCookie)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo);
            Debug.Assert(loginResult != ELoginResult.WrongPassword); // OAuth 로그인 시 비밀번호가 틀릴 수 없음
            return HandleLogin(loginResult, user, setCookie);
        }

        [HttpPost("signup/oauth")]
        public IActionResult OAuthSignUp([FromForm] string provider, [FromForm] string name, [FromHeader] bool setCookie)
        {
            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{Url.Action(nameof(OAuthSignUpCallbackAsync), "Identity")}?setcookie={setCookie}",
                IssuedUtc = DateTimeOffset.UtcNow,
            };

            return Challenge(authenticationProperties, provider);
        }

        [HttpGet("signup/oauth")]
        [OAuthAuthorize]
        public async Task<IActionResult> OAuthSignUpCallbackAsync(OAuthSignUpInfo signUpInfo, [FromQuery] bool setCookie)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo);
            return HandleSignUp(signUpResult, user, setCookie);
        }

        [HttpPost("logout")]
        [Authorize]
        public LogoutResult Logout() => new();

        [HttpPost("refresh")]
        public RefreshResult Refresh() => new RefreshResult();

        private IActionResult HandleLogin(ELoginResult loginResult, User? user, bool setCookie)
        {
            switch (loginResult)
            {
                case ELoginResult.Success:
                    Debug.Assert(user is not null); // 로그인 성공 시 user는 null이 아니어야 함
                    return new LoginSuccessResult(user, setCookie);

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
                    return new LoginSuccessResult(user, setCookie);

                case ESignUpResult.IdDuplicate:
                    return Conflict();

                case ESignUpResult.NameDuplicate:
                    return Conflict();

                case ESignUpResult.EmailDuplicate:
                    return Conflict();

                case ESignUpResult.AlreadyExists:
                    return Conflict();

                case ESignUpResult.ProviderNotFound:
                    return NotFound();

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(signUpResult), (int)signUpResult, typeof(ESignUpResult));
            }
        }
    }
}
