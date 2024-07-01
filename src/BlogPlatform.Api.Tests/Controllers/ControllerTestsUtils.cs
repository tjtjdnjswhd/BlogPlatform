using BlogPlatform.Shared.Models;

using Microsoft.AspNetCore.Mvc;

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

        public static Error VerifyNotFoundResult(IActionResult actionResult)
        {
            NotFoundObjectResult objectResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            return VerifyErrorResult(objectResult);
        }

        public static Error VerifyConflictResult(IActionResult actionResult)
        {
            ConflictObjectResult objectResult = Assert.IsType<ConflictObjectResult>(actionResult);
            return VerifyErrorResult(objectResult);
        }

        public static Error VerifyBadRequestResult(IActionResult actionResult)
        {
            BadRequestObjectResult objectResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            return VerifyErrorResult(objectResult);
        }

        public static Error VerifyInternalServerError(IActionResult actionResult)
        {
            ObjectResult statusCodeResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(500, statusCodeResult.StatusCode);
            return VerifyErrorResult(statusCodeResult);
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
            Assert.IsType<UnauthorizedResult>(actionResult);
        }

        public static void VerifyCreatedResult(IActionResult actionResult, string actionName, string controllerName)
        {
            CreatedAtActionResult objectResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            Assert.Equal(actionName, objectResult.ActionName);
            Assert.Equal(controllerName, objectResult.ControllerName);
        }

        public static Error VerifyErrorResult(ObjectResult objectResult)
        {
            return Assert.IsType<Error>(objectResult.Value);
        }
    }
}
