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

using Swashbuckle.AspNetCore.Annotations;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BlogPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private const string LoginDescription = "returnUrl을 설정할 경우 cookie를 통해 인증합니다. 설정하지 않은 경우 body로 반환합니다";

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
        [SwaggerOperation("현재 로그인된 사용자의 정보를 반환합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "사용자 정보", typeof(UserRead))]
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
        [SwaggerOperation("Id/PW로 로그인합니다", LoginDescription)]
        [SwaggerResponse(StatusCodes.Status200OK, "로그인 성공")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "비밀번호가 틀림")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "비밀번호 변경 필요")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정이 존재하지 않음")]
        public async Task<IActionResult> BasicLoginAsync([FromBody] BasicLoginInfo loginInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            return HandleLogin(loginResult, user, returnUrl);
        }

        [HttpPost("signup/basic")]
        [SignUpEmailVerificationFilter(nameof(signUpInfo))]
        [SwaggerOperation("Id/PW로 가입합니다", LoginDescription)]
        [SwaggerResponse(StatusCodes.Status200OK, "가입 성공")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "가입 실패\r\n")]
        public async Task<IActionResult> BasicSignUpAsync([FromBody] BasicSignUpInfo signUpInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            Debug.Assert(signUpResult is not (ESignUpResult.ProviderNotFound or ESignUpResult.OAuthAlreadyExists)); // OAuth 계정이 아닌 경우 OAuthAlreadyExists가 나올 수 없음
            return HandleSignUp(signUpResult, user, returnUrl);
        }

        [HttpPost("signup/email/verify")]
        [SwaggerOperation("가입 이메일을 인증하기 위한 이메일을 보냅니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "이메일 전송 성공")]
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
        [SwaggerOperation("가입 이메일을 인증합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "이메일 인증 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "잘못된 코드")]
        public async Task<IActionResult> ConfirmSignUpEmailAsync([FromQuery] string code, CancellationToken cancellationToken)
        {
            string? email = await _emailVerifyService.VerifySignUpEmailCodeAsync(code, cancellationToken);
            return email is not null ? Ok() : BadRequest();
        }

        [HttpPost("login/oauth")]
        [SwaggerOperation("OAuth 공급자를 통해 로그인합니다", LoginDescription)]
        [SwaggerResponse(StatusCodes.Status200OK, "로그인 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정이 존재하지 않음")]
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
        [SwaggerIgnore]
        public async Task<IActionResult> OAuthLoginCallbackAsync([FromSpecial] OAuthLoginInfo loginInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ELoginResult loginResult, User? user) = await _identityService.LoginAsync(loginInfo, cancellationToken);
            Debug.Assert(loginResult != ELoginResult.WrongPassword); // OAuth 로그인 시 비밀번호가 틀릴 수 없음
            return HandleLogin(loginResult, user, returnUrl);
        }

        [HttpPost("signup/oauth")]
        [SwaggerOperation("OAuth 공급자를 통해 가입합니다", LoginDescription)]
        [SwaggerResponse(StatusCodes.Status200OK, "가입 성공")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "중복된 값으로 인해 가입 실패")]
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
        [SwaggerIgnore]
        public async Task<IActionResult> OAuthSignUpCallbackAsync([FromSpecial] OAuthSignUpInfo signUpInfo, [FromQuery, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            (ESignUpResult signUpResult, User? user) = await _identityService.SignUpAsync(signUpInfo, cancellationToken);
            return HandleSignUp(signUpResult, user, returnUrl);
        }

        [HttpPost("add/oauth")]
        [UserAuthorize]
        [SwaggerOperation("현재 계정에 OAuth 공급자 계정을 추가합니다", "returnUrl을 설정할 경우 성공 시 URL 그대로 리디렉트, 실패 시 querystring에 error를 추가해 리디렉트 합니다. 설정하지 않을 경우 성공/실패에 따른 결과를 반환합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "추가 성공")]
        [SwaggerResponse(StatusCodes.Status302Found, "추가 성공 후 리디렉트")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정이 존재하지 않음")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "추가 실패")]
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
        [SwaggerIgnore]
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
                    EAddOAuthResult.ProviderNotFound => Problem(detail: message, statusCode: StatusCodes.Status404NotFound),
                    EAddOAuthResult.Success => Ok(),
                    _ => Problem(detail: message, statusCode: StatusCodes.Status409Conflict)
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
        [SwaggerOperation("현재 계정에 연결된 OAuth 공급자 계정을 제거합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "제거 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "해당 OAuth 공급자는 존재하지 않음")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "해당 계정 이외의 계정이 없음")]
        public async Task<IActionResult> RemoveOAuthAsync([FromRoute] string provider, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            ERemoveOAuthResult removeOAuthResult = await _identityService.RemoveOAuthAsync(userId, provider, cancellationToken);
            switch (removeOAuthResult)
            {
                case ERemoveOAuthResult.Success:
                    return Ok();

                case ERemoveOAuthResult.HasSingleAccount:
                    return Conflict();

                case ERemoveOAuthResult.UserNotFound:
                    return new AuthenticatedUserDataNotFoundResult();

                case ERemoveOAuthResult.OAuthNotFound:
                    return NotFound();

                default:
                    Debug.Assert(false);
                    throw new InvalidEnumArgumentException(nameof(removeOAuthResult), (int)removeOAuthResult, typeof(ERemoveOAuthResult));
            }
        }

        [HttpPost("logout")]
        [SwaggerOperation("로그아웃합니다", "returnUrl을 설정할 경우 로그아웃 후 해당 URL로 리디렉트합니다. cookie를 통한 인증 시 해당 쿠키를 삭제합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "로그아웃 성공")]
        public SignOutResult Logout([FromQuery, ReturnUrlWhiteList] string? returnUrl) => SignOut(new AuthenticationProperties() { RedirectUri = returnUrl });

        [HttpPost("refresh")]
        [SwaggerOperation("현재 토큰을 갱신합니다", "returnUrl을 설정할 경우 갱신 후 해당 URL로 리디렉트합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "갱신 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "토큰이 존재하지 않음")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "토큰 만료")]
        public RefreshResult Refresh([ModelBinder<RefreshAuthorizeTokenBinder>, FromBody] AuthorizeToken? authorizeToken, [FromQuery, ReturnUrlWhiteList] string? returnUrl) => new(authorizeToken, returnUrl);

        [HttpPost("password/change")]
        [SwaggerOperation("비밀번호를 변경합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "변경 성공")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "현재 비밀번호 불일치")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정이 존재하지 않음")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] PasswordChangeModel model, CancellationToken cancellationToken)
        {
            EChangePasswordResult changeResult = await _identityService.ChangePasswordAsync(model, cancellationToken);
            return changeResult switch
            {
                EChangePasswordResult.Success => Ok(),
                EChangePasswordResult.WrongPassword => Unauthorized(),
                EChangePasswordResult.BasicAccountNotFound => NotFound(),
                _ => throw new InvalidEnumArgumentException(nameof(changeResult), (int)changeResult, typeof(EChangePasswordResult))
            };
        }

        [HttpPost("password/reset")]
        [SwaggerOperation("해당 이메일을 가진 유저의 계정 비밀번호를 초기화합니다. 변경된 비밀번호는 메일로 전송합니다", "초기화 후 로그인 전 비밀번호를 변경해야 합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "초기화 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정이 존재하지 않음")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] EmailModel email, CancellationToken cancellationToken)
        {
            string? newPassword = await _identityService.ResetPasswordAsync(email.Email, cancellationToken);
            if (newPassword is null)
            {
                return NotFound();
            }

            _userEmailService.SendPasswordResetMail(email.Email, newPassword, CancellationToken.None);
            return Ok();
        }

        [HttpPost("name")]
        [UserAuthorize]
        [SwaggerOperation("유저 이름을 변경합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "변경 성공")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "이름 중복")]
        public async Task<IActionResult> ChangeNameAsync([FromBody] UserNameModel name, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            EChangeNameResult result = await _identityService.ChangeNameAsync(userId, name.Name, cancellationToken);
            return result switch
            {
                EChangeNameResult.Success => Ok(),
                EChangeNameResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EChangeNameResult.NameDuplicate => Conflict(),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(EChangeNameResult))
            };
        }

        [HttpPost("id/find")]
        [SwaggerOperation("해당 이메일을 가진 유저의 아이디를 메일로 전송합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "전송 성공")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정이 존재하지 않음")]
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
        [SwaggerOperation("현재 유저를 탈퇴시킵니다", "24시간 안에 탈퇴를 취소할 수 있습니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "탈퇴 성공")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "인증 실패")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "DB 오류")]
        public async Task<IActionResult> WithDrawAsync([UserIdBind] int userId, CancellationToken cancellationToken)
        {
            EWithDrawResult result = await _identityService.WithDrawAsync(userId, cancellationToken);
            return result switch
            {
                EWithDrawResult.Success => Ok(),
                EWithDrawResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EWithDrawResult.DatabaseError => Problem(detail: "DB error", statusCode: StatusCodes.Status500InternalServerError),
                _ => throw new InvalidEnumArgumentException(nameof(result), (int)result, typeof(EWithDrawResult))
            };
        }

        [HttpPost("withdraw/cancel/basic")]
        [SwaggerOperation("Id/PW 인증 방식으로 유저 탈퇴를 취소합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "취소 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Expired: 탈퇴 기간 지남. Withdraw not requested: 탈퇴하지 않은 유저")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "인증 실패")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "계정 없음")]
        public async Task<IActionResult> CancelWithDrawAsync([FromBody] BasicLoginInfo loginInfo, CancellationToken cancellationToken)
        {
            ECancelWithDrawResult cancelResult = await _identityService.CancelWithDrawAsync(loginInfo, cancellationToken);
            return cancelResult switch
            {
                ECancelWithDrawResult.Success => Ok(),
                ECancelWithDrawResult.AccountNotFound => Problem("Account not found", statusCode: StatusCodes.Status404NotFound),
                ECancelWithDrawResult.Expired => Problem(detail: "Expired", statusCode: StatusCodes.Status400BadRequest),
                ECancelWithDrawResult.WithDrawNotRequested => Problem(detail: "Withdraw not requested", statusCode: StatusCodes.Status400BadRequest),
                ECancelWithDrawResult.DatabaseError => Problem("DB error", statusCode: StatusCodes.Status500InternalServerError),
                _ => throw new InvalidEnumArgumentException(null, (int)cancelResult, typeof(ECancelWithDrawResult)),
            };
        }

        [HttpPost("withdraw/cancel/oauth")]
        [SwaggerOperation("유저 탈퇴를 취소합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "취소 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Expired: 탈퇴 기간 지남. Withdraw not requested: 탈퇴하지 않은 유저")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "인증 실패")]
        public IActionResult CancelWithDraw([FromForm, Required(AllowEmptyStrings = false)] string provider, [Url, ReturnUrlWhiteList] string? returnUrl, CancellationToken cancellationToken)
        {
            string? redirectUri = Url.Action("CancelWithDrawOAuthCallback", "Identity");
            Debug.Assert(redirectUri is not null);

            AuthenticationProperties authenticationProperties = new()
            {
                ExpiresUtc = _timeProvider.GetUtcNow().AddMinutes(10),
                IsPersistent = false,
                RedirectUri = $"{redirectUri}?{(returnUrl is null ? string.Empty : $"&returnUrl={returnUrl}")}",
                IssuedUtc = _timeProvider.GetUtcNow(),
            };

            return Challenge(authenticationProperties, provider);
        }

        [HttpGet("withdraw/cancel/oauth")]
        [SwaggerIgnore]
        public async Task<IActionResult> CancelWithDrawOAuthCallbackAsync([FromSpecial] OAuthLoginInfo oAuthLoginInfo, [Url, ReturnUrlWhiteList, FromQuery] string? returnUrl, CancellationToken cancellationToken)
        {
            ECancelWithDrawResult cancelResult = await _identityService.CancelWithDrawAsync(oAuthLoginInfo, cancellationToken);

            if (returnUrl is null)
            {
                return cancelResult switch
                {
                    ECancelWithDrawResult.Success => Ok(),
                    ECancelWithDrawResult.AccountNotFound => Problem("Account not found", statusCode: StatusCodes.Status400BadRequest),
                    ECancelWithDrawResult.Expired => Problem(detail: "Expired", statusCode: StatusCodes.Status400BadRequest),
                    ECancelWithDrawResult.WithDrawNotRequested => Problem(detail: "Withdraw not requested", statusCode: StatusCodes.Status400BadRequest),
                    ECancelWithDrawResult.DatabaseError => Problem("DB error", statusCode: StatusCodes.Status500InternalServerError),
                    _ => throw new InvalidEnumArgumentException(null, (int)cancelResult, typeof(ECancelWithDrawResult)),
                };
            }
            else
            {
                string message = cancelResult switch
                {
                    ECancelWithDrawResult.Success => "",
                    ECancelWithDrawResult.AccountNotFound => "AccountNotFound",
                    ECancelWithDrawResult.Expired => "Expired",
                    ECancelWithDrawResult.WithDrawNotRequested => "WithDrawNotRequested",
                    ECancelWithDrawResult.DatabaseError => "DatabaseError",
                    _ => throw new InvalidEnumArgumentException(null, (int)cancelResult, typeof(ECancelWithDrawResult)),
                };

                UriHelper.FromAbsolute(returnUrl, out _, out _, out _, query: out var query, out _);
                query = query.Add("error", message);
                returnUrl = returnUrl.Split('?')[0] + query;
                return Redirect(returnUrl);
            }
        }

        [HttpPost("email/change")]
        [UserAuthorize]
        [SwaggerOperation("이메일 변경을 위한 이메일을 전송합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "전송 성공")]
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
        [SwaggerOperation("이메일 변경을 확인합니다")]
        [SwaggerResponse(StatusCodes.Status200OK, "변경 성공")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "잘못된 코드")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "중복된 이메일")]
        public async Task<IActionResult> ConfirmChangeEmailAsync([FromQuery] string code, [UserIdBind] int userId, CancellationToken cancellationToken)
        {
            string? email = await _emailVerifyService.VerifyChangeEmailCodeAsync(userId, code, cancellationToken);
            if (email is null)
            {
                return Problem(detail: "Invalid code", statusCode: StatusCodes.Status400BadRequest);
            }

            EChangeEmailResult result = await _identityService.ChangeEmailAsync(userId, email, cancellationToken);
            return result switch
            {
                EChangeEmailResult.Success => Ok(),
                EChangeEmailResult.UserNotFound => new AuthenticatedUserDataNotFoundResult(),
                EChangeEmailResult.EmailDuplicate => Problem(detail: "EmailDuplicate", statusCode: StatusCodes.Status409Conflict),
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

            if (returnUrl is null)
            {
                return loginResult switch
                {
                    ELoginResult.NotFound => NotFound(),
                    ELoginResult.WrongPassword => Unauthorized(),
                    _ => throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult))
                };
            }
            else
            {
                string message = loginResult switch
                {
                    ELoginResult.NotFound => "NotFound",
                    ELoginResult.WrongPassword => "WrongPassword",
                    _ => throw new InvalidEnumArgumentException(nameof(loginResult), (int)loginResult, typeof(ELoginResult))
                };

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
                return Problem(detail: message, statusCode: StatusCodes.Status409Conflict);
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