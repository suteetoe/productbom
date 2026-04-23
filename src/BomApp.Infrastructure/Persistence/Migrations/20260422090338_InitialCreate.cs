using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bom");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "bom",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "boms",
                schema: "bom",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    yield_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    yield_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bom_assignments",
                schema: "bom",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    assigned_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_assignments_boms_bom_id",
                        column: x => x.bom_id,
                        principalSchema: "bom",
                        principalTable: "boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_lines",
                schema: "bom",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    bom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    material_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sub_bom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_bom_lines_boms_bom_id",
                        column: x => x.bom_id,
                        principalSchema: "bom",
                        principalTable: "boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bom_lines_boms_sub_bom_id",
                        column: x => x.sub_bom_id,
                        principalSchema: "bom",
                        principalTable: "boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "production_orders",
                schema: "bom",
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
                    table.PrimaryKey("PK_production_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_orders_boms_bom_id",
                        column: x => x.bom_id,
                        principalSchema: "bom",
                        principalTable: "boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_order_lines",
                schema: "bom",
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
                    table.PrimaryKey("PK_production_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_order_lines_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalSchema: "bom",
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_changed_at",
                schema: "bom",
                table: "audit_logs",
                column: "changed_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_audit_entity",
                schema: "bom",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_bom_assignments_bom_id",
                schema: "bom",
                table: "bom_assignments",
                column: "bom_id");

            migrationBuilder.CreateIndex(
                name: "idx_bom_assignments_item_code",
                schema: "bom",
                table: "bom_assignments",
                column: "item_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_bom_lines_bom_id",
                schema: "bom",
                table: "bom_lines",
                column: "bom_id");

            migrationBuilder.CreateIndex(
                name: "IX_bom_lines_sub_bom_id",
                schema: "bom",
                table: "bom_lines",
                column: "sub_bom_id");

            migrationBuilder.CreateIndex(
                name: "idx_boms_code",
                schema: "bom",
                table: "boms",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_boms_status",
                schema: "bom",
                table: "boms",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_po_lines_production_order_id",
                schema: "bom",
                table: "production_order_lines",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_production_orders_created_at",
                schema: "bom",
                table: "production_orders",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_production_orders_item_code",
                schema: "bom",
                table: "production_orders",
                column: "item_code");

            migrationBuilder.CreateIndex(
                name: "idx_production_orders_source_so",
                schema: "bom",
                table: "production_orders",
                column: "source_so_numbers")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_production_orders_status",
                schema: "bom",
                table: "production_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_bom_id",
                schema: "bom",
                table: "production_orders",
                column: "bom_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_order_no",
                schema: "bom",
                table: "production_orders",
                column: "order_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "bom_assignments",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "bom_lines",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "production_order_lines",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "production_orders",
                schema: "bom");

            migrationBuilder.DropTable(
                name: "boms",
                schema: "bom");
        }
    }
}
