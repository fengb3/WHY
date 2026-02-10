using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WHY.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionRecommendationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookmarkCount",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BountyAmount",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DownvoteCount",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasAcceptedAnswer",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ShareCount",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpvoteCount",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "QuestionVotes",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsUpvote = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionVotes", x => new { x.QuestionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_QuestionVotes_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LastActivityAt",
                table: "Questions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionVotes_UserId",
                table: "QuestionVotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionVotes");

            migrationBuilder.DropIndex(
                name: "IX_Questions_LastActivityAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BookmarkCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BountyAmount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DownvoteCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "HasAcceptedAnswer",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "UpvoteCount",
                table: "Questions");
        }
    }
}
