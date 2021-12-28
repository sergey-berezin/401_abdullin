using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace _401_abdullin.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hash = table.Column<int>(type: "INTEGER", nullable: false),
                    Bitmap = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecognizedObjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessedImageId = table.Column<int>(type: "INTEGER", nullable: false),
                    X1 = table.Column<float>(type: "REAL", nullable: false),
                    Y1 = table.Column<float>(type: "REAL", nullable: false),
                    X2 = table.Column<float>(type: "REAL", nullable: false),
                    Y2 = table.Column<float>(type: "REAL", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecognizedObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecognizedObjects_ProcessedImages_ProcessedImageId",
                        column: x => x.ProcessedImageId,
                        principalTable: "ProcessedImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecognizedObjects_ProcessedImageId",
                table: "RecognizedObjects",
                column: "ProcessedImageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecognizedObjects");

            migrationBuilder.DropTable(
                name: "ProcessedImages");
        }
    }
}
