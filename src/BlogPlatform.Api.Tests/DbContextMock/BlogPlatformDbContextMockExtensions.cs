using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using Moq;

using System.Linq.Expressions;

namespace BlogPlatform.Api.Tests.DbContextMock
{
    public static class BlogPlatformDbContextMockExtensions
    {
        public static Mock<DbSet<TEntity>> SetDbSet<TEntity>(this Mock<BlogPlatformDbContext> dbContextMock, Expression<Func<BlogPlatformDbContext, DbSet<TEntity>>> dbSetExpression, IEnumerable<TEntity> entities)
            where TEntity : EntityBase
        {
            if (dbSetExpression.Body.NodeType != ExpressionType.MemberAccess)
            {
                throw new ArgumentException("Expression must be a member access expression.", nameof(dbSetExpression));
            }

            IQueryable<TEntity> queryable = entities.AsQueryable();
            Mock<DbSet<TEntity>> dbSetMock = new();
            dbSetMock.As<IQueryable<TEntity>>().Setup(m => m.Provider).Returns(new InMemoryAsyncQueryProvider<TEntity>(queryable.Provider));
            dbSetMock.As<IQueryable<TEntity>>().Setup(m => m.Expression).Returns(queryable.Where(e => e.SoftDeleteLevel == 0).Expression);
            dbSetMock.As<IQueryable<TEntity>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<TEntity>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            dbSetMock.As<IAsyncEnumerable<TEntity>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new InMemoryAsyncEnumerator<TEntity>(queryable.GetEnumerator()));

            dbSetMock.Setup(set => set.Find(It.IsAny<object[]>())).Returns<object[]>(ids => queryable.FirstOrDefault(e => e.Id == (int)ids[0]));
            dbSetMock.Setup(set => set.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns<object[], CancellationToken>((ids, _) => ValueTask.FromResult(queryable.FirstOrDefault(e => e.Id == (int)ids[0])));

            dbContextMock.Setup(dbSetExpression).Returns(dbSetMock.Object);
            dbContextMock.Setup(b => b.Set<TEntity>()).Returns(dbSetMock.Object);

            return dbSetMock;
        }
    }
}
