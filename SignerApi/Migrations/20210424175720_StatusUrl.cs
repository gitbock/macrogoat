using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class StatusUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SystemCertFileName",
                table: "ApiActivity",
                newName: "SystemCertFilename");

            migrationBuilder.AddColumn<string>(
                name: "StatusUrl",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusUrl",
                table: "ApiActivity");

            migrationBuilder.RenameColumn(
                name: "SystemCertFilename",
                table: "ApiActivity",
                newName: "SystemCertFileName");
        }
    }
}
