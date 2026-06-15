using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaSkincare.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIsActiveSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActiveSnapshot",
                table: "Products",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActiveSnapshot",
                table: "Products");
        }
    }
}
