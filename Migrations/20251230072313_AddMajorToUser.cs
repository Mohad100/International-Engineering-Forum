using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fourm.Migrations
{
    /// <inheritdoc />
    public partial class AddMajorToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Major",
                table: "Users");
        }
    }
}
