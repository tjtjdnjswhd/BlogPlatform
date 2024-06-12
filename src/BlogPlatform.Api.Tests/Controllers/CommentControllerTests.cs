using BlogPlatform.Api.Controllers;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Tests.DbContextMock;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Moq;

using StatusGeneric;

using Xunit.Abstractions;

using Utils = BlogPlatform.Api.Tests.Controllers.ControllerTestsUtils;

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

            List<Post> posts = [
                new Post("post1", "description1", 1) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Post("post2", "description2", 2) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new Post("post3", "description3", 3) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt }
            ];

            List<Comment> comments = [
                new("Comment1", 1, 1, null) { Id = 1, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment2", 2, 2, 1) { Id = 2, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment3", 2, 1, 2) { Id = 3, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment4", 3, 2, null) { Id = 4, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment5", 3, 1, null) { Id = 5, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
                new("Comment6", 3, 2, 3) { Id = 6, SoftDeletedAt = EntityBase.DefaultSoftDeletedAt },
            ];

            _setUp.DbContextMock.SetDbSet(db => db.Posts, posts);
            _commentDbSetMock = _setUp.DbContextMock.SetDbSet(db => db.Comments, comments);

            XUnitLogger<CommentController> controllerLogger = new(testOutputHelper);
            _commentController = new(_setUp.DbContextMock.Object, _setUp.SoftDeleteServiceMock.Object, controllerLogger);
        }

        [Fact]
        public async Task Get_NotFound()
        {
            // Act
            IActionResult result = await _commentController.GetAsync(7, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 7 }, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Get_Ok()
        {
            // Act
            IActionResult result = await _commentController.GetAsync(3, CancellationToken.None);

            // Assert
            Utils.VerifyOkObjectResult<CommentRead>(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 3 }, CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GetByPost_Ok()
        {
            // Act
            IAsyncEnumerable<CommentRead> result = _commentController.GetByPost(3, 1);

            // Assert
            IEnumerable<CommentRead> comments = result.ToBlockingEnumerable();
            Assert.Equal(3, comments.Count());
        }

        [Fact]
        public void Get_Search_Ok()
        {
            // Arrange
            CommentSearch search = new("Comment", 3, 2);

            // Act
            IAsyncEnumerable<CommentSearchResult> result = _commentController.GetAsync(search);

            // Assert
            IEnumerable<CommentSearchResult> comments = result.ToBlockingEnumerable();
            Assert.Equal(2, comments.Count());
        }

        [Fact]
        public async Task Create_PostNotFound()
        {
            // Act
            IActionResult result = await _commentController.CreateAsync("Comment7", 4, null, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _commentDbSetMock.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_ParentCommentNotFound()
        {
            // Act
            IActionResult result = await _commentController.CreateAsync("Comment7", 3, 7, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _commentDbSetMock.Verify(c => c.Add(It.IsAny<Comment>()), Times.Never);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Create_Parent_CreatedAtAction()
        {
            // Act
            IActionResult result = await _commentController.CreateAsync("Comment7", 3, 2, 1, CancellationToken.None);

            // Assert
            Utils.VerifyCreatedResult(result, "GetAsync", "Comment");
            _commentDbSetMock.Verify(c => c.Add(It.IsAny<Comment>()), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Create_Child_CreatedAtAction()
        {
            // Act
            IActionResult result = await _commentController.CreateAsync("Comment7", 3, null, 1, CancellationToken.None);

            // Assert
            Utils.VerifyCreatedResult(result, "GetAsync", "Comment");
        }

        [Fact]
        public async Task Update_NotFound()
        {
            // Act
            IActionResult result = await _commentController.UpdateAsync(7, "Comment7", 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 7 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_Forbid()
        {
            // Act
            IActionResult result = await _commentController.UpdateAsync(1, "Comment7", 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Update_NoContent()
        {
            // Act
            IActionResult result = await _commentController.UpdateAsync(1, "Comment7", 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            // Act
            IActionResult result = await _commentController.DeleteAsync(7, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNotFoundResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 7 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Delete_Forbid()
        {
            // Act
            IActionResult result = await _commentController.DeleteAsync(1, 2, CancellationToken.None);

            // Assert
            Utils.VerifyForbidResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
            _setUp.DbContextMock.Verify(d => d.SaveChangesAsync(CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task Delete_InternalServeError()
        {
            // Arrange
            StatusGenericHandler<int> errorStatus = new();
            errorStatus.AddError("error");
            _setUp.SoftDeleteServiceMock.Setup(s => s.SetSoftDeleteAsync(It.IsAny<Comment>(), It.IsAny<bool>()))
                .ReturnsAsync(errorStatus);

            // Act
            IActionResult result = await _commentController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyInternalServerError(result);
        }

        [Fact]
        public async Task Delete_NoContent()
        {
            // Act
            IActionResult result = await _commentController.DeleteAsync(1, 1, CancellationToken.None);

            // Assert
            Utils.VerifyNoContentResult(result);
            _commentDbSetMock.Verify(d => d.FindAsync(new object[] { 1 }, CancellationToken.None), Times.Once);
        }
    }
}
