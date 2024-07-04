using AspNet.Security.OAuth.KakaoTalk;
using AspNet.Security.OAuth.Naver;

using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.ModelBinders;
using BlogPlatform.Api.Identity.Services;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Identity.Services.Options;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Extensions;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace BlogPlatform.Api.Identity.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentity(this IServiceCollection services, IConfigurationSection optionsSection, IConfigurationSection oauthProviderSection)
        {
            ServiceDescriptor authenticationSchemeProviderService = new(typeof(IAuthenticationSchemeProvider), typeof(IgnoreCaseAuthenticationSchemeProvider), ServiceLifetime.Singleton);
            services.Replace(authenticationSchemeProviderService);

            IConfigurationSection identityServiceOptionsSection = optionsSection.GetRequiredSection("IdentityService");
            services.AddIdentityService(identityServiceOptionsSection);

            IConfigurationSection userEmailOptionsSection = optionsSection.GetRequiredSection("UserEmail");
            services.AddUserEmailService(userEmailOptionsSection);

            IConfigurationSection accountOptionsSection = optionsSection.GetRequiredSection("Account");
            services.AddAccountOptions(accountOptionsSection);

            services.AddEmailVerifyService();

            services.AddScoped<IPasswordHasher<BasicAccount>, PasswordHasher<BasicAccount>>();

            services.AddScoped<IUserClaimsPrincipalFactory<User>, JwtClaimsPrincipalFactory>();

            IConfigurationSection authorizeTokenOptionsSection = optionsSection.GetRequiredSection("AuthorizeToken");
            services.AddScoped<IAuthorizeTokenService, AuthorizeTokenService>();
            services.Configure<AuthorizeTokenOptions>(authorizeTokenOptionsSection);

            services.AddScoped<JsonWebTokenHandler>();

            services.AddScoped<UserBanFilter>();

            services.Configure<MvcOptions>(m =>
            {
                m.Filters.AddService<UserBanFilter>();
                m.ModelBinderProviders.Insert(0, new OAuthInfoModelBinderProvider());
            });

            services.AddAuthorization(options =>
            {
                AuthorizationPolicyBuilder userPolicyBuilder = new(JwtSignInHandler.AuthenticationScheme);
                userPolicyBuilder.RequireRole(PolicyConstants.UserRolePolicy);
                userPolicyBuilder.RequireAuthenticatedUser();
                options.AddPolicy(PolicyConstants.UserRolePolicy, userPolicyBuilder.Build());

                AuthorizationPolicyBuilder adminPolicyBuilder = new(JwtSignInHandler.AuthenticationScheme);
                adminPolicyBuilder.RequireRole(PolicyConstants.AdminRolePolicy);
                adminPolicyBuilder.RequireAuthenticatedUser();
                options.AddPolicy(PolicyConstants.AdminRolePolicy, adminPolicyBuilder.Build());

                AuthorizationPolicyBuilder oauthPolicyBuilder = new(GoogleDefaults.AuthenticationScheme, KakaoTalkAuthenticationDefaults.AuthenticationScheme, NaverAuthenticationDefaults.AuthenticationScheme);
                oauthPolicyBuilder.RequireAuthenticatedUser();
                options.AddPolicy(PolicyConstants.OAuthPolicy, oauthPolicyBuilder.Build());
            });

            AuthorizeTokenOptions jwtOptions = authorizeTokenOptionsSection.Get<AuthorizeTokenOptions>() ?? throw new Exception();
            services.AddAuthentication(JwtSignInHandler.AuthenticationScheme)
                .AddJwtSignIn(options =>
                {
                    options.ClaimsIssuer = jwtOptions.Issuer;
                    options.Audience = jwtOptions.Audience;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    };

                    options.Events = new()
                    {
                        OnMessageReceived = async context =>
                        {
                            var scope = context.HttpContext.RequestServices.CreateScope();
                            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
                            AuthorizeToken? cookieToken = await authorizeTokenService.GetAsync(context.HttpContext.Request, true);
                            context.Token = cookieToken?.AccessToken;
                        },
                        OnTokenValidated = async context =>
                        {
                            var scope = context.HttpContext.RequestServices.CreateScope();

                            JsonWebToken jsonWebToken = context.SecurityToken as JsonWebToken ?? throw new Exception();
                            if (!jsonWebToken.TryGetClaim(ClaimTypes.AuthenticationMethod, out Claim methodClaim))
                            {
                                context.Fail("");
                                return;
                            }

                            Debug.Assert(methodClaim.Value == JwtClaimValues.AuthenticationMethodBearer || methodClaim.Value == JwtClaimValues.AuthenticationMethodCookie);

                            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
                            AuthorizeToken? cookieToken = await authorizeTokenService.GetAsync(context.HttpContext.Request, true);
                            if (methodClaim.Value == JwtClaimValues.AuthenticationMethodBearer && jsonWebToken.EncodedToken == cookieToken?.AccessToken)
                            {
                                context.Fail("해당 토큰은 쿠키로 인증해야 합니다");
                                return;
                            }

                            if (methodClaim.Value == JwtClaimValues.AuthenticationMethodCookie && jsonWebToken.EncodedToken != cookieToken?.AccessToken)
                            {
                                context.Fail("해당 토큰은 Authorization 헤더로 인증해야 합니다");
                                return;
                            }
                        }
                    };
                })
                .AddGoogle(options =>
                {
                    options.ClientId = oauthProviderSection["Google:ClientSecret"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["Google:ClientSecret"] ?? throw new Exception();
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddKakaoTalk(options =>
                {
                    options.ClientId = oauthProviderSection["KakaoTalk:ClientId"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["KakaoTalk:ClientSecret"] ?? throw new Exception();
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddNaver(options =>
                {
                    options.ClientId = oauthProviderSection["Naver:ClientId"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["Naver:ClientSecret"] ?? throw new Exception();
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie();

            return services;
        }
    }
}