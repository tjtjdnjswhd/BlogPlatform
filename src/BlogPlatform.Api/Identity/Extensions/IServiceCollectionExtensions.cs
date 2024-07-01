using AspNet.Security.OAuth.KakaoTalk;
using AspNet.Security.OAuth.Naver;

using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.ModelBinders;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Extensions;
using BlogPlatform.Shared.Identity.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

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

            IConfigurationSection jwtOptionsSection = optionsSection.GetRequiredSection("Jwt");
            services.AddJwtService(jwtOptionsSection);

            IConfigurationSection userEmailOptionsSection = optionsSection.GetRequiredSection("UserEmail");
            services.AddUserEmailService(userEmailOptionsSection);

            IConfigurationSection accountOptionsSection = optionsSection.GetRequiredSection("Account");
            services.AddAccountOptions(accountOptionsSection);

            services.AddEmailVerifyService();

            services.AddScoped<IPasswordHasher<BasicAccount>, PasswordHasher<BasicAccount>>();

            services.AddScoped<UserBanFilter>();

            services.Configure<MvcOptions>(m =>
            {
                m.Filters.AddService<UserBanFilter>();
                m.ModelBinderProviders.Insert(0, new OAuthInfoModelBinderProvider());
            });

            services.AddAuthorization(options =>
            {
                AuthorizationPolicyBuilder userPolicyBuilder = new(JwtBearerDefaults.AuthenticationScheme);
                userPolicyBuilder.RequireRole(PolicyConstants.UserPolicy);
                userPolicyBuilder.RequireAuthenticatedUser();
                options.AddPolicy(PolicyConstants.UserPolicy, userPolicyBuilder.Build());

                AuthorizationPolicyBuilder adminPolicyBuilder = new(JwtBearerDefaults.AuthenticationScheme);
                adminPolicyBuilder.RequireRole(PolicyConstants.AdminPolicy);
                adminPolicyBuilder.RequireAuthenticatedUser();
                options.AddPolicy(PolicyConstants.AdminPolicy, adminPolicyBuilder.Build());

                AuthorizationPolicyBuilder oauthPolicyBuilder = new(GoogleDefaults.AuthenticationScheme, KakaoTalkAuthenticationDefaults.AuthenticationScheme, NaverAuthenticationDefaults.AuthenticationScheme);
                oauthPolicyBuilder.RequireAuthenticatedUser();
                options.AddPolicy(PolicyConstants.OAuthPolicy, oauthPolicyBuilder.Build());
            });

            JwtOptions jwtOptions = jwtOptionsSection.Get<JwtOptions>() ?? throw new Exception();
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = false;
                    options.MapInboundClaims = false;

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
                        AuthenticationType = JwtBearerDefaults.AuthenticationScheme
                    };

                    options.Events = new()
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies[jwtOptions.AccessTokenName];
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddGoogle(options =>
                {
                    options.ClientId = oauthProviderSection["Google:ClientSecret"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["Google:ClientSecret"] ?? throw new Exception();
                })
                .AddKakaoTalk(options =>
                {
                    options.ClientId = oauthProviderSection["KakaoTalk:ClientId"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["KakaoTalk:ClientSecret"] ?? throw new Exception();
                })
                .AddNaver(options =>
                {
                    options.ClientId = oauthProviderSection["Naver:ClientId"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["Naver:ClientSecret"] ?? throw new Exception();
                })
                .AddCookie();

            return services;
        }
    }
}