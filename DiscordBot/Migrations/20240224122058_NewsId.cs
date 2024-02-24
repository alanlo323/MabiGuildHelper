using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class NewsId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_News",
                table: "News");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuildNewsOverrides",
                table: "GuildNewsOverrides");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "GuildNewsOverrides");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "News",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "NewsId",
                table: "GuildNewsOverrides",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_News",
                table: "News",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuildNewsOverrides",
                table: "GuildNewsOverrides",
                columns: new[] { "GuildId", "NewsId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_News",
                table: "News");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuildNewsOverrides",
                table: "GuildNewsOverrides");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "News");

            migrationBuilder.DropColumn(
                name: "NewsId",
                table: "GuildNewsOverrides");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "GuildNewsOverrides",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_News",
                table: "News",
                column: "Url");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuildNewsOverrides",
                table: "GuildNewsOverrides",
                columns: new[] { "GuildId", "Url" });
        }
    }
}
