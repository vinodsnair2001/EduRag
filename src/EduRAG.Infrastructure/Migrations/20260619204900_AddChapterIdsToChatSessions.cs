using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterIdsToChatSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Store selected chapter IDs as a JSON int array on each chat session.
            // NULL or empty string = no chapter filter (all chunks for the subject).
            migrationBuilder.AddColumn<string>(
                name: "SelectedChapterIds",
                table: "ChatSessions",
                type: "text",
                nullable: true);

            // Composite index for chapter-filtered vector search pre-filter.
            // Covers the common WHERE ClassId = @c AND SubjectId = @s AND ChapterId = ANY(@ids) pattern.
            migrationBuilder.CreateIndex(
                name: "idx_chunks_class_subject_chapter",
                table: "MaterialChunks",
                columns: new[] { "ClassId", "SubjectId", "ChapterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_chunks_class_subject_chapter",
                table: "MaterialChunks");

            migrationBuilder.DropColumn(
                name: "SelectedChapterIds",
                table: "ChatSessions");
        }
    }
}
