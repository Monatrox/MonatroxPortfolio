using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonatroxPortfolio.Projects.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProjectPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects_ProjectPages",
                columns: table => new
                {
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects_ProjectPages", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_ProjectPages_Projects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects_Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects_ProjectPages");
        }
    }
}
