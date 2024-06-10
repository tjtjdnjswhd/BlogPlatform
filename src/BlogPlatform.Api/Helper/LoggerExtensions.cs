using StatusGeneric;

namespace BlogPlatform.Api.Helper
{
    public static partial class LoggerExtensions
    {
        public static void LogSoftDeleteStatus(this ILogger logger, IStatusGeneric statusGeneric)
        {
            LogSoftDeleteGeneric(logger, statusGeneric.Message, statusGeneric.GetAllErrors(), statusGeneric.HasErrors ? LogLevel.Warning : LogLevel.Information);
        }

        [LoggerMessage("Soft delete message: {message}, Errors: {errors}")]
        private static partial void LogSoftDeleteGeneric(this ILogger logger, string message, string errors, LogLevel logLevel);
    }
}
