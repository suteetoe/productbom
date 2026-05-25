using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecreateBomProductionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bom_production_detail", schema: "public");
            migrationBuilder.DropTable(name: "bom_production_order_lines", schema: "public");
            migrationBuilder.DropTable(name: "bom_production", schema: "public");
            migrationBuilder.DropTable(name: "bom_production_orders", schema: "public");

            migrationBuilder.CreateTable(
                name: "bom_productions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: false),
                    doc_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    doc_time = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_productions", x => x.id);
                    table.UniqueConstraint("AK_bom_productions_doc_no", x => x.doc_no);
                });

            migrationBuilder.CreateTable(
                name: "bom_production_orders",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doc_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: false),
                    ref_doc_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ref_doc_date = table.Column<DateOnly>(type: "date", nullable: false),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_production_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_production_orders_bom_productions_doc_no",
                        column: x => x.doc_no,
                        principalSchema: "public",
                        principalTable: "bom_productions",
                        principalColumn: "doc_no",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bom_production_details",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doc_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_production_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_production_details_bom_productions_doc_no",
                        column: x => x.doc_no,
                        principalSchema: "public",
                        principalTable: "bom_productions",
                        principalColumn: "doc_no",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "idx_bom_productions_doc_date", schema: "public", table: "bom_productions", column: "doc_date");
            migrationBuilder.CreateIndex(name: "idx_bom_productions_doc_no", schema: "public", table: "bom_productions", column: "doc_no", unique: true);
            migrationBuilder.CreateIndex(name: "idx_bom_production_orders_doc_date", schema: "public", table: "bom_production_orders", column: "doc_date");
            migrationBuilder.CreateIndex(name: "idx_bom_production_orders_doc_no", schema: "public", table: "bom_production_orders", column: "doc_no");
            migrationBuilder.CreateIndex(name: "idx_bom_production_orders_item_code", schema: "public", table: "bom_production_orders", column: "item_code");
            migrationBuilder.CreateIndex(name: "idx_bom_production_orders_ref_doc_no", schema: "public", table: "bom_production_orders", column: "ref_doc_no");
            migrationBuilder.CreateIndex(name: "idx_bom_production_details_doc_no", schema: "public", table: "bom_production_details", column: "doc_no");
            migrationBuilder.CreateIndex(name: "idx_bom_production_details_item_code", schema: "public", table: "bom_production_details", column: "item_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bom_production_details", schema: "public");
            migrationBuilder.DropTable(name: "bom_production_orders", schema: "public");
            migrationBuilder.DropTable(name: "bom_productions", schema: "public");

            migrationBuilder.CreateTable(
                name: "bom_production",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: false),
                    doc_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    doc_time = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_production", x => x.id);
                    table.UniqueConstraint("AK_bom_production_doc_no", x => x.doc_no);
                });

            migrationBuilder.CreateTable(
                name: "bom_production_detail",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doc_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_production_detail", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_production_detail_bom_production_doc_no",
                        column: x => x.doc_no,
                        principalSchema: "public",
                        principalTable: "bom_production",
                        principalColumn: "doc_no",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bom_production_orders",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bom_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    source_so_numbers = table.Column<string[]>(type: "text[]", nullable: false),
                    source_doc_date_from = table.Column<DateOnly>(type: "date", nullable: true),
                    source_doc_date_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_via = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "UI"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_production_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_production_orders_bom_boms_bom_id",
                        column: x => x.bom_id,
                        principalSchema: "public",
                        principalTable: "bom_boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_production_order_lines",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    production_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    material_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    required_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_production_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_production_order_lines_bom_production_orders_production~",
                        column: x => x.production_order_id,
                        principalSchema: "public",
                        principalTable: "bom_production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "idx_bom_production_doc_date", schema: "public", table: "bom_production", column: "doc_date");
            migrationBuilder.CreateIndex(name: "idx_bom_production_doc_no", schema: "public", table: "bom_production", column: "doc_no", unique: true);
            migrationBuilder.CreateIndex(name: "idx_bom_production_detail_doc_no", schema: "public", table: "bom_production_detail", column: "doc_no");
            migrationBuilder.CreateIndex(name: "idx_po_lines_production_order_id", schema: "public", table: "bom_production_order_lines", column: "production_order_id");
            migrationBuilder.CreateIndex(name: "idx_production_orders_created_at", schema: "public", table: "bom_production_orders", column: "created_at", descending: new bool[0]);
            migrationBuilder.CreateIndex(name: "idx_production_orders_item_code", schema: "public", table: "bom_production_orders", column: "item_code");
            migrationBuilder.CreateIndex(name: "idx_production_orders_source_so", schema: "public", table: "bom_production_orders", column: "source_so_numbers").Annotation("Npgsql:IndexMethod", "gin");
            migrationBuilder.CreateIndex(name: "idx_production_orders_status", schema: "public", table: "bom_production_orders", column: "status");
            migrationBuilder.CreateIndex(name: "IX_bom_production_orders_bom_id", schema: "public", table: "bom_production_orders", column: "bom_id");
            migrationBuilder.CreateIndex(name: "IX_bom_production_orders_order_no", schema: "public", table: "bom_production_orders", column: "order_no", unique: true);
        }
    }
}
