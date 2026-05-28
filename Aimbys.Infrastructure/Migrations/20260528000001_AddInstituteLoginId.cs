using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimbys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstituteLoginId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column nullable first so existing rows are not broken
            // during the ALTER; immediately default them to a random 8-digit
            // placeholder so we can flip it to NOT NULL in one pass.
            migrationBuilder.AddColumn<string>(
                name: "InstituteLoginId",
                table: "Institutes",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "00000000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstituteLoginId",
                table: "Institutes");
        }
    }
}
