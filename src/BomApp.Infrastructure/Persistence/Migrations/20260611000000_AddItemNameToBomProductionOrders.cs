using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemNameToBomProductionOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.bom_production_orders
                ADD COLUMN IF NOT EXISTS item_name character varying(255) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.bom_production_orders
                DROP COLUMN IF EXISTS item_name;
                """);
        }
    }
}
