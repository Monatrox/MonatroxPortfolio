using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonatroxPortfolio.Profile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Profile_Profiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Headline = table.Column<string>(type: "TEXT", nullable: false),
                    Biography = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profile_Career",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", nullable: false),
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    Roles = table.Column<string>(type: "TEXT", nullable: false),
                    Organization = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile_Career", x => new { x.ProfileId, x.Id });
                    table.ForeignKey(
                        name: "FK_Profile_Career_Profile_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profile_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profile_Education",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", nullable: false),
                    Qualifications = table.Column<string>(type: "TEXT", nullable: false),
                    Institution = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile_Education", x => new { x.ProfileId, x.Id });
                    table.ForeignKey(
                        name: "FK_Profile_Education_Profile_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profile_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profile_Skills",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile_Skills", x => new { x.ProfileId, x.Id });
                    table.ForeignKey(
                        name: "FK_Profile_Skills_Profile_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profile_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profile_SocialLinks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile_SocialLinks", x => new { x.ProfileId, x.Id });
                    table.ForeignKey(
                        name: "FK_Profile_SocialLinks_Profile_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profile_Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profile_Career");

            migrationBuilder.DropTable(
                name: "Profile_Education");

            migrationBuilder.DropTable(
                name: "Profile_Skills");

            migrationBuilder.DropTable(
                name: "Profile_SocialLinks");

            migrationBuilder.DropTable(
                name: "Profile_Profiles");
        }
    }
}
