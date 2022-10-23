using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenQMS.Migrations
{
    public partial class CreateProductQMS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapaId",
                table: "Change",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Change",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "AppDocument",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Product",
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
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Product_AspNetUsers_EditedBy",
                        column: x => x.EditedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Deviation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Evaluation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcceptedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deviation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deviation_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Capa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    DeviationId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreventiveAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeterminedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeterminedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Assessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcceptedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Implementation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capa_Deviation_DeviationId",
                        column: x => x.DeviationId,
                        principalTable: "Deviation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Capa_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Change_CapaId",
                table: "Change",
                column: "CapaId");

            migrationBuilder.CreateIndex(
                name: "IX_Change_ProductId",
                table: "Change",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Capa_DeviationId",
                table: "Capa",
                column: "DeviationId");

            migrationBuilder.CreateIndex(
                name: "IX_Capa_ProductId",
                table: "Capa",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Deviation_ProductId",
                table: "Deviation",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ApprovedBy",
                table: "Product",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Product_EditedBy",
                table: "Product",
                column: "EditedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Change_Capa_CapaId",
                table: "Change",
                column: "CapaId",
                principalTable: "Capa",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Change_Product_ProductId",
                table: "Change",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Change_Capa_CapaId",
                table: "Change");

            migrationBuilder.DropForeignKey(
                name: "FK_Change_Product_ProductId",
                table: "Change");

            migrationBuilder.DropTable(
                name: "Capa");

            migrationBuilder.DropTable(
                name: "Deviation");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Change_CapaId",
                table: "Change");

            migrationBuilder.DropIndex(
                name: "IX_Change_ProductId",
                table: "Change");

            migrationBuilder.DropColumn(
                name: "CapaId",
                table: "Change");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Change");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "AppDocument");
        }
    }
}
