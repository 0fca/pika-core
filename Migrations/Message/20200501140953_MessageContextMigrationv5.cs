using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PikaCore.Migrations.Message
{
    public partial class MessageContextMigrationv5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleToWho",
                table: "Messages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "VisibleToWho",
                table: "Messages",
                type: "text[]",
                nullable: true,
                defaultValue: null);
        }
    }
}
