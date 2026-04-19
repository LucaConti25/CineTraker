using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineTraker.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedMovieIdToUserMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeedMovieId",
                table: "UserMaps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeedMovieId",
                table: "UserMaps");
        }
    }
}
