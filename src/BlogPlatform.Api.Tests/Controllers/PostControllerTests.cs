using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using Moq;

using Xunit.Abstractions;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class PostControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private Mock<DbSet<Blog>> _blogDbSetMock;
        private readonly PostController _postController;

        public PostControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();

            List<Blog> blogs =
            [
                new("blog1", "description1", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("blog2", "description2", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("blog3", "description3", 3) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt}
            ];

            _blogDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Blogs, blogs);

            XUnitLogger<PostController> controllerLogger = new(testOutputHelper);

        }
    }
}
