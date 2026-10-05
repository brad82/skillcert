using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillCert.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compliance_checkpoints",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_compliant = table.Column<bool>(type: "boolean", nullable: false),
                    completion_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compliance_checkpoints", x => new { x.user_id, x.competency_list_id });
                    table.ForeignKey(
                        name: "fk_compliance_checkpoints_competency_lists_competency_list_id",
                        column: x => x.competency_list_id,
                        principalTable: "competency_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_compliance_checkpoints_domain_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completion_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    blob_path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    sha256hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    trigger = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_training_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_training_records_competency_lists_competency_list_id",
                        column: x => x.competency_list_id,
                        principalTable: "competency_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_training_records_domain_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_compliance_checkpoints_competency_list_id",
                table: "compliance_checkpoints",
                column: "competency_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_training_records_competency_list_id",
                table: "training_records",
                column: "competency_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_training_records_user_id_generated_at",
                table: "training_records",
                columns: new[] { "user_id", "generated_at" });

            migrationBuilder.CreateIndex(
                name: "ux_training_records_automatic",
                table: "training_records",
                columns: new[] { "user_id", "competency_list_id", "completion_date" },
                unique: true,
                filter: "trigger = 'ComplianceAchieved'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compliance_checkpoints");

            migrationBuilder.DropTable(
                name: "training_records");
        }
    }
}
