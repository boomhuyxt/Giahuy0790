using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Giahuy0790.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedItemsForKids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blocked_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kid_id = table.Column<int>(type: "integer", nullable: false),
                    item_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Website"),
                    target = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "Khác"),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by_user_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocked_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_blocked_items_AspNetUsers_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_blocked_items_kids_kid_id",
                        column: x => x.kid_id,
                        principalTable: "kids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blocked_items_created_by_user_id",
                table: "blocked_items",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_blocked_items_kid_id",
                table: "blocked_items",
                column: "kid_id");

            migrationBuilder.CreateIndex(
                name: "IX_blocked_items_kid_id_target",
                table: "blocked_items",
                columns: new[] { "kid_id", "target" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocked_items");
        }
    }
}
