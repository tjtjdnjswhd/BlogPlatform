using System.Linq.Expressions;

namespace BlogPlatform.Api.Tests.DbContextMock
{
    public class InMemoryQueryable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public InMemoryQueryable(IEnumerable<T> enumerable) : base(enumerable)
        {
        }

        public InMemoryQueryable(Expression expression) : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new InMemoryAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new InMemoryAsyncQueryProvider<T>(this);

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return GetAsyncEnumerator();
        }
    }
}
