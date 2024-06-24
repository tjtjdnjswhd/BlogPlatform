using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogPlatform.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class V1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OAuthProvider",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthProvider", x => x.Id);
                    table.CheckConstraint("CK_OAuthProvider_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                    table.CheckConstraint("CK_Role_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BanExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.CheckConstraint("CK_User_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BasicAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPasswordChangeRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasicAccounts", x => x.Id);
                    table.CheckConstraint("CK_BasicAccounts_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                    table.ForeignKey(
                        name: "FK_BasicAccounts_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Blog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog", x => x.Id);
                    table.CheckConstraint("CK_Blog_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                    table.ForeignKey(
                        name: "FK_Blog_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OAuthAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NameIdentifier = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthAccount", x => x.Id);
                    table.CheckConstraint("CK_OAuthAccount_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                    table.ForeignKey(
                        name: "FK_OAuthAccount_OAuthProvider_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "OAuthProvider",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OAuthAccount_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_Role_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_User_UsersId",
                        column: x => x.UsersId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlogId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.CheckConstraint("CK_Category_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                    table.ForeignKey(
                        name: "FK_Category_Blog_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Post",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Post", x => x.Id);
                    table.CheckConstraint("CK_Post_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                    table.ForeignKey(
                        name: "FK_Post_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    SoftDeleteLevel = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                    table.CheckConstraint("CK_Comment_SoftDeleteLevel_SoftDeletedAt", "(SoftDeleteLevel = 0 AND SoftDeletedAt = '9999-12-31 23:59:59.999999') OR (SoftDeleteLevel <> 0 AND SoftDeletedAt <> '9999-12-31 23:59:59.999999')");
                    table.ForeignKey(
                        name: "FK_Comment_Comment_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comment_Post_PostId",
                        column: x => x.PostId,
                        principalTable: "Post",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comment_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BasicAccounts_AccountId_SoftDeletedAt",
                table: "BasicAccounts",
                columns: new[] { "AccountId", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BasicAccounts_SoftDeleteLevel",
                table: "BasicAccounts",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BasicAccounts_UserId",
                table: "BasicAccounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Name_SoftDeletedAt",
                table: "Blog",
                columns: new[] { "Name", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blog_SoftDeleteLevel",
                table: "Blog",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_UserId_SoftDeletedAt",
                table: "Blog",
                columns: new[] { "UserId", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_BlogId",
                table: "Category",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_SoftDeleteLevel",
                table: "Category",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ParentCommentId",
                table: "Comment",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_PostId",
                table: "Comment",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_SoftDeleteLevel",
                table: "Comment",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_UserId",
                table: "Comment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAccount_NameIdentifier_ProviderId_SoftDeletedAt",
                table: "OAuthAccount",
                columns: new[] { "NameIdentifier", "ProviderId", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAccount_ProviderId",
                table: "OAuthAccount",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAccount_SoftDeleteLevel",
                table: "OAuthAccount",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAccount_UserId",
                table: "OAuthAccount",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthProvider_Name_SoftDeletedAt",
                table: "OAuthProvider",
                columns: new[] { "Name", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthProvider_SoftDeleteLevel",
                table: "OAuthProvider",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Post_CategoryId",
                table: "Post",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Post_SoftDeleteLevel",
                table: "Post",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name_SoftDeletedAt",
                table: "Role",
                columns: new[] { "Name", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_Priority_SoftDeletedAt",
                table: "Role",
                columns: new[] { "Priority", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_SoftDeleteLevel",
                table: "Role",
                column: "SoftDeleteLevel");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email_SoftDeletedAt",
                table: "User",
                columns: new[] { "Email", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Name_SoftDeletedAt",
                table: "User",
                columns: new[] { "Name", "SoftDeletedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_SoftDeleteLevel",
                table: "User",
                column: "SoftDeleteLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasicAccounts");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "OAuthAccount");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "Post");

            migrationBuilder.DropTable(
                name: "OAuthProvider");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Blog");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
