using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantHardeningAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Projects_Id_OrganizationId",
                table: "Projects",
                columns: new[] { "Id", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OrganizationId",
                table: "Tasks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OrganizationId_CreatedAtUtc",
                table: "Tasks",
                columns: new[] { "OrganizationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OrganizationId_DueDateUtc",
                table: "Tasks",
                columns: new[] { "OrganizationId", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OrganizationId_ProjectId",
                table: "Tasks",
                columns: new[] { "OrganizationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId_OrganizationId",
                table: "Tasks",
                columns: new[] { "ProjectId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Id_OrganizationId",
                table: "Projects",
                columns: new[] { "Id", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Projects_ProjectId_OrganizationId",
                table: "Tasks",
                columns: new[] { "ProjectId", "OrganizationId" },
                principalTable: "Projects",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Projects_ProjectId_OrganizationId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OrganizationId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OrganizationId_CreatedAtUtc",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OrganizationId_DueDateUtc",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OrganizationId_ProjectId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ProjectId_OrganizationId",
                table: "Tasks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Projects_Id_OrganizationId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Id_OrganizationId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
