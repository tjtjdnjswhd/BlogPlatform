using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using Moq;

using Xunit.Abstractions;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class CommentControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private readonly Mock<DbSet<Comment>> _commentDbSetMock;
        private readonly CommentController _commentController;

        public CommentControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();
            List<Comment> comments = [
                new("Comment1", 1, 1, null) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment2", 2, 2, 1) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment3", 2, 1, 2) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment4", 3, 2, null) { Id = 4, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment5", 3, 1, null) { Id = 5, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment6", 3, 2, 3) { Id = 6, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            _commentDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Comments, comments);

            XUnitLogger<CommentController> controllerLogger = new(testOutputHelper);
            _commentController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, controllerLogger);
        }
    }
}
