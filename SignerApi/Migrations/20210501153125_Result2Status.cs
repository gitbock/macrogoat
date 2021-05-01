using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class Result2Status : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Result",
                table: "ApiActivity",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "EncCertPw",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncCertPw",
                table: "ApiActivity");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ApiActivity",
                newName: "Result");
        }
    }
}
