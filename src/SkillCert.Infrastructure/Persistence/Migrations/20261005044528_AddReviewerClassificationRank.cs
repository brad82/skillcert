using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillCert.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerClassificationRank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rank",
                table: "reviewer_classifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "reviewer_classifications",
                keyColumn: "id",
                keyValue: new Guid("0199a8c0-0000-7000-8000-000000000001"),
                column: "rank",
                value: 10);

            migrationBuilder.UpdateData(
                table: "reviewer_classifications",
                keyColumn: "id",
                keyValue: new Guid("0199a8c0-0000-7000-8000-000000000002"),
                column: "rank",
                value: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rank",
                table: "reviewer_classifications");
        }
    }
}
