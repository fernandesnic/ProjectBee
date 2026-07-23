using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBee.Migrations
{
    /// <inheritdoc />
    public partial class AlterandoTiposStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdressNumber",
                table: "Storages");

            migrationBuilder.AlterColumn<string>(
                name: "IdNumber",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AddressNumber",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Storages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Storages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Storages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressNumber",
                table: "Storages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Storages");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Storages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Storages");

            migrationBuilder.AlterColumn<int>(
                name: "IdNumber",
                table: "Storages",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "AdressNumber",
                table: "Storages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
