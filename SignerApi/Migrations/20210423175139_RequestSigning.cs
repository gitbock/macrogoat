using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class RequestSigning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserFilename",
                table: "ApiActivity",
                newName: "UserOfficeFilename");

            migrationBuilder.RenameColumn(
                name: "SystemFilename",
                table: "ApiActivity",
                newName: "UserCertFilename");

            migrationBuilder.AddColumn<string>(
                name: "SystemCertFileName",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemOfficeFilename",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemCertFileName",
                table: "ApiActivity");

            migrationBuilder.DropColumn(
                name: "SystemOfficeFilename",
                table: "ApiActivity");

            migrationBuilder.RenameColumn(
                name: "UserOfficeFilename",
                table: "ApiActivity",
                newName: "UserFilename");

            migrationBuilder.RenameColumn(
                name: "UserCertFilename",
                table: "ApiActivity",
                newName: "SystemFilename");
        }
    }
}
