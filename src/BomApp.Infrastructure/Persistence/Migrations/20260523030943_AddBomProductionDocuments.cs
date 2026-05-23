using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBomProductionDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "idx_bom_production_doc_date",
                schema: "public",
                table: "bom_production",
                column: "doc_date");

            migrationBuilder.CreateIndex(
                name: "idx_bom_production_doc_no",
                schema: "public",
                table: "bom_production",
                column: "doc_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_bom_production_detail_doc_no",
                schema: "public",
                table: "bom_production_detail",
                column: "doc_no");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bom_production_detail",
                schema: "public");

            migrationBuilder.DropTable(
                name: "bom_production",
                schema: "public");
        }
    }
}
