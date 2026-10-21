using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Giahuy0790.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyGroupManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "family_group_id",
                table: "kids",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "family_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_family_groups_AspNetUsers_owner_id",
                        column: x => x.owner_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "family_members",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    family_group_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_in_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Parent"),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_family_members_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_family_members_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kids_family_group_id",
                table: "kids",
                column: "family_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_groups_owner_id",
                table: "family_groups",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_family_group_id",
                table: "family_members",
                column: "family_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_user_id",
                table: "family_members",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_kids_family_groups_family_group_id",
                table: "kids",
                column: "family_group_id",
                principalTable: "family_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kids_family_groups_family_group_id",
                table: "kids");

            migrationBuilder.DropTable(
                name: "family_members");

            migrationBuilder.DropTable(
                name: "family_groups");

            migrationBuilder.DropIndex(
                name: "IX_kids_family_group_id",
                table: "kids");

            migrationBuilder.DropColumn(
                name: "family_group_id",
                table: "kids");
        }
    }
}
