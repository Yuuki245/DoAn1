using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace truyenchu.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBookshelfAndReadingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapter_Story_StoryId",
                table: "Chapter");

            migrationBuilder.AlterColumn<int>(
                name: "StoryId",
                table: "Chapter",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Chapter",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Chapter",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserBookshelf",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBookshelf", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBookshelf_Story_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Story",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBookshelf_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReadingHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoryId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    DateRead = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReadingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReadingHistory_Chapter_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapter",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReadingHistory_Story_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Story",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserReadingHistory_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBookshelf_StoryId",
                table: "UserBookshelf",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookshelf_UserId",
                table: "UserBookshelf",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReadingHistory_ChapterId",
                table: "UserReadingHistory",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReadingHistory_StoryId",
                table: "UserReadingHistory",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReadingHistory_UserId",
                table: "UserReadingHistory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapter_Story_StoryId",
                table: "Chapter",
                column: "StoryId",
                principalTable: "Story",
                principalColumn: "StoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapter_Story_StoryId",
                table: "Chapter");

            migrationBuilder.DropTable(
                name: "UserBookshelf");

            migrationBuilder.DropTable(
                name: "UserReadingHistory");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.AlterColumn<int>(
                name: "StoryId",
                table: "Chapter",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Chapter",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Chapter",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapter_Story_StoryId",
                table: "Chapter",
                column: "StoryId",
                principalTable: "Story",
                principalColumn: "StoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
