using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Models.User;

namespace BlogPlatform.Api.QueryExtensions
{
    public static class UserQueryExtensions
    {
        public static IQueryable<UserRead> SelectUserRead(this IQueryable<User> users)
        {
            return users.Select(u => new UserRead(u.Id,
                                                  u.BasicAccounts.Select(b => b.AccountId).FirstOrDefault(),
                                                  u.Name,
                                                  u.Email,
                                                  u.CreatedAt,
                                                  u.Blog.Select(b => b.Id).FirstOrDefault(),
                                                  u.Roles.Select(r => r.Name),
                                                  u.OAuthAccounts.Select(o => o.Provider.Name)));
        }
    }
}
