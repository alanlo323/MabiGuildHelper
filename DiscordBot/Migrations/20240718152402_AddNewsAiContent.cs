using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsAiContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiContent",
                table: "News",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HtmlContent",
                table: "News",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiContent",
                table: "News");

            migrationBuilder.DropColumn(
                name: "HtmlContent",
                table: "News");
        }
    }
}
