using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseShelfToBomProductionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "wh_code",
                schema: "public",
                table: "bom_production_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shelf_code",
                schema: "public",
                table: "bom_production_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "wh_code",
                schema: "public",
                table: "bom_production_details");

            migrationBuilder.DropColumn(
                name: "shelf_code",
                schema: "public",
                table: "bom_production_details");
        }
    }
}
