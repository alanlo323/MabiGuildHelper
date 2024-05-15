using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class DiscordDbMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceMessageId = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorUsername = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorId = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelId = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelMention = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelTopic = table.Column<string>(type: "TEXT", nullable: true),
                    ServerId = table.Column<string>(type: "TEXT", nullable: true),
                    ServerName = table.Column<string>(type: "TEXT", nullable: true),
                    ServerDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ServerMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EmbedsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    CleanContent = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
