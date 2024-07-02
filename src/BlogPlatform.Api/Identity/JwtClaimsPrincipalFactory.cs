using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlogPlatform.Api.Identity
{
    public class JwtClaimsPrincipalFactory : IUserClaimsPrincipalFactory<User>
    {
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly ILogger<JwtClaimsPrincipalFactory> _logger;

        public JwtClaimsPrincipalFactory(BlogPlatformDbContext blogPlatformDbContext, ILogger<JwtClaimsPrincipalFactory> logger)
        {
            _blogPlatformDbContext = blogPlatformDbContext;
            _logger = logger;
        }

        public async Task<ClaimsPrincipal> CreateAsync(User user)
        {
            _logger.LogDebug("Creating claims principal for user: {userId}", user.Id);

            Claim[] roleClaims = await _blogPlatformDbContext.Users
                .Where(u => u.Id == user.Id)
                .SelectMany(u => u.Roles)
                .Select(r => new Claim(ClaimTypes.Role, r.Name)).ToArrayAsync();

            List<Claim> claims = [
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Name, user.Name),
            ];
            claims.AddRange(roleClaims);

            _logger.LogInformation("Claims principal created for user: {userId}. claims: {claims}", user.Id, claims);

            ClaimsIdentity claimsIdentity = new(claims, "application", ClaimTypes.Name, ClaimTypes.Role);
            ClaimsPrincipal claimsPrincipal = new(claimsIdentity);

            return claimsPrincipal;
        }
    }
}
