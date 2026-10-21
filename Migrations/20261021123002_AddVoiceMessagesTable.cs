using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Giahuy0790.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "voice_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sender_avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    kid_id = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    family_group_id = table.Column<int>(type: "integer", nullable: true),
                    audio_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    is_heard = table.Column<bool>(type: "boolean", nullable: false),
                    heard_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_voice_messages_kids_kid_id",
                        column: x => x.kid_id,
                        principalTable: "kids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_voice_messages_kid_id",
                table: "voice_messages",
                column: "kid_id");

            migrationBuilder.CreateIndex(
                name: "IX_voice_messages_kid_id_created_at",
                table: "voice_messages",
                columns: new[] { "kid_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_voice_messages_sender_id",
                table: "voice_messages",
                column: "sender_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "voice_messages");
        }
    }
}
