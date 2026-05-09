using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineTraker.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovieRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImdbID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PosterUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedByUsername = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMaps_SeedMovieId",
                table: "UserMaps",
                column: "SeedMovieId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMaps_Movies_SeedMovieId",
                table: "UserMaps",
                column: "SeedMovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMaps_Movies_SeedMovieId",
                table: "UserMaps");

            migrationBuilder.DropTable(
                name: "MovieRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserMaps_SeedMovieId",
                table: "UserMaps");
        }
    }
}
