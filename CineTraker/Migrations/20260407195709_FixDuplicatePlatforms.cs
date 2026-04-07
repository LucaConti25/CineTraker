using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineTraker.Migrations
{
    /// <inheritdoc />
    public partial class FixDuplicatePlatforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StreamingSources",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingSources_Name",
                table: "StreamingSources",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StreamingSources_Name",
                table: "StreamingSources");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StreamingSources",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
