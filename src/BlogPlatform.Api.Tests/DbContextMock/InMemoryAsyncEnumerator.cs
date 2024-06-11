namespace BlogPlatform.Api.Tests.DbContextMock
{
    public class InMemoryAsyncEnumerator<T> : IAsyncEnumerator<T>, IDisposable
    {
        private readonly IEnumerator<T> _enumerator;

        public InMemoryAsyncEnumerator(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new(_enumerator.MoveNext());
        }

        public T Current => _enumerator.Current;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return new();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
