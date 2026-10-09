using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AttendeeCourseUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendeeCourses_AttendeeId",
                schema: "Sheepfold",
                table: "AttendeeCourses");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeCourses_AttendeeId_CourseId",
                schema: "Sheepfold",
                table: "AttendeeCourses",
                columns: new[] { "AttendeeId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendeeCourses_AttendeeId_CourseId",
                schema: "Sheepfold",
                table: "AttendeeCourses");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeCourses_AttendeeId",
                schema: "Sheepfold",
                table: "AttendeeCourses",
                column: "AttendeeId");
        }
    }
}
