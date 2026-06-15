using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaSkincare.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthBenefits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthBenefits",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthBenefits",
                table: "Products");
        }
    }
}
