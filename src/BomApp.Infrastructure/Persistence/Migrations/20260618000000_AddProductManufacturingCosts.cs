using BomApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BomDbContext))]
    [Migration("20260618000000_AddProductManufacturingCosts")]
    public partial class AddProductManufacturingCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.bom_material_process_use
                    ADD COLUMN IF NOT EXISTS cost_per_unit numeric NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS total_cost numeric NOT NULL DEFAULT 0;

                ALTER TABLE IF EXISTS public.bom_material_process_finish_good
                    ADD COLUMN IF NOT EXISTS cost_per_unit numeric NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS total_cost numeric NOT NULL DEFAULT 0;

                ALTER TABLE IF EXISTS public.bom_material_process
                    ADD COLUMN IF NOT EXISTS total_cost numeric NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.bom_material_process_finish_good
                    DROP COLUMN IF EXISTS total_cost,
                    DROP COLUMN IF EXISTS cost_per_unit;

                ALTER TABLE IF EXISTS public.bom_material_process_use
                    DROP COLUMN IF EXISTS total_cost,
                    DROP COLUMN IF EXISTS cost_per_unit;

                ALTER TABLE IF EXISTS public.bom_material_process
                    DROP COLUMN IF EXISTS total_cost;
                """);
        }
    }
}
