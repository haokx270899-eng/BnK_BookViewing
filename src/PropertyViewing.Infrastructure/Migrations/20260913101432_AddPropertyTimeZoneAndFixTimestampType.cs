using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyViewing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyTimeZoneAndFixTimestampType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "viewings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "viewings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "properties",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Europe/London");

            migrationBuilder.UpdateData(
                table: "properties",
                keyColumn: "Id",
                keyValue: 1,
                column: "TimeZoneId",
                value: "Europe/London");

            migrationBuilder.UpdateData(
                table: "properties",
                keyColumn: "Id",
                keyValue: 2,
                column: "TimeZoneId",
                value: "America/New_York");

            migrationBuilder.UpdateData(
                table: "properties",
                keyColumn: "Id",
                keyValue: 3,
                column: "TimeZoneId",
                value: "Asia/Ho_Chi_Minh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "properties");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "viewings",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "viewings",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
