using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using Moq;

using StatusGeneric;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class ControllerTestsSetUp
    {
        public Mock<BlogPlatformDbContext> DbContextMock { get; private set; }

        public Mock<ICascadeSoftDeleteService> SoftDeleteServiceMock { get; private set; }

        public ControllerTestsSetUp()
        {
            DbContextOptionsBuilder<BlogPlatformDbContext> dbContextOptionsBuilder = new();
            DbContextMock = new(dbContextOptionsBuilder.Options);

            SoftDeleteServiceMock = new();
            SoftDeleteServiceMock.Setup(c => c.SetSoftDeleteAsync(It.IsAny<EntityBase>(), true)).ReturnsAsync(new StatusGenericHandler<int>());
            SoftDeleteServiceMock.Setup(c => c.ResetSoftDeleteAsync(It.IsAny<EntityBase>(), true)).ReturnsAsync(new StatusGenericHandler<int>());
        }
    }
}
