/*
This file is part of the OpenQMS.net project (https://github.com/C-realize/OpenQMS).
Copyright (C) 2022-2024  C-realize IT Services SRL (https://www.c-realize.com)

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see http://www.gnu.org/licenses/.
*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenQMS.Migrations
{
    public partial class Release110 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asset_AspNetUsers_ApprovedBy",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_Asset_AspNetUsers_EditedBy",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_Material_AspNetUsers_ApprovedBy",
                table: "Material");

            migrationBuilder.DropForeignKey(
                name: "FK_Material_AspNetUsers_EditedBy",
                table: "Material");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_AspNetUsers_ApprovedBy",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_AspNetUsers_EditedBy",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_ApprovedBy",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_EditedBy",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Material_ApprovedBy",
                table: "Material");

            migrationBuilder.DropIndex(
                name: "IX_Material_EditedBy",
                table: "Material");

            migrationBuilder.DropIndex(
                name: "IX_Asset_ApprovedBy",
                table: "Asset");

            migrationBuilder.DropIndex(
                name: "IX_Asset_EditedBy",
                table: "Asset");

            migrationBuilder.AlterColumn<string>(
                name: "EditedBy",
                table: "Product",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "Product",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EditedBy",
                table: "Material",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "Material",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EditedBy",
                table: "Asset",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "Asset",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "EditedBy",
                table: "Product",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "Product",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EditedBy",
                table: "Material",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "Material",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EditedBy",
                table: "Asset",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedBy",
                table: "Asset",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_ApprovedBy",
                table: "Product",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Product_EditedBy",
                table: "Product",
                column: "EditedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Material_ApprovedBy",
                table: "Material",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Material_EditedBy",
                table: "Material",
                column: "EditedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ApprovedBy",
                table: "Asset",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_EditedBy",
                table: "Asset",
                column: "EditedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_AspNetUsers_ApprovedBy",
                table: "Asset",
                column: "ApprovedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_AspNetUsers_EditedBy",
                table: "Asset",
                column: "EditedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Material_AspNetUsers_ApprovedBy",
                table: "Material",
                column: "ApprovedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Material_AspNetUsers_EditedBy",
                table: "Material",
                column: "EditedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_AspNetUsers_ApprovedBy",
                table: "Product",
                column: "ApprovedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_AspNetUsers_EditedBy",
                table: "Product",
                column: "EditedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
