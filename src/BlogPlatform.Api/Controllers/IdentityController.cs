using BlogPlatform.Api.Attributes;
using BlogPlatform.Api.Identity.ActionResults;
using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.ModelBinders;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;
using BlogPlatform.Shared.Identity.Validations;
using BlogPlatform.Shared.Models;
using BlogPlatform.Shared.Models.User;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
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
        private readonly IEmailVerifyService _emailVerifyService;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IIdentityService identityService, IUserEmailService userEmailService, IEmailVerifyService emailVerifyService, TimeProvider timeProvider, ILogger<IdentityController> logger)
        {
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
            UserRead? userRead = await _identityService.GetFirstUserReadAsync(false, [u => u.Id == userId], cancellationToken);
            if (userRead is null)
            {
                return new AuthenticatedUserDataNotFoundResult();
            }
            return Ok(userRead);
        }

        [HttpPost("login/basic")]
        [PasswordChangeRequiredFilter(nameof(loginInfo))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, returnUrl);
        }

        [HttpPost("signup/basic")]
        [SignUpEmailVerificationFilter(nameof(signUpInfo))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> BasicSignUpAsync([FromBody] BasicSignUpInfo signUpInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            Debug.Assert(signUpResult is not (ESignUpResult.ProviderNotFound or ESignUpResult.OAuthAlreadyExists)); // OAuth 계정이 아닌 경우 OAuthAlreadyExists가 나올 수 없음
            return HandleSignUp(signUpResult, user, returnUrl);
        }

        [HttpPost("signup/email/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> SendSignUpVerifyEmailAsync([FromBody] EmailModel newEmail, CancellationToken cancellationToken)
        {
            string? verifyUri = Url.ActionLink("ConfirmSignUpEmail", "Identity");
            Debug.Assert(!string.IsNullOrWhiteSpace(verifyUri)); // VerifyEmailAsync가 존재하므로 null이 아니어야 함
            string code = _emailVerifyService.GenerateVerificationCode();
            await _emailVerifyService.SetSignUpVerifyCodeAsync(newEmail.Email, code, cancellationToken);
            _userEmailService.SendEmailVerifyMail(newEmail.Email, $"{verifyUri}?code={code}", cancellationToken);
            return Ok();
        }

        [HttpGet("signup/email/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ConfirmSignUpEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _emailVerifyService.VerifySignUpEmailCodeAsync(code, cancellationToken);
            return email is not null ? Ok() : BadRequest(new Error("잘못된 코드입니다"));
        }

        [HttpPost("login/oauth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult OAuthLogin([FromForm, Required(AllowEmptyStrings = false)] string provider, [FromQuery, ReturnUrlWhiteList] string? returnUrl)
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
        public async Task<IActionResult> OAuthLoginCallbackAsync([FromSpecial] OAuthLoginInfo loginInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            Debug.Assert(loginResult != ELoginResult.WrongPassword); // OAuth 로그인 시 비밀번호가 틀릴 수 없음
            return HandleLogin(loginResult, user, returnUrl);
        }

        [HttpPost("signup/oauth")]
        public IActionResult OAuthSignUp([FromForm, Required(AllowEmptyStrings = false)] string provider, [FromForm, UserNameValidate] string name, [FromQuery, Url] string? returnUrl)
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
        public async Task<IActionResult> OAuthSignUpCallbackAsync([FromSpecial] OAuthSignUpInfo signUpInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            return HandleSignUp(signUpResult, user, returnUrl);
        }

        [HttpPost("add/oauth")]
        [UserAuthorize]
        public IActionResult AddOAuth([FromForm] string provider, [FromQuery, ReturnUrlWhiteList] string? returnUrl, [UserIdBind] int userId)
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
        public async Task<IActionResult> AddOAuthCallbackAsync([FromSpecial] OAuthLoginInfo info, [FromQuery] int userId, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            EAddOAuthResult addOAuthResult = await _identityService.AddOAuthAsync(userId, info, cancellationToken);

            string message = addOAuthResult switch
            {
                EAddOAuthResult.Success => string.Empty,
                EAddOAuthResult.UserNotFound => "UserNotFound",
                EAddOAuthResult.UserAlreadyHasOAuth => "UserAlreadyHasOAuth",
                EAddOAuthResult.OAuthAlreadyExists => "OAuthAlreadyExists",
                EAddOAuthResult.ProviderNotFound => "ProviderNotFound",
                _ => throw new InvalidEnumArgumentException(nameof(addOAuthResult), (int)addOAuthResult, typeof(EAddOAuthResult))
            };

            if (returnUrl is null)
            {
                return addOAuthResult switch
                {
                    EAddOAuthResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                    EAddOAuthResult.Success => Ok(),
                    _ => Conflict(new Error(message))
                };
            }
            else
            {
                UriHelper.FromAbsolute(returnUrl, out _, out _, out _, query: out var query, out _);
                query = query.Add("error", message);

                returnUrl = returnUrl.Split('?')[0] + query;
                return addOAuthResult switch
                {
                    EAddOAuthResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                    _ => Redirect(returnUrl),
                };
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
                    return Conflict(new Error("HasSingleAccount"));

                case ERemoveOAuthResult.UserNotFound:
                    return new AuthenticatedUserDataNotFoundResult();

                case ERemoveOAuthResult.OAuthNotFound:
                    return NotFound(new Error("OAuthNotFound"));

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(removeOAuthResult), (int)removeOAuthResult, typeof(ERemoveOAuthResult));
            }
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesDefaultResponseType]
        public SignOutResult Logout([FromQuery, ReturnUrlWhiteList] string? returnUrl) => SignOut(new AuthenticationProperties() { RedirectUri = returnUrl });

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public RefreshResult Refresh([ModelBinder<RefreshAuthorizeTokenBinder>, FromSpecial] AuthorizeToken authorizeToken, [FromQuery, ReturnUrlWhiteList] string? returnUrl) => new(authorizeToken, returnUrl);

        [HttpPost("password/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] PasswordModel password, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            EChangePasswordResult changeResult = await _identityService.ChangePasswordAsync(userId, password.Password, cancellationToken);
            return changeResult switch
            {
                EChangePasswordResult.Success => Ok(),
                EChangePasswordResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EChangePasswordResult.BasicAccountNotFound => NotFound(new Error("BasicAccountNotFound")),
                _ => throw new InvalidEnumArgumentException(nameof(changeResult), (int)changeResult, typeof(EChangePasswordResult))
            };
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
                return NotFound(new Error("NotFound"));
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
            EChangeNameResult result = await _identityService.ChangeNameAsync(userId, name.Name, cancellationToken);
            return result switch
            {
                EChangeNameResult.Success => Ok(),
                EChangeNameResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EChangeNameResult.NameDuplicate => Conflict(new Error("NameDuplicate")),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(EChangeNameResult))
            };
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
                EWithDrawResult.DatabaseError => StatusCode(StatusCodes.Status500InternalServerError, new Error("DatabaseError")),
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
                ECancelWithDrawResult.Expired => BadRequest(new Error("Expired")),
                ECancelWithDrawResult.WithDrawNotRequested => BadRequest(new Error("WithDrawNotRequested")),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(ECancelWithDrawResult))
            };
        }

        [HttpPost("email/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> VerifyChangeEmailAsync([FromBody] EmailModel newEmail, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            string? verifyUri = Url.ActionLink("ConfirmChangeEmail", "Identity");
            Debug.Assert(!string.IsNullOrWhiteSpace(verifyUri)); // ConfirmChangeEmailAsync가 존재하므로 null이 아니어야 함
            string code = _emailVerifyService.GenerateVerificationCode();
            await _emailVerifyService.SetChangeVerifyCodeAsync(userId, newEmail.Email, code, cancellationToken);
            _userEmailService.SendEmailVerifyMail(newEmail.Email, $"{verifyUri}?code={code}", cancellationToken);
            return Ok();
        }

        [HttpGet("email/change")]
        [UserAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> ConfirmChangeEmailAsync([FromQuery] string code, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            string? email = await _emailVerifyService.VerifyChangeEmailCodeAsync(userId, code, cancellationToken);
            if (email is null)
            {
                return BadRequest(new Error("Invalid code"));
            }

            EChangeEmailResult result = await _identityService.ChangeEmailAsync(userId, email, cancellationToken);
            return result switch
            {
                EChangeEmailResult.Success => Ok(),
                EChangeEmailResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EChangeEmailResult.EmailDuplicate => Conflict(new Error("EmailDuplicate")),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(EChangeEmailResult))
            };
        }

        private IActionResult HandleLogin(ELoginResult loginResult, User? user, string? returnUrl)
        {
            Debug.Assert(loginResult is ELoginResult.Success ^ user is null);

            if (loginResult is ELoginResult.Success)
            {
                LoginActionResult loginActionResult = new(user!, returnUrl);
                return loginActionResult;
            }

            Debug.Assert(loginResult is not ELoginResult.Success);
            string message = loginResult switch
            {
                ELoginResult.NotFound => "NotFound",
                ELoginResult.WrongPassword => "WrongPassword",
                _ => throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult))
            };

            if (returnUrl is null)
            {
                return loginResult switch
                {
                    ELoginResult.NotFound => NotFound(new Error(message)),
                    ELoginResult.WrongPassword => Unauthorized(new Error(message)),
                    _ => throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult))
                };
            }
            else
            {
                UriHelper.FromAbsolute(returnUrl, out _, out _, out _, query: out var query, out _);
                query = query.Add("error", message);
                returnUrl = returnUrl.Split('?')[0] + query;
                return Redirect(returnUrl);
            }
        }

        private IActionResult HandleSignUp(ESignUpResult signUpResult, User? user, string? returnUrl)
        {
            Debug.Assert(signUpResult is ESignUpResult.Success ^ user is null);
            if (signUpResult is ESignUpResult.Success)
            {
                LoginActionResult loginActionResult = new(user!, returnUrl);
                return loginActionResult;
            }

            Debug.Assert(signUpResult is not ESignUpResult.Success);
            string message = signUpResult switch
            {
                ESignUpResult.UserIdAlreadyExists => "UserIdAlreadyExists",
                ESignUpResult.NameAlreadyExists => "NameAlreadyExists",
                ESignUpResult.EmailAlreadyExists => "EmailAlreadyExists",
                ESignUpResult.ProviderNotFound => "ProviderNotFound",
                ESignUpResult.OAuthAlreadyExists => "OAuthAlreadyExists",
                _ => throw new InvalidEnumArgumentException(nameof(signUpResult), (int)signUpResult, typeof(ESignUpResult))
            };

            if (returnUrl is null)
            {
                return Conflict(new Error(message));
            }
            else
            {
                UriHelper.FromAbsolute(returnUrl, out _, out _, out _, query: out var query, out _);
                query = query.Add("error", message);
                returnUrl = returnUrl.Split('?')[0] + query;
                return Redirect(returnUrl);
            }
        }
    }
}