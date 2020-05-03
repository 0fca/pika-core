using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PikaCore.Migrations.Message
{
    public partial class MessageContextMigrationv4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "Messages",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string[]>(
                name: "VisibleToWho",
                table: "Messages",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<bool>(
                name: "IsFixed",
                table: "Issues",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VisibleToWho",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsFixed",
                table: "Issues");
        }
    }
}
