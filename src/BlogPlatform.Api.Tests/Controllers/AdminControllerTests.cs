using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

using Moq;

using StatusGeneric;

using Xunit.Abstractions;

using Utils = BlogPlatform.Api.Tests.Controllers.ControllerTestsUtils;

namespace BlogPlatform.Api.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly ControllerTestsSetUp _setUp;
        private readonly Mock<DbSet<User>> _userDbSetMock;
        private readonly Mock<DbSet<Role>> _roleDbSetMock;
        private readonly Mock<DbSet<Blog>> _blogDbSetMock;
        private readonly Mock<DbSet<Category>> _categoryDbSetMock;
        private readonly Mock<DbSet<Post>> _postDbSetMock;
        private readonly AdminController _adminController;

        public AdminControllerTests(ITestOutputHelper testOutputHelper)
        {
            _setUp = new();

            List<User> users = [
                new User("user0", "user@user.com") { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new User("admin0", "admin@admin.com") { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new User("deleted0", "deleted@deleted.com") { Id = 3, SoftDeleteLevel = 1, SoftDeletedAt = DateTimeOffset.UtcNow }
            ];

            List<Role> roles = [
                new Role("Admin", 0) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Role("User", 1) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Blog> blogs = [
                new Blog("blog0", "blog0", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Blog("blog1", "blog1", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Blog("blog2", "blog2", 3) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt }
            ];

            List<Category> categories = [
                new Category("category0", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Category("category1", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Post> posts = [
                new Post("post0", "post0", 1) {Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Post("post1", "post1", 2) {Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            List<Comment> comments = [
                new Comment("content0", 1, 1, null) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Comment("content1", 2, 2, 1) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            users[0].Roles.Add(roles[1]);
            users[0].BasicAccounts.Add(new BasicAccount("userId", "asda", 1));
            users[0].Blog.Add(blogs[0]);
            users[1].Roles.Add(roles[0]);
            users[1].Roles.Add(roles[1]);
            users[2].Roles.Add(roles[1]);

            _userDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Users, users);
            _roleDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Roles, roles);
            _blogDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Blogs, blogs);
            _categoryDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Categories, categories);
            _postDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Posts, posts);
            _setUp.DbContextMock.SetDbSet(db => db.Comments, comments);

            XUnitLogger<AdminController> controllerLogger = new(testOutputHelper);

            Mock<IMailSender> mailSenderMock = new();
            _adminController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, mailSenderMock.Object, controllerLogger);

            RouteData routeData = new();
            routeData.Routers.Add(new Mock<IRouter>().Object);
            ActionContext actionContext = new(new DefaultHttpContext(), routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            _adminController.Url = new UrlHelper(actionContext);
        }

        [Theory]
        [InlineData(new int[] { 1, 2 })]
        [InlineData(null)]
        public async Task SendEmails_Ok(int[]? userIds)
        {
            // Act
            IActionResult result = await _adminController.SendEmails("subject", "body", userIds?.ToList(), CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
        }

        [Fact]
        public async Task GetUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.GetUserAsync(new SearchUser(false, null, null, "notExistUser"), CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
        }

        [Fact]
        public async Task GetUserAsync_Ok()
        {
            // Act
            IActionResult result = await _adminController.GetUserAsync(new SearchUser(false, null, null, "user0"), CancellationToken.None);

            // Assert
            Utils.VerifyOkObjectResult<UserRead>(result);
        }

        [Fact]
        public async Task DeleteUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.DeleteUserAsync("notExistUser", CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_InternalServerError()
        {
            // Arrange
            StatusGenericHandler<int> statusGenericHandler = new();
            statusGenericHandler.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<User>(), It.IsAny<bool>())).ReturnsAsync(statusGenericHandler);

            // Act
            IActionResult result = await _adminController.DeleteUserAsync("user@user.com", CancellationToken.None);

            // Assert
            Utils.VerifyInternalServerError(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<User>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_NoContent()
        {
            // Act
            IActionResult result = await _adminController.DeleteUserAsync("user@user.com", CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<User>(), true), Times.Once);
        }

        [Fact]
        public async Task BanUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.BanUserAsync("notExistUser", TimeSpan.FromDays(1), CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task BanUserAsync_Ok()
        {
            // Act
            IActionResult result = await _adminController.BanUserAsync("user@user.com", TimeSpan.FromDays(1), CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task UnbanUserAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.UnbanUserAsync("notExistUser", CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task UnbanUserAsync_Ok()
        {
            // Act
            IActionResult result = await _adminController.UnbanUserAsync("user@user.com", CancellationToken.None);

            // Assert
            Utils.VerifyOkResult(result);
            _setUp.DbContextMock.Verify(db => db.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.DeletePostAsync(7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Post>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeletePostAsync_NoContent()
        {
            // Act
            IActionResult result = await _adminController.DeletePostAsync(1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Post>(), true), Times.Once);
        }

        [Fact]
        public async Task DeleteCommentAsync_NotFound()
        {
            // Act
            IActionResult result = await _adminController.DeleteCommentAsync(7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Comment>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCommentAsync_NoContent()
        {
            // Act
            IActionResult result = await _adminController.DeleteCommentAsync(1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _setUp.SoftDeleteServiceMock.Verify(s => s.SetSoftDeleteAsync(It.IsAny<Comment>(), true), Times.Once);
        }
    }
}
