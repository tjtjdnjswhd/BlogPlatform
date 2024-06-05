namespace BlogPlatform.Api.Tests
{
    public class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public TestTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
