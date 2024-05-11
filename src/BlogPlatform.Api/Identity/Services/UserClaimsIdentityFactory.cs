using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Services.interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.Security.Claims;

namespace BlogPlatform.Api.Services
{
    public class UserClaimsIdentityFactory : IUserClaimsIdentityFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BlogPlatformDbContext _blogPlatformDbContext;
        private readonly UserClaimsIdentityFactoryOptions _factoryOptions;
        private readonly ILogger<UserClaimsIdentityFactory> _logger;

        public UserClaimsIdentityFactory(IServiceProvider serviceProvider, BlogPlatformDbContext blogPlatformDbContext, IOptions<UserClaimsIdentityFactoryOptions> factoryOptions, ILogger<UserClaimsIdentityFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _blogPlatformDbContext = blogPlatformDbContext;
            _factoryOptions = factoryOptions.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ClaimsIdentity> CreateClaimsIdentityAsync(User user, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Creating claims for {user}", user);

            List<string> roleNames = await _blogPlatformDbContext.Roles
                .Where(r =>
                    r.Users.Any(u => u.Id == user.Id))
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found user roles for {user}: {roles}", user, roleNames);

            Claim roleClaim = _factoryOptions.ToRoleClaimFunc(roleNames);
            IEnumerable<Claim> claims = (await _factoryOptions.ClaimsFactoryFunc(_serviceProvider, user, cancellationToken)).Append(roleClaim);

            _logger.LogDebug("Created claims for {user}: {claims}", user, claims);

            ClaimsIdentity claimsIdentity = new(claims, _factoryOptions.AuthenticationType, _factoryOptions.NameClaimType, _factoryOptions.RoleClaimType);
            return claimsIdentity;
        }
    }
}
