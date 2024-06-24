using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Filters
{
    /// <summary>
    /// 유저가 차단되었는지 확인하는 필터.
    /// 차단된 유저의 요청을 거부하고, <see cref="ForbidResult"/>로 short-circuiting 실행함
    /// </summary>
    public class UserBanFilter : IAsyncAuthorizationFilter
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<UserBanFilter> _logger;

        public UserBanFilter(BlogPlatformDbContext blogPlatformDbContext, TimeProvider timeProvider, ILogger<UserBanFilter> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if ((!user.Identity?.IsAuthenticated) ?? true)
            {
                return;
            }

            if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out int userId))
            {
                Debug.Assert(false);
            }

            DateTimeOffset? banExpiresAt = await _blogPlatformDbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.BanExpiresAt)
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (banExpiresAt is not null && _timeProvider.GetUtcNow() < banExpiresAt)
            {
                _logger.LogInformation("User {userId} is banned until {banExpiresAt}", userId, banExpiresAt);
                await context.HttpContext.Response.WriteAsJsonAsync(new Error($"해당 계정은 {banExpiresAt}까지 사용할 수 없습니다."));
                context.Result = new ForbidResult();
            }
        }
    }
}
