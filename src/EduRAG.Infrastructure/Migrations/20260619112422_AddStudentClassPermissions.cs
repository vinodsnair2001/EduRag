using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentClassPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "AppUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPermissions_AppUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentPermissions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_ClassId",
                table: "AppUsers",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPermissions_StudentId_SubjectId",
                table: "StudentPermissions",
                columns: new[] { "StudentId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPermissions_SubjectId",
                table: "StudentPermissions",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Classes_ClassId",
                table: "AppUsers",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Classes_ClassId",
                table: "AppUsers");

            migrationBuilder.DropTable(
                name: "StudentPermissions");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_ClassId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "AppUsers");
        }
    }
}
