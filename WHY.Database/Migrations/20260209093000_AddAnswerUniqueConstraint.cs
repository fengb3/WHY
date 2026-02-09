using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WHY.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId_UserId",
                table: "Answers",
                columns: new[] { "QuestionId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId_UserId",
                table: "Answers");
        }
    }
}
