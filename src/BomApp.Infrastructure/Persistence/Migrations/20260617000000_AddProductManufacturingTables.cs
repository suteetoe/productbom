using BomApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BomDbContext))]
    [Migration("20260617000000_AddProductManufacturingTables")]
    public partial class AddProductManufacturingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.bom_material_process (
                    doc_no varchar(50) PRIMARY KEY,
                    doc_date varchar(10) NOT NULL,
                    wh_code varchar(50) NOT NULL,
                    shelf_code varchar(50) NOT NULL,
                    remark varchar(255) NOT NULL DEFAULT '',
                    total_cost numeric NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS public.bom_material_process_use (
                    doc_no varchar(50) NOT NULL REFERENCES public.bom_material_process(doc_no) ON DELETE CASCADE,
                    item_code varchar(50) NOT NULL,
                    qty numeric NOT NULL,
                    unit_code varchar(50) NOT NULL,
                    wh_code varchar(50) NOT NULL,
                    shelf_code varchar(50) NOT NULL,
                    cost_per_unit numeric NOT NULL DEFAULT 0,
                    total_cost numeric NOT NULL DEFAULT 0,
                    line_number integer NOT NULL,
                    CONSTRAINT pk_bom_material_process_use PRIMARY KEY (doc_no, line_number),
                    CONSTRAINT ck_bom_material_process_use_qty_positive CHECK (qty > 0)
                );

                CREATE TABLE IF NOT EXISTS public.bom_material_process_finish_good (
                    doc_no varchar(50) NOT NULL REFERENCES public.bom_material_process(doc_no) ON DELETE CASCADE,
                    item_code varchar(50) NOT NULL,
                    qty numeric NOT NULL,
                    unit_code varchar(50) NOT NULL,
                    wh_code varchar(50) NOT NULL,
                    shelf_code varchar(50) NOT NULL,
                    cost_per_unit numeric NOT NULL DEFAULT 0,
                    total_cost numeric NOT NULL DEFAULT 0,
                    line_number integer NOT NULL,
                    CONSTRAINT pk_bom_material_process_finish_good PRIMARY KEY (doc_no, line_number),
                    CONSTRAINT ck_bom_material_process_finish_good_qty_positive CHECK (qty > 0)
                );

                CREATE INDEX IF NOT EXISTS idx_bom_material_process_doc_date
                    ON public.bom_material_process(doc_date DESC);

                CREATE INDEX IF NOT EXISTS idx_bom_material_process_use_doc_no
                    ON public.bom_material_process_use(doc_no);

                CREATE INDEX IF NOT EXISTS idx_bom_material_process_use_item_code
                    ON public.bom_material_process_use(item_code);

                CREATE INDEX IF NOT EXISTS idx_bom_material_process_finish_good_doc_no
                    ON public.bom_material_process_finish_good(doc_no);

                CREATE INDEX IF NOT EXISTS idx_bom_material_process_finish_good_item_code
                    ON public.bom_material_process_finish_good(item_code);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS public.bom_material_process_finish_good;
                DROP TABLE IF EXISTS public.bom_material_process_use;
                DROP TABLE IF EXISTS public.bom_material_process;
                """);
        }
    }
}
