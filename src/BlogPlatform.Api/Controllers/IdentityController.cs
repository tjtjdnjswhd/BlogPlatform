using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.ModelBinders;
using BlogPlatform.Api.QueryExtensions;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;
using BlogPlatform.Shared.Identity.Validations;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.User;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly IIdentityService _identityService;
        private readonly IUserEmailService _userEmailService;
        private readonly IEmailVerifyService _emailVerifyService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(BlogPlatformDbContext blogPlatformDbContext, IIdentityService identityService, IUserEmailService userEmailService, IEmailVerifyService emailVerifyService, TimeProvider timeProvider, ILogger<IdentityController> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _identityService = identityService;
            _userEmailService = userEmailService;
            _emailVerifyService = emailVerifyService;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        [UserAuthorize]
        [HttpGet]
        public async Task<IActionResult> GetUserInfo([UserIdBind] int userId, CancellationToken cancellationToken)
        {
            UserRead? userRead = await _blogPlatformDbContext.Users
                .Where(u => u.Id == userId)
                .SelectUserRead()
                .FirstOrDefaultAsync(cancellationToken);

            if (userRead is null)
            {
                return new AuthenticatedUserDataNotFoundResult();
            }

            userRead.BlogUri = userRead.BlogId is not (null or 0) ? Url.ActionLink("Get", "Blog", new { id = userRead.BlogId }) : null;

            return Ok(userRead);
        }

        [HttpPost("login/basic")]
        [PasswordChangeRequiredFilter(nameof(loginInfo))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, loginInfo.ReturnUrl);
        }

        [HttpPost("signup/basic")]
        [SignUpEmailVerificationFilter(nameof(signUpInfo))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BasicSignUpAsync([FromBody] BasicSignUpInfo signUpInfo, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            Debug.Assert(signUpResult is not (ESignUpResult.ProviderNotFound or ESignUpResult.OAuthAlreadyExists)); // OAuth 계정이 아닌 경우 OAuthAlreadyExists가 나올 수 없음
            return HandleSignUp(signUpResult, user, signUpInfo.ReturnUrl);
        }

        [HttpPost("signup/basic/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SendVerifyEmailAsync([FromBody] EmailModel newEmail, CancellationToken cancellationToken)
        {
            string? verifyUri = Url.ActionLink("VerifyEmail", "Identity");
            Debug.Assert(verifyUri is not null); // VerifyEmailAsync가 존재하므로 null이 아니어야 함
            string code = _emailVerifyService.GenerateVerificationCode();
            await _emailVerifyService.SetVerifyCodeAsync(newEmail.Email, code, cancellationToken);
            _userEmailService.SendEmailVerifyMail(newEmail.Email, $"{verifyUri}&code={code}", cancellationToken);
            return Ok();
        }

        [HttpGet("signup/basic/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> VerifyEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _emailVerifyService.VerifyEmailCodeAsync(code, cancellationToken);
            return email is not null ? Ok() : BadRequest(new Error("잘못된 코드입니다"));
        }

        [HttpPost("login/oauth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult OAuthLogin([FromForm, Required(AllowEmptyStrings = false)] string provider, [FromQuery] string? returnUrl)
        {
            string? redirectUri = Url.Action("OAuthLoginCallback", "Identity");
            Debug.Assert(redirectUri is not null);

            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{redirectUri}?{(returnUrl is null ? string.Empty : $"&returnUrl={returnUrl}")}",
                IssuedUtc = _timeProvider.GetUtcNow(),
            };

            _logger.LogDebug("Login with OAuth. provider: {provider}", provider);
            return Challenge(authenticationProperties, provider);
        }

        [HttpGet("login/oauth")]
        [OAuthAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> OAuthLoginCallbackAsync([FromSpecial] OAuthLoginInfo loginInfo, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            Debug.Assert(loginResult != ELoginResult.WrongPassword); // OAuth 로그인 시 비밀번호가 틀릴 수 없음
            return HandleLogin(loginResult, user, returnUrl);
        }

        [HttpPost("signup/oauth")]
        public IActionResult OAuthSignUp([FromForm, Required(AllowEmptyStrings = false)] string provider, [FromForm, UserNameValidate] string name, [FromQuery] string? returnUrl)
        {
            string? redirectUri = Url.Action("OAuthSignUpCallback", "Identity");
            Debug.Assert(redirectUri is not null);

            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{redirectUri}?{(returnUrl is null ? string.Empty : $"&returnUrl={returnUrl}")}",
                IssuedUtc = _timeProvider.GetUtcNow(),
            };

            OAuthSignUpModel signUpModel = new(provider, name);
            return new OAuthSignUpChallengeResult(authenticationProperties, signUpModel);
        }

        [HttpGet("signup/oauth")]
        [OAuthAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> OAuthSignUpCallbackAsync([FromSpecial] OAuthSignUpInfo signUpInfo, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            return HandleSignUp(signUpResult, user, returnUrl);
        }

        [HttpPost("add/oauth")]
        [UserAuthorize]
        public IActionResult AddOAuth([FromForm] string provider, [FromQuery] string? returnUrl, [UserIdBind] int userId)
        {
            string? redirectUri = Url.Action("AddOAuthCallback", "Identity");
            Debug.Assert(redirectUri is not null);

            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{redirectUri}?userid={userId}{(returnUrl is null ? string.Empty : $"&returnUrl={returnUrl}")}",
                IssuedUtc = _timeProvider.GetUtcNow(),
            };

            return Challenge(authenticationProperties, provider);
        }

        [HttpGet("add/oauth")]
        [OAuthAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> AddOAuthCallbackAsync([FromSpecial] OAuthLoginInfo info, [FromQuery] int userId, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
        {
            EAddOAuthResult addOAuthResult = await _identityService.AddOAuthAsync(userId, info, cancellationToken);
            switch (addOAuthResult)
            {
                case EAddOAuthResult.Success:
                    return returnUrl is null ? Ok() : Redirect(returnUrl);

                case EAddOAuthResult.UserNotFound:
                    return new AuthenticatedUserDataNotFoundResult();

                case EAddOAuthResult.UserAlreadyHasOAuth:
                    return Conflict(new Error("동일한 OAuth 제공자를 가지고 있습니다"));

                case EAddOAuthResult.OAuthAlreadyExists:
                    return Conflict(new Error("이미 사용하는 OAuth 계정입니다"));

                case EAddOAuthResult.ProviderNotFound:
                    return NotFound(new Error("잘못된 OAuth 제공자입니다"));

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
        public async Task<IActionResult> RemoveOAuthAsync([FromRoute] string provider, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            ERemoveOAuthResult removeOAuthResult = await _identityService.RemoveOAuthAsync(userId, provider, cancellationToken);
            switch (removeOAuthResult)
            {
                case ERemoveOAuthResult.Success:
                    return Ok();

                case ERemoveOAuthResult.HasSingleAccount:
                    return Conflict(new Error("연결 계정이 1개일 경우 삭제할 수 없습니다"));

                case ERemoveOAuthResult.UserNotFound:
                    return new AuthenticatedUserDataNotFoundResult();

                case ERemoveOAuthResult.OAuthNotFound:
                    return NotFound(new Error("해당 OAuth 제공자를 사용하고 있지 않습니다"));

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(removeOAuthResult), (int)removeOAuthResult, typeof(ERemoveOAuthResult));
            }
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public SignOutResult Logout() => SignOut();

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public RefreshResult Refresh([ModelBinder<RefreshAuthorizeTokenBinder>, FromSpecial] AuthorizeToken authorizeToken, [RefreshTokenSetCookie] bool setCookie) => new(authorizeToken, setCookie);

        [HttpPost("password/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] PasswordModel password, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            bool isUserExist = await _identityService.ChangePasswordAsync(userId, password.Password, cancellationToken);
            return isUserExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("password/reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] EmailModel email, CancellationToken cancellationToken)
        {
            string? newPassword = await _identityService.ResetPasswordAsync(email.Email, cancellationToken);
            if (newPassword is null)
            {
                return NotFound(new Error("존재하지 않는 계정의 이메일입니다"));
            }

            _userEmailService.SendPasswordResetMail(email.Email, newPassword, CancellationToken.None);
            return Ok();
        }

        [HttpPost("name")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangeNameAsync([FromBody] UserNameModel name, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            bool isExist = await _identityService.ChangeNameAsync(userId, name.Name, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        [HttpPost("id/find")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> FindIdAsync([FromBody] EmailModel email, CancellationToken cancellationToken)
        {
            string? accountId = await _identityService.FindAccountIdAsync(email.Email, cancellationToken);
            if (accountId is null)
            {
                return NotFound();
            }

            _userEmailService.SendAccountIdMail(email.Email, accountId, cancellationToken);
            return Ok();
        }

        [HttpPost("withdraw")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> WithDrawAsync([UserIdBind] int userId, CancellationToken cancellationToken)
        {
            EWithDrawResult result = await _identityService.WithDrawAsync(userId, cancellationToken);
            return result switch
            {
                EWithDrawResult.Success => Ok(),
                EWithDrawResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EWithDrawResult.DatabaseError => StatusCode(StatusCodes.Status500InternalServerError, new Error("데이터베이스에서 오류가 발생했습니다")),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(EWithDrawResult))
            };
        }

        [HttpPost("withdraw/cancel")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> CancelWithDrawAsync([UserIdBind] int userId, CancellationToken cancellationToken)
        {
            ECancelWithDrawResult result = await _identityService.CancelWithDrawAsync(userId, cancellationToken);
            return result switch
            {
                ECancelWithDrawResult.Success => Ok(),
                ECancelWithDrawResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                ECancelWithDrawResult.Expired => BadRequest(new Error("탈퇴 요청이 만료되었습니다")),
                ECancelWithDrawResult.WithDrawNotRequested => BadRequest(new Error("탈퇴하지 않은 계정입니다")),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(ECancelWithDrawResult))
            };
        }

        [HttpPost("email/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangeEmailAsync([FromBody] EmailModel email, CancellationToken cancellationToken)
        {
            string? confirmUri = Url.Action("ConfirmChangeEmail", "Identity");
            Debug.Assert(confirmUri is not null); // ConfirmChangeEmailAsync가 존재하므로 null이 아니어야 함
            string code = _emailVerifyService.GenerateVerificationCode();
            await _emailVerifyService.SetVerifyCodeAsync(email.Email, code, cancellationToken);
            _userEmailService.SendEmailVerifyMail(email.Email, $"{confirmUri}&code={code}", cancellationToken);
            return Ok();
        }

        [HttpGet("email/change/confirm")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ConfirmChangeEmailAsync([FromQuery] string code, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            string? email = await _emailVerifyService.VerifyEmailCodeAsync(code, cancellationToken);
            if (email is null)
            {
                return BadRequest(new Error("잘못된 코드입니다"));
            }

            bool isExist = await _identityService.ChangeEmailAsync(userId, email, cancellationToken);
            return isExist ? Ok() : new AuthenticatedUserDataNotFoundResult();
        }

        private IActionResult HandleLogin(ELoginResult loginResult, User? user, string? returnUrl)
        {
            Debug.Assert(loginResult is ELoginResult.Success ^ user is null);
            string message = loginResult switch
            {
                ELoginResult.Success => "로그인 성공",
                ELoginResult.NotFound => "존재하지 않는 계정입니다",
                ELoginResult.WrongPassword => "틀린 비밀번호입니다",
                _ => throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult))
            };

            return loginResult switch
            {
                ELoginResult.Success => new LoginActionResult(user!, returnUrl),
                ELoginResult.NotFound => NotFound(new Error(message)),
                ELoginResult.WrongPassword => Unauthorized(new Error(message)),
                _ => throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult))
            };
        }

        private IActionResult HandleSignUp(ESignUpResult signUpResult, User? user, string? returnUrl)
        {
            Debug.Assert(signUpResult is ESignUpResult.Success ^ user is null);
            string message = signUpResult switch
            {
                ESignUpResult.Success => "회원가입 성공",
                ESignUpResult.UserIdAlreadyExists => "중복된 Id입니다",
                ESignUpResult.NameAlreadyExists => "중복된 이름입니다",
                ESignUpResult.EmailAlreadyExists => "중복된 이메일입니다",
                ESignUpResult.OAuthAlreadyExists => "이미 존재하는 계정입니다",
                ESignUpResult.ProviderNotFound => "잘못된 OAuth 제공자입니다",
                _ => throw new InvalidEnumArgumentException(nameof(signUpResult), (int)signUpResult, typeof(ESignUpResult))
            };

            return signUpResult switch
            {
                ESignUpResult.Success => new LoginActionResult(user!, returnUrl),
                ESignUpResult.UserIdAlreadyExists => Conflict(new Error(message)),
                ESignUpResult.NameAlreadyExists => Conflict(new Error(message)),
                ESignUpResult.EmailAlreadyExists => Conflict(new Error(message)),
                ESignUpResult.ProviderNotFound => Conflict(new Error(message)),
                ESignUpResult.OAuthAlreadyExists => Conflict(new Error(message)),
                _ => throw new InvalidEnumArgumentException(nameof(signUpResult), (int)signUpResult, typeof(ESignUpResult))
            };
        }
    }
}
