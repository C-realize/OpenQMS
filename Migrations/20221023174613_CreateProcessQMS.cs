using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenQMS.Migrations
{
    public partial class CreateProcessQMS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessId",
                table: "Deviation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessId",
                table: "Change",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessId",
                table: "Capa",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Process",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EditedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Process", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deviation_ProcessId",
                table: "Deviation",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_Change_ProcessId",
                table: "Change",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_Capa_ProcessId",
                table: "Capa",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Capa_Process_ProcessId",
                table: "Capa",
                column: "ProcessId",
                principalTable: "Process",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Change_Process_ProcessId",
                table: "Change",
                column: "ProcessId",
                principalTable: "Process",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deviation_Process_ProcessId",
                table: "Deviation",
                column: "ProcessId",
                principalTable: "Process",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Capa_Process_ProcessId",
                table: "Capa");

            migrationBuilder.DropForeignKey(
                name: "FK_Change_Process_ProcessId",
                table: "Change");

            migrationBuilder.DropForeignKey(
                name: "FK_Deviation_Process_ProcessId",
                table: "Deviation");

            migrationBuilder.DropTable(
                name: "Process");

            migrationBuilder.DropIndex(
                name: "IX_Deviation_ProcessId",
                table: "Deviation");

            migrationBuilder.DropIndex(
                name: "IX_Change_ProcessId",
                table: "Change");

            migrationBuilder.DropIndex(
                name: "IX_Capa_ProcessId",
                table: "Capa");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "Deviation");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "Change");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "Capa");
        }
    }
}
