using StatusGeneric;

namespace BlogPlatform.Api.Helper
{
    public static partial class LoggerExtensions
    {
        public static void LogSoftDeleteStatus(this ILogger logger, IStatusGeneric statusGeneric, LogLevel logLevel)
        {
            LogSoftDeleteGeneric(logger, statusGeneric.Message, statusGeneric.GetAllErrors(), logLevel);
        }

        [LoggerMessage("Soft delete message: {message}, Errors: {errors}")]
        private static partial void LogSoftDeleteGeneric(this ILogger logger, string message, string errors, LogLevel logLevel);
    }
}
