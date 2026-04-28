using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HazelNet_Infrastractire.Migrations
{
    /// <inheritdoc />
    public partial class Adddescriptionpropertytodeck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLog_ReviewHistory_ReviewHistoryId",
                table: "ReviewLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewLog",
                table: "ReviewLog");

            migrationBuilder.RenameTable(
                name: "ReviewLog",
                newName: "ReviewLogs");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewLog_ReviewHistoryId",
                table: "ReviewLogs",
                newName: "IX_ReviewLogs_ReviewHistoryId");

            migrationBuilder.AddColumn<string>(
                name: "DeckDescription",
                table: "Decks",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewLogs",
                table: "ReviewLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLogs_ReviewHistory_ReviewHistoryId",
                table: "ReviewLogs",
                column: "ReviewHistoryId",
                principalTable: "ReviewHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLogs_ReviewHistory_ReviewHistoryId",
                table: "ReviewLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewLogs",
                table: "ReviewLogs");

            migrationBuilder.DropColumn(
                name: "DeckDescription",
                table: "Decks");

            migrationBuilder.RenameTable(
                name: "ReviewLogs",
                newName: "ReviewLog");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewLogs_ReviewHistoryId",
                table: "ReviewLog",
                newName: "IX_ReviewLog_ReviewHistoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewLog",
                table: "ReviewLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLog_ReviewHistory_ReviewHistoryId",
                table: "ReviewLog",
                column: "ReviewHistoryId",
                principalTable: "ReviewHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
