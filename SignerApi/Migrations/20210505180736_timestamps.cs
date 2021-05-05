using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class timestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "ApiActivity",
                newName: "TsStart");

            migrationBuilder.AddColumn<DateTime>(
                name: "TsLastUpdate",
                table: "ApiActivity",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TsLastUpdate",
                table: "ApiActivity");

            migrationBuilder.RenameColumn(
                name: "TsStart",
                table: "ApiActivity",
                newName: "Timestamp");
        }
    }
}
