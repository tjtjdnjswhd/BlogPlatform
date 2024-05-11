using BlogPlatform.EFCore.Models;

using System.Security.Claims;

namespace BlogPlatform.Api.Services.interfaces
{
    public interface IUserClaimsIdentityFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ClaimsIdentity> CreateClaimsIdentityAsync(User user, CancellationToken cancellationToken = default);
    }
}