using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BlogPlatform.Api.Identity.ActionResults
{
    public class RefreshResult : IStatusCodeActionResult
    {
        public int? StatusCode => StatusCodes.Status200OK;

        public Task ExecuteResultAsync(ActionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
