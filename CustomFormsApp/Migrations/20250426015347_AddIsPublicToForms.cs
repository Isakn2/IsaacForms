using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomFormsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicToForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Forms");
        }
    }
}
