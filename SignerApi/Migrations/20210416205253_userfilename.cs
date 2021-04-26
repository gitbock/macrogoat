using Microsoft.EntityFrameworkCore.Migrations;

namespace SignerApi.Migrations
{
    public partial class userfilename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserFilename",
                table: "ApiActivity",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserFilename",
                table: "ApiActivity");
        }
    }
}
