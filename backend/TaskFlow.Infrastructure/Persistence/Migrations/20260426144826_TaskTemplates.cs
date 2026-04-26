using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TaskTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DefaultPriority = table.Column<int>(type: "integer", nullable: false),
                    DefaultDueDaysFromNow = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskTemplates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplateChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    item_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplateChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTemplateChecklistItems_TaskTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplateTags",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplateTags", x => new { x.TemplateId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TaskTemplateTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskTemplateTags_TaskTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OrganizationId_TemplateId",
                table: "Tasks",
                columns: new[] { "OrganizationId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TemplateId",
                table: "Tasks",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplateChecklistItems_TemplateId_item_order",
                table: "TaskTemplateChecklistItems",
                columns: new[] { "TemplateId", "item_order" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_CreatedByUserId",
                table: "TaskTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_OrganizationId_Name",
                table: "TaskTemplates",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplateTags_TagId",
                table: "TaskTemplateTags",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TaskTemplates_TemplateId",
                table: "Tasks",
                column: "TemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TaskTemplates_TemplateId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "TaskTemplateChecklistItems");

            migrationBuilder.DropTable(
                name: "TaskTemplateTags");

            migrationBuilder.DropTable(
                name: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OrganizationId_TemplateId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_TemplateId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Tasks");
        }
    }
}
