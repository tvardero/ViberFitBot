using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViberFitBot.ViberApi.Migrations
{
    public partial class AddTable_Track : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Imei = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    DistanceMetres = table.Column<double>(type: "float", nullable: false),
                    FirstDataId = table.Column<int>(type: "int", nullable: false),
                    LatestDataId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tracks_TrackLocation_FirstDataId",
                        column: x => x.FirstDataId,
                        principalTable: "TrackLocation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tracks_TrackLocation_LatestDataId",
                        column: x => x.LatestDataId,
                        principalTable: "TrackLocation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_FirstDataId",
                table: "Tracks",
                column: "FirstDataId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_LatestDataId",
                table: "Tracks",
                column: "LatestDataId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tracks");
        }
    }
}
