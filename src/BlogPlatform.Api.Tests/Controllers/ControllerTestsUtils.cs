using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BlogPlatform.Api.Tests.Controllers
{
    public static class ControllerTestsUtils
    {
        public static T VerifyOkObjectResult<T>(IActionResult actionResult)
        {
            OkObjectResult objectResult = Assert.IsType<OkObjectResult>(actionResult);
            return Assert.IsType<T>(objectResult.Value);
        }

        public static void VerifyOkResult(IActionResult actionResult)
        {
            Assert.IsType<OkResult>(actionResult);
        }

        public static void VerifyNotFoundResult(IActionResult actionResult)
        {
            Assert.True(actionResult is NotFoundResult || actionResult is NotFoundObjectResult || actionResult is IStatusCodeActionResult { StatusCode: StatusCodes.Status404NotFound });
        }

        public static void VerifyConflictResult(IActionResult actionResult)
        {
            Assert.True(actionResult is ConflictResult || actionResult is ConflictObjectResult || actionResult is IStatusCodeActionResult { StatusCode: StatusCodes.Status409Conflict });
        }

        public static void VerifyBadRequestResult(IActionResult actionResult)
        {
            Assert.True(actionResult is BadRequestResult || actionResult is BadRequestObjectResult || actionResult is IStatusCodeActionResult { StatusCode: StatusCodes.Status400BadRequest });
        }

        public static void VerifyInternalServerError(IActionResult actionResult)
        {
            ObjectResult statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        public static void VerifyNoContentResult(IActionResult actionResult)
        {
            Assert.IsType<NoContentResult>(actionResult);
        }

        public static void VerifyForbidResult(IActionResult actionResult)
        {
            Assert.IsType<ForbidResult>(actionResult);
        }

        public static void VerifyUnauthorizedResult(IActionResult actionResult)
        {
            Assert.True(actionResult is UnauthorizedResult || actionResult is UnauthorizedObjectResult || actionResult is IStatusCodeActionResult { StatusCode: StatusCodes.Status401Unauthorized });
        }

        public static void VerifyCreatedResult(IActionResult actionResult, string actionName, string controllerName)
        {
            CreatedAtActionResult objectResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            Assert.Equal(actionName, objectResult.ActionName);
            Assert.Equal(controllerName, objectResult.ControllerName);
        }
    }
}
