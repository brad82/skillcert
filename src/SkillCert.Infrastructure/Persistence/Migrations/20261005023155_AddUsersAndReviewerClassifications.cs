using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkillCert.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndReviewerClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reviewer_classifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    affirmation_policy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_classifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_subject_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_administrator = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_reviewer_classifications",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_classification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_reviewer_classifications", x => new { x.user_id, x.reviewer_classification_id });
                    table.ForeignKey(
                        name: "fk_user_reviewer_classifications_reviewer_classifications_revi",
                        column: x => x.reviewer_classification_id,
                        principalTable: "reviewer_classifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_reviewer_classifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "reviewer_classifications",
                columns: new[] { "id", "affirmation_policy", "code", "name" },
                values: new object[,]
                {
                    { new Guid("0199a8c0-0000-7000-8000-000000000001"), "Automatic", "Instructor", "Instructor" },
                    { new Guid("0199a8c0-0000-7000-8000-000000000002"), "ReviewerConfirmation", "Supervisor", "Supervisor" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_classifications_code",
                table: "reviewer_classifications",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_reviewer_classifications_reviewer_classification_id",
                table: "user_reviewer_classifications",
                column: "reviewer_classification_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_external_subject_id",
                table: "users",
                column: "external_subject_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_reviewer_classifications");

            migrationBuilder.DropTable(
                name: "reviewer_classifications");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
