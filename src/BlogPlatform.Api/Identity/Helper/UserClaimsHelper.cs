using Microsoft.IdentityModel.JsonWebTokens;

using System.Security.Claims;

namespace BlogPlatform.Api.Identity.Helper
{
    public static class UserClaimsHelper
    {
        private static readonly JsonWebTokenHandler DefaultJsonWebTokenHandler = new();

        public static bool TryGetUserId(ClaimsPrincipal principal, out int userId)
        {
            return int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        public static bool TryGetUserId(string accessToken, out int userId)
        {
            JsonWebToken jsonWebToken = DefaultJsonWebTokenHandler.ReadJsonWebToken(accessToken);
            return int.TryParse(jsonWebToken.Subject, out userId);
        }
    }
}
