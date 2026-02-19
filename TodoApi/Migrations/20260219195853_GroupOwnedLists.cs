using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Migrations
{
    /// <inheritdoc />
    public partial class GroupOwnedLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "TodoLists",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "TodoLists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "TodoLists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoLists_AssignedUserId_Id",
                table: "TodoLists",
                columns: new[] { "AssignedUserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoLists_GroupId_Name",
                table: "TodoLists",
                columns: new[] { "GroupId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_TodoLists_Groups_GroupId",
                table: "TodoLists",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoLists_Users_AssignedUserId",
                table: "TodoLists",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoLists_Groups_GroupId",
                table: "TodoLists");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoLists_Users_AssignedUserId",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoLists_AssignedUserId_Id",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoLists_GroupId_Name",
                table: "TodoLists");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "TodoLists");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "TodoLists");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "TodoLists",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
