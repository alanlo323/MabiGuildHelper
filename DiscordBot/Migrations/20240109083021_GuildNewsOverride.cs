using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class GuildNewsOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildNewsOverrides",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    NewsUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildNewsOverrides", x => new { x.GuildId, x.NewsUrl });
                    table.ForeignKey(
                        name: "FK_GuildNewsOverrides_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildNewsOverrides");
        }
    }
}
