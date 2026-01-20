using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Antigravity.Api.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddClientTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Avisos\";");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "end_client_time",
                table: "WorkDays",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "start_client_time",
                table: "WorkDays",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "check_in_client_time",
                table: "SiteVisits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "check_out_client_time",
                table: "SiteVisits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "end_client_time",
                table: "Breaks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "start_client_time",
                table: "Breaks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_client_time",
                table: "WorkDays");

            migrationBuilder.DropColumn(
                name: "start_client_time",
                table: "WorkDays");

            migrationBuilder.DropColumn(
                name: "check_in_client_time",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "check_out_client_time",
                table: "SiteVisits");

            migrationBuilder.DropColumn(
                name: "end_client_time",
                table: "Breaks");

            migrationBuilder.DropColumn(
                name: "start_client_time",
                table: "Breaks");
        }
    }
}
