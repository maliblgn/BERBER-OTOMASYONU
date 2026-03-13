using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftetroBarber.Migrations
{
    /// <inheritdoc />
    public partial class AddIsClosedAndNullableBarberId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkingHours_Barbers_BarberId",
                table: "WorkingHours");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarberId",
                table: "WorkingHours",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "WorkingHours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkingHours_Barbers_BarberId",
                table: "WorkingHours",
                column: "BarberId",
                principalTable: "Barbers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkingHours_Barbers_BarberId",
                table: "WorkingHours");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "WorkingHours");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarberId",
                table: "WorkingHours",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkingHours_Barbers_BarberId",
                table: "WorkingHours",
                column: "BarberId",
                principalTable: "Barbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
