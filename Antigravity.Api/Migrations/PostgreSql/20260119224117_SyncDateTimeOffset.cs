using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antigravity.Api.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SyncDateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Avisos_codcli",
                table: "Avisos",
                column: "codcli");

            migrationBuilder.AddForeignKey(
                name: "FK_Avisos_Clients_codcli",
                table: "Avisos",
                column: "codcli",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avisos_Clients_codcli",
                table: "Avisos");

            migrationBuilder.DropIndex(
                name: "IX_Avisos_codcli",
                table: "Avisos");
        }
    }
}
