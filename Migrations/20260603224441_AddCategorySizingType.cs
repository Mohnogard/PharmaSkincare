using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaSkincare.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySizingType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SizingType",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizingType",
                table: "Categories");
        }
    }
}
