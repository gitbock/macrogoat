using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class SystemFilename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UniqueFilename",
                table: "ApiActivity",
                newName: "UniqueFilenameKey");

            migrationBuilder.AddColumn<string>(
                name: "SystemFilename",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemFilename",
                table: "ApiActivity");

            migrationBuilder.RenameColumn(
                name: "UniqueFilenameKey",
                table: "ApiActivity",
                newName: "UniqueFilename");
        }
    }
}
