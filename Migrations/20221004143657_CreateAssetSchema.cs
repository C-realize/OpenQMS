using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenQMS.Migrations
{
    public partial class CreateAssetSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "Deviation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "Change",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "Capa",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EditedBy = table.Column<int>(type: "int", nullable: true),
                    EditedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asset_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Asset_AspNetUsers_EditedBy",
                        column: x => x.EditedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deviation_AssetId",
                table: "Deviation",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Change_AssetId",
                table: "Change",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Capa_AssetId",
                table: "Capa",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ApprovedBy",
                table: "Asset",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_EditedBy",
                table: "Asset",
                column: "EditedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Capa_Asset_AssetId",
                table: "Capa",
                column: "AssetId",
                principalTable: "Asset",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Change_Asset_AssetId",
                table: "Change",
                column: "AssetId",
                principalTable: "Asset",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deviation_Asset_AssetId",
                table: "Deviation",
                column: "AssetId",
                principalTable: "Asset",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Capa_Asset_AssetId",
                table: "Capa");

            migrationBuilder.DropForeignKey(
                name: "FK_Change_Asset_AssetId",
                table: "Change");

            migrationBuilder.DropForeignKey(
                name: "FK_Deviation_Asset_AssetId",
                table: "Deviation");

            migrationBuilder.DropTable(
                name: "Asset");

            migrationBuilder.DropIndex(
                name: "IX_Deviation_AssetId",
                table: "Deviation");

            migrationBuilder.DropIndex(
                name: "IX_Change_AssetId",
                table: "Change");

            migrationBuilder.DropIndex(
                name: "IX_Capa_AssetId",
                table: "Capa");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Deviation");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Change");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Capa");
        }
    }
}
