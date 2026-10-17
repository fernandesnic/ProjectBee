using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBee.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageIdNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes do índice, a API aceitava código repetido. Para o índice poder ser criado, o armazém
            // mais antigo de cada código fica com ele e os repetidos ganham um sufixo (-2, -3...).
            // Códigos acima de 20 caracteres são cortados antes, para caber na coluna nova.
            // O Down não desfaz essa renomeação: os códigos originais repetidos não voltam.
            migrationBuilder.Sql("""
                UPDATE Storages SET IdNumber = LEFT(IdNumber, 20) WHERE LEN(IdNumber) > 20;

                WITH Duplicates AS (
                    SELECT IdNumber,
                           ROW_NUMBER() OVER (PARTITION BY IdNumber ORDER BY CreatedAt, Id) AS Position
                    FROM Storages
                )
                UPDATE Duplicates
                SET IdNumber = LEFT(IdNumber, 16) + '-' + CAST(Position AS nvarchar(3))
                WHERE Position > 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdNumber",
                table: "Storages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_IdNumber",
                table: "Storages",
                column: "IdNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Storages_IdNumber",
                table: "Storages");

            migrationBuilder.AlterColumn<string>(
                name: "IdNumber",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
