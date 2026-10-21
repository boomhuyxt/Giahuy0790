using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Giahuy0790.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyQrInviteTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_qr_invites",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    family_group_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<string>(type: "text", nullable: false),
                    invite_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    short_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_by_user_id = table.Column<string>(type: "text", nullable: true),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_qr_invites", x => x.id);
                    table.ForeignKey(
                        name: "FK_family_qr_invites_AspNetUsers_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_family_qr_invites_AspNetUsers_used_by_user_id",
                        column: x => x.used_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_family_qr_invites_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_family_qr_invites_created_by_user_id",
                table: "family_qr_invites",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_qr_invites_family_group_id",
                table: "family_qr_invites",
                column: "family_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_family_qr_invites_invite_token",
                table: "family_qr_invites",
                column: "invite_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_qr_invites_short_code",
                table: "family_qr_invites",
                column: "short_code");

            migrationBuilder.CreateIndex(
                name: "IX_family_qr_invites_used_by_user_id",
                table: "family_qr_invites",
                column: "used_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_qr_invites");
        }
    }
}
