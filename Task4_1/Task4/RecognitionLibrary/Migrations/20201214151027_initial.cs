using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RecognitionLibrary.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Image = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecognitionImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    ImageDetailsId = table.Column<long>(type: "INTEGER", nullable: true),
                    Label = table.Column<int>(type: "INTEGER", nullable: false),
                    Statistic = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecognitionImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecognitionImages_Blobs_ImageDetailsId",
                        column: x => x.ImageDetailsId,
                        principalTable: "Blobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecognitionImages_ImageDetailsId",
                table: "RecognitionImages",
                column: "ImageDetailsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecognitionImages");

            migrationBuilder.DropTable(
                name: "Blobs");
        }
    }
}
