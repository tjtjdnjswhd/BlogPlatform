using Microsoft.EntityFrameworkCore.Query;

using System.Linq.Expressions;

namespace BlogPlatform.Api.Tests.DbContextMock
{
    public class InMemoryAsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _innerProvider;

        public InMemoryAsyncQueryProvider(IQueryProvider innerProvider)
        {
            _innerProvider = innerProvider;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new InMemoryQueryable<T>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new InMemoryQueryable<TElement>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var result = Execute(expression);

            var expectedResultType = typeof(TResult).GetGenericArguments()?.FirstOrDefault();
            if (expectedResultType == null)
            {
                return default;
            }

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(expectedResultType)
                .Invoke(null, [result])!;
        }

        public object? Execute(Expression expression)
        {
            return _innerProvider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _innerProvider.Execute<TResult>(expression);
        }
    }
}
