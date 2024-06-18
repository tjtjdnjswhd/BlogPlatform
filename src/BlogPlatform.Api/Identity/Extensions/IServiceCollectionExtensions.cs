using AspNet.Security.OAuth.KakaoTalk;
using AspNet.Security.OAuth.Naver;

using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using System.Text;

namespace BlogPlatform.Api.Identity.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentity(this IServiceCollection services, IConfigurationSection optionsSection, IConfigurationSection oauthProviderSection)
        {
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUserEmailService, UserEmailService>();
            services.AddScoped<IEmailVerifyService, EmailVerifyService>();
            services.AddScoped<IPasswordHasher<BasicAccount>, PasswordHasher<BasicAccount>>();

            IConfigurationSection jwtOptionsSection = optionsSection.GetRequiredSection("Jwt");
            services.AddOptions<JwtOptions>().Bind(jwtOptionsSection).ValidateOnStart().ValidateDataAnnotations();

            IConfigurationSection accountOptionsSection = optionsSection.GetRequiredSection("Account");
            services.AddOptions<AccountOptions>().Bind(accountOptionsSection).ValidateOnStart().ValidateDataAnnotations();

            IConfigurationSection userEmailOptionsSection = optionsSection.GetRequiredSection("UserEmail");
            services.AddOptions<UserEmailOptions>().Bind(userEmailOptionsSection).ValidateOnStart().ValidateDataAnnotations();

            services.AddScoped<UserBanFilter>();
            services.Configure<MvcOptions>(m =>
            {
                m.Filters.AddService<UserBanFilter>();
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
            });

            JwtOptions jwtOptions = jwtOptionsSection.Get<JwtOptions>() ?? throw new Exception();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                .AddGoogle("google", options =>
                {
                    options.ClientId = oauthProviderSection["Google:ClientSecret"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["Google:ClientSecret"] ?? throw new Exception();
                })
                .AddKakaoTalk("kakaotalk", options =>
                {
                    options.ClientId = oauthProviderSection["KakaoTalk:ClientId"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["KakaoTalk:ClientSecret"] ?? throw new Exception();
                })
                .AddNaver("naver", options =>
                {
                    options.ClientId = oauthProviderSection["Naver:ClientId"] ?? throw new Exception();
                    options.ClientSecret = oauthProviderSection["Naver:ClientSecret"] ?? throw new Exception();
                });

            return services;
        }
    }
}