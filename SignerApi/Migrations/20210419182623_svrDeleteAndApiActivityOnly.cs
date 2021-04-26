using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class svrDeleteAndApiActivityOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UniqueFilenameKey",
                table: "ApiActivity",
                newName: "UniqueKey");

            migrationBuilder.AddColumn<string>(
                name: "CertExpire",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertIssuedBy",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertIssuedTo",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DownloadUrl",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertExpire",
                table: "ApiActivity");

            migrationBuilder.DropColumn(
                name: "CertIssuedBy",
                table: "ApiActivity");

            migrationBuilder.DropColumn(
                name: "CertIssuedTo",
                table: "ApiActivity");

            migrationBuilder.DropColumn(
                name: "DownloadUrl",
                table: "ApiActivity");

            migrationBuilder.RenameColumn(
                name: "UniqueKey",
                table: "ApiActivity",
                newName: "UniqueFilenameKey");
        }
    }
}
