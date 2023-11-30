using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ErinnTimeChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ErinnTimeMessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DailyEffectChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DailyEffectMessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DailyDungeonInfoChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    DailyDungeonInfoMessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    InstanceResetReminderChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    InstanceResetReminderMessageIdBattle = table.Column<ulong>(type: "INTEGER", nullable: true),
                    InstanceResetReminderMessageIdLife = table.Column<ulong>(type: "INTEGER", nullable: true),
                    InstanceResetReminderMessageIdMisc = table.Column<ulong>(type: "INTEGER", nullable: true),
                    InstanceResetReminderMessageIdOneDay = table.Column<ulong>(type: "INTEGER", nullable: true),
                    InstanceResetReminderMessageIdToday = table.Column<ulong>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "GuildUserSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildUserSettings", x => new { x.GuildId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GuildUserSettings_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstanceReminderSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    InstanceReminderId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceReminderSettings", x => new { x.GuildId, x.UserId, x.InstanceReminderId });
                    table.ForeignKey(
                        name: "FK_InstanceReminderSettings_GuildUserSettings_GuildId_UserId",
                        columns: x => new { x.GuildId, x.UserId },
                        principalTable: "GuildUserSettings",
                        principalColumns: new[] { "GuildId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstanceReminderSettings");

            migrationBuilder.DropTable(
                name: "GuildUserSettings");

            migrationBuilder.DropTable(
                name: "GuildSettings");
        }
    }
}
