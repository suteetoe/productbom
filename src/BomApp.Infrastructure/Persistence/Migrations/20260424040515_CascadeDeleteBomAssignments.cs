using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteBomAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bom_assignments_boms_bom_id",
                schema: "bom",
                table: "bom_assignments");

            migrationBuilder.AddForeignKey(
                name: "FK_bom_assignments_boms_bom_id",
                schema: "bom",
                table: "bom_assignments",
                column: "bom_id",
                principalSchema: "bom",
                principalTable: "boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bom_assignments_boms_bom_id",
                schema: "bom",
                table: "bom_assignments");

            migrationBuilder.AddForeignKey(
                name: "FK_bom_assignments_boms_bom_id",
                schema: "bom",
                table: "bom_assignments",
                column: "bom_id",
                principalSchema: "bom",
                principalTable: "boms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
