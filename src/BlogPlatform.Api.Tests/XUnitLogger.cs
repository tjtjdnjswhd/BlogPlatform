using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

namespace BlogPlatform.Api.Tests
{
    public class XUnitLogger<T> : ILogger<T>, IDisposable
    {
        private readonly ITestOutputHelper _outputHelper;

        public XUnitLogger(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _outputHelper.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }

        public void Dispose()
        {
        }
    }
}
