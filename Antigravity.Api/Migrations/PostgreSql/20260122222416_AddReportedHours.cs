using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antigravity.Api.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddReportedHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "reported_hours",
                table: "SiteVisits",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reported_hours",
                table: "SiteVisits");
        }
    }
}
