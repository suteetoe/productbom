using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveToPublicSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bom_assignments_boms_bom_id",
                schema: "bom",
                table: "bom_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_bom_lines_boms_bom_id",
                schema: "bom",
                table: "bom_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_bom_lines_boms_sub_bom_id",
                schema: "bom",
                table: "bom_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_production_order_lines_production_orders_production_order_id",
                schema: "bom",
                table: "production_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_production_orders_boms_bom_id",
                schema: "bom",
                table: "production_orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_production_orders",
                schema: "bom",
                table: "production_orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_production_order_lines",
                schema: "bom",
                table: "production_order_lines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_boms",
                schema: "bom",
                table: "boms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                schema: "bom",
                table: "audit_logs");

            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "bom_lines",
                schema: "bom",
                newName: "bom_lines",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "bom_assignments",
                schema: "bom",
                newName: "bom_assignments",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "production_orders",
                schema: "bom",
                newName: "bom_production_orders",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "production_order_lines",
                schema: "bom",
                newName: "bom_production_order_lines",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "boms",
                schema: "bom",
                newName: "bom_boms",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                schema: "bom",
                newName: "bom_audit_logs",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "IX_production_orders_order_no",
                schema: "public",
                table: "bom_production_orders",
                newName: "IX_bom_production_orders_order_no");

            migrationBuilder.RenameIndex(
                name: "IX_production_orders_bom_id",
                schema: "public",
                table: "bom_production_orders",
                newName: "IX_bom_production_orders_bom_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bom_production_orders",
                schema: "public",
                table: "bom_production_orders",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bom_production_order_lines",
                schema: "public",
                table: "bom_production_order_lines",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bom_boms",
                schema: "public",
                table: "bom_boms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bom_audit_logs",
                schema: "public",
                table: "bom_audit_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_bom_assignments_bom_boms_bom_id",
                schema: "public",
                table: "bom_assignments",
                column: "bom_id",
                principalSchema: "public",
                principalTable: "bom_boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_lines_bom_boms_bom_id",
                schema: "public",
                table: "bom_lines",
                column: "bom_id",
                principalSchema: "public",
                principalTable: "bom_boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_lines_bom_boms_sub_bom_id",
                schema: "public",
                table: "bom_lines",
                column: "sub_bom_id",
                principalSchema: "public",
                principalTable: "bom_boms",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_production_order_lines_bom_production_orders_production~",
                schema: "public",
                table: "bom_production_order_lines",
                column: "production_order_id",
                principalSchema: "public",
                principalTable: "bom_production_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_production_orders_bom_boms_bom_id",
                schema: "public",
                table: "bom_production_orders",
                column: "bom_id",
                principalSchema: "public",
                principalTable: "bom_boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bom_assignments_bom_boms_bom_id",
                schema: "public",
                table: "bom_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_bom_lines_bom_boms_bom_id",
                schema: "public",
                table: "bom_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_bom_lines_bom_boms_sub_bom_id",
                schema: "public",
                table: "bom_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_bom_production_order_lines_bom_production_orders_production~",
                schema: "public",
                table: "bom_production_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_bom_production_orders_bom_boms_bom_id",
                schema: "public",
                table: "bom_production_orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_bom_production_orders",
                schema: "public",
                table: "bom_production_orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_bom_production_order_lines",
                schema: "public",
                table: "bom_production_order_lines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_bom_boms",
                schema: "public",
                table: "bom_boms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_bom_audit_logs",
                schema: "public",
                table: "bom_audit_logs");

            migrationBuilder.EnsureSchema(
                name: "bom");

            migrationBuilder.RenameTable(
                name: "bom_lines",
                schema: "public",
                newName: "bom_lines",
                newSchema: "bom");

            migrationBuilder.RenameTable(
                name: "bom_assignments",
                schema: "public",
                newName: "bom_assignments",
                newSchema: "bom");

            migrationBuilder.RenameTable(
                name: "bom_production_orders",
                schema: "public",
                newName: "production_orders",
                newSchema: "bom");

            migrationBuilder.RenameTable(
                name: "bom_production_order_lines",
                schema: "public",
                newName: "production_order_lines",
                newSchema: "bom");

            migrationBuilder.RenameTable(
                name: "bom_boms",
                schema: "public",
                newName: "boms",
                newSchema: "bom");

            migrationBuilder.RenameTable(
                name: "bom_audit_logs",
                schema: "public",
                newName: "audit_logs",
                newSchema: "bom");

            migrationBuilder.RenameIndex(
                name: "IX_bom_production_orders_order_no",
                schema: "bom",
                table: "production_orders",
                newName: "IX_production_orders_order_no");

            migrationBuilder.RenameIndex(
                name: "IX_bom_production_orders_bom_id",
                schema: "bom",
                table: "production_orders",
                newName: "IX_production_orders_bom_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_production_orders",
                schema: "bom",
                table: "production_orders",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_production_order_lines",
                schema: "bom",
                table: "production_order_lines",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_boms",
                schema: "bom",
                table: "boms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                schema: "bom",
                table: "audit_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_bom_assignments_boms_bom_id",
                schema: "bom",
                table: "bom_assignments",
                column: "bom_id",
                principalSchema: "bom",
                principalTable: "boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_lines_boms_bom_id",
                schema: "bom",
                table: "bom_lines",
                column: "bom_id",
                principalSchema: "bom",
                principalTable: "boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bom_lines_boms_sub_bom_id",
                schema: "bom",
                table: "bom_lines",
                column: "sub_bom_id",
                principalSchema: "bom",
                principalTable: "boms",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_production_order_lines_production_orders_production_order_id",
                schema: "bom",
                table: "production_order_lines",
                column: "production_order_id",
                principalSchema: "bom",
                principalTable: "production_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_production_orders_boms_bom_id",
                schema: "bom",
                table: "production_orders",
                column: "bom_id",
                principalSchema: "bom",
                principalTable: "boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
