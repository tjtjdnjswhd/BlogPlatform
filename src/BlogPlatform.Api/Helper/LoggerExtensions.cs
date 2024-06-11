using StatusGeneric;

namespace BlogPlatform.Api.Helper
{
    public static partial class LoggerExtensions
    {
        public static void LogStatusGeneric(this ILogger logger, IStatusGeneric statusGeneric)
        {
            LogGenericGeneric(logger, statusGeneric.Message, statusGeneric.GetAllErrors(), statusGeneric.HasErrors ? LogLevel.Warning : LogLevel.Information);
        }

        [LoggerMessage("Soft delete message: {message}, Errors: {errors}")]
        private static partial void LogGenericGeneric(this ILogger logger, string message, string errors, LogLevel logLevel);
    }
}
