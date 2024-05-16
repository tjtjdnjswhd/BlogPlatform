using BlogPlatform.EFCore;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Filters
{
    public class UserBanFilter : IAsyncAuthorizationFilter
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly ILogger<UserBanFilter> _logger;

        public UserBanFilter(BlogPlatformDbContext blogPlatformDbContext, ILogger<UserBanFilter> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
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

            if (banExpiresAt is not null && DateTimeOffset.UtcNow < banExpiresAt)
            {
                _logger.LogInformation("User {userId} is banned until {banExpiresAt}", userId, banExpiresAt);
                context.Result = new ForbidResult($"해당 계정은 {banExpiresAt}까지 사용할 수 없습니다.");
            }
        }
    }
}
