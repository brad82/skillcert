using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillCert.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetencyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "competency_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competency_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "review_signatures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_signatures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "competency_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    short_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    recertification_days = table.Column<int>(type: "integer", nullable: true),
                    allows_self_review = table.Column<bool>(type: "boolean", nullable: false),
                    allows_peer_review = table.Column<bool>(type: "boolean", nullable: false),
                    invalidates_previous_reviews = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resources = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competency_revisions", x => x.id);
                    table.UniqueConstraint("ak_competency_revision_id_competency_id", x => new { x.id, x.competency_id });
                    table.CheckConstraint("ck_competency_revisions_recertification_days_positive", "recertification_days IS NULL OR recertification_days > 0");
                    table.ForeignKey(
                        name: "fk_competency_revisions_competencies_competency_id",
                        column: x => x.competency_id,
                        principalTable: "competencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_list_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    heading_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    heading_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    parent_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Heading")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competency_list_nodes", x => x.id);
                    table.UniqueConstraint("ak_competency_list_nodes_id_competency_list_id_kind", x => new { x.id, x.competency_list_id, x.kind });
                    table.CheckConstraint("ck_competency_list_nodes_parent_is_heading", "parent_kind = 'Heading'");
                    table.CheckConstraint("ck_competency_list_nodes_shape", "(kind = 'Heading' AND heading_title IS NOT NULL AND competency_id IS NULL) OR (kind = 'Competency' AND competency_id IS NOT NULL AND heading_title IS NULL AND heading_code IS NULL)");
                    table.ForeignKey(
                        name: "fk_competency_list_nodes_competencies_competency_id",
                        column: x => x.competency_id,
                        principalTable: "competencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_list_nodes_competency_list_nodes_parent_node_id_",
                        columns: x => new { x.parent_node_id, x.competency_list_id, x.parent_kind },
                        principalTable: "competency_list_nodes",
                        principalColumns: new[] { "id", "competency_list_id", "kind" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_list_nodes_competency_lists_competency_list_id",
                        column: x => x.competency_list_id,
                        principalTable: "competency_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_list_assignments",
                columns: table => new
                {
                    user_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_list_assignments", x => new { x.user_group_id, x.competency_list_id });
                    table.ForeignKey(
                        name: "fk_group_list_assignments_competency_lists_competency_list_id",
                        column: x => x.competency_list_id,
                        principalTable: "competency_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_list_assignments_user_groups_user_group_id",
                        column: x => x.user_group_id,
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_memberships",
                columns: table => new
                {
                    user_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_group_memberships", x => new { x.user_group_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_user_group_memberships_user_groups_user_group_id",
                        column: x => x.user_group_id,
                        principalTable: "user_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_group_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competency_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewer_classification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    review_signature_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmation_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competency_reviews", x => x.id);
                    table.CheckConstraint("ck_competency_reviews_confirmed_fields", "(confirmation_status = 'Confirmed') = (confirmed_at IS NOT NULL AND confirmed_by_user_id IS NOT NULL)");
                    table.CheckConstraint("ck_competency_reviews_rejected_fields", "(confirmation_status = 'Rejected') = (rejected_at IS NOT NULL AND rejected_by_user_id IS NOT NULL AND rejection_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_competency_reviews_competencies_competency_id",
                        column: x => x.competency_id,
                        principalTable: "competencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_competency_revision_competency_revision_",
                        columns: x => new { x.competency_revision_id, x.competency_id },
                        principalTable: "competency_revisions",
                        principalColumns: new[] { "id", "competency_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_domain_users_candidate_user_id",
                        column: x => x.candidate_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_domain_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_domain_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_domain_users_rejected_by_user_id",
                        column: x => x.rejected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_domain_users_reviewer_user_id",
                        column: x => x.reviewer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_review_signatures_review_signature_id",
                        column: x => x.review_signature_id,
                        principalTable: "review_signatures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_competency_reviews_reviewer_classifications_reviewer_classi",
                        column: x => x.reviewer_classification_id,
                        principalTable: "reviewer_classifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revision_permitted_classifications",
                columns: table => new
                {
                    competency_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_classification_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revision_permitted_classifications", x => new { x.competency_revision_id, x.reviewer_classification_id });
                    table.ForeignKey(
                        name: "fk_revision_permitted_classifications_competency_revisions_com",
                        column: x => x.competency_revision_id,
                        principalTable: "competency_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_revision_permitted_classifications_reviewer_classifications",
                        column: x => x.reviewer_classification_id,
                        principalTable: "reviewer_classifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_competencies_normalized_code",
                table: "competencies",
                column: "normalized_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_competency_list_nodes_competency_id",
                table: "competency_list_nodes",
                column: "competency_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_list_nodes_competency_list_id_competency_id",
                table: "competency_list_nodes",
                columns: new[] { "competency_list_id", "competency_id" },
                unique: true,
                filter: "competency_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_competency_list_nodes_competency_list_id_parent_node_id_sor",
                table: "competency_list_nodes",
                columns: new[] { "competency_list_id", "parent_node_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_competency_list_nodes_parent_node_id_competency_list_id_par",
                table: "competency_list_nodes",
                columns: new[] { "parent_node_id", "competency_list_id", "parent_kind" });

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_candidate_user_id_competency_id_reviewed",
                table: "competency_reviews",
                columns: new[] { "candidate_user_id", "competency_id", "reviewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_competency_id",
                table: "competency_reviews",
                column: "competency_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_competency_revision_id_competency_id",
                table: "competency_reviews",
                columns: new[] { "competency_revision_id", "competency_id" });

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_confirmed_by_user_id",
                table: "competency_reviews",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_created_by_user_id",
                table: "competency_reviews",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_pending_by_reviewer",
                table: "competency_reviews",
                columns: new[] { "reviewer_user_id", "reviewed_at" },
                filter: "confirmation_status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_rejected_by_user_id",
                table: "competency_reviews",
                column: "rejected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_review_signature_id",
                table: "competency_reviews",
                column: "review_signature_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_reviews_reviewer_classification_id",
                table: "competency_reviews",
                column: "reviewer_classification_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_revisions_competency_id_revision_number",
                table: "competency_revisions",
                columns: new[] { "competency_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_list_assignments_competency_list_id",
                table: "group_list_assignments",
                column: "competency_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_revision_permitted_classifications_reviewer_classification_",
                table: "revision_permitted_classifications",
                column: "reviewer_classification_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_group_memberships_user_id",
                table: "user_group_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_groups_name",
                table: "user_groups",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competency_list_nodes");

            migrationBuilder.DropTable(
                name: "competency_reviews");

            migrationBuilder.DropTable(
                name: "group_list_assignments");

            migrationBuilder.DropTable(
                name: "revision_permitted_classifications");

            migrationBuilder.DropTable(
                name: "user_group_memberships");

            migrationBuilder.DropTable(
                name: "review_signatures");

            migrationBuilder.DropTable(
                name: "competency_lists");

            migrationBuilder.DropTable(
                name: "competency_revisions");

            migrationBuilder.DropTable(
                name: "user_groups");

            migrationBuilder.DropTable(
                name: "competencies");
        }
    }
}
