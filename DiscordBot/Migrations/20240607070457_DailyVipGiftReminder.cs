using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class DailyVipGiftReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InstanceReminderId",
                table: "InstanceReminderSettings",
                newName: "ReminderId");

            migrationBuilder.CreateTable(
                name: "DailyVipGiftReminderSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ReminderId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyVipGiftReminderSettings", x => new { x.GuildId, x.UserId, x.ReminderId });
                    table.ForeignKey(
                        name: "FK_DailyVipGiftReminderSettings_GuildUserSettings_GuildId_UserId",
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
                name: "DailyVipGiftReminderSettings");

            migrationBuilder.RenameColumn(
                name: "ReminderId",
                table: "InstanceReminderSettings",
                newName: "InstanceReminderId");
        }
    }
}
