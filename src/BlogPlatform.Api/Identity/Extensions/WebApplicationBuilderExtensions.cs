using AspNet.Security.OAuth.KakaoTalk;
using AspNet.Security.OAuth.Naver;

using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Filters;
using BlogPlatform.Api.Identity.Options;
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
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddJwtIdentity(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IIdentityService, IdentityService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IPasswordHasher<BasicAccount>, PasswordHasher<BasicAccount>>();

            JwtOptions jwtOptions = builder.Configuration.GetRequiredSection("JwtOptions").Get<JwtOptions>() ?? throw new Exception();

            builder.Services.AddOptions<JwtOptions>().BindConfiguration("JwtOptions").ValidateOnStart().ValidateDataAnnotations();
            builder.Services.AddOptions<AccountOptions>().BindConfiguration("AccountOptions").ValidateOnStart().ValidateDataAnnotations();

            builder.Services.AddScoped<UserBanFilter>();
            builder.Services.Configure<MvcOptions>(m =>
            {
                m.Filters.AddService<UserBanFilter>();
            });

            builder.Services.AddAuthorization(options =>
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

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.SaveToken = false;

                    options.ClaimsIssuer = jwtOptions.Issuer;
                    options.Audience = jwtOptions.Audience;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    };
                })
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new Exception();
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new Exception();
                })
                .AddKakaoTalk(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:KakaoTalk:ClientId"] ?? throw new Exception();
                    options.ClientSecret = builder.Configuration["Authentication:KakaoTalk:ClientSecret"] ?? throw new Exception();
                })
                .AddNaver(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Naver:ClientId"] ?? throw new Exception();
                    options.ClientSecret = builder.Configuration["Authentication:Naver:ClientSecret"] ?? throw new Exception();
                });

            return builder;
        }
    }
}