using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class DailyDungeonInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "DailyDungeonInfoChannelId",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "DailyDungeonInfoMessageId",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyDungeonInfoChannelId",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "DailyDungeonInfoMessageId",
                table: "GuildSettings");
        }
    }
}
