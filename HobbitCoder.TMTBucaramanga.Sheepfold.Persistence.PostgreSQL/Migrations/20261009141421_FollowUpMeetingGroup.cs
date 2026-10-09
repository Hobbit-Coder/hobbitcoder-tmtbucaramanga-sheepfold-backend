using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class FollowUpMeetingGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "Sheepfold",
                table: "FollowUps",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_GroupId",
                schema: "Sheepfold",
                table: "FollowUps",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Groups_GroupId",
                schema: "Sheepfold",
                table: "FollowUps",
                column: "GroupId",
                principalSchema: "Sheepfold",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Groups_GroupId",
                schema: "Sheepfold",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_GroupId",
                schema: "Sheepfold",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "Sheepfold",
                table: "FollowUps");
        }
    }
}
