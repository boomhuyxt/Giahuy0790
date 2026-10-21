using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Giahuy0790.Migrations
{
    /// <inheritdoc />
    public partial class AddScreenTimeAndDeviceSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kid_screen_times",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kid_id = table.Column<int>(type: "integer", nullable: false),
                    device_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Máy tính / Điện thoại của bé"),
                    daily_limit_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 120),
                    weekend_limit_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 180),
                    is_daily_limit_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_bedtime_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    bedtime_start = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "21:30"),
                    bedtime_end = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "06:30"),
                    is_allowed_window_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    allowed_start_time = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "07:00"),
                    allowed_end_time = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "21:00"),
                    is_locked_now = table.Column<bool>(type: "boolean", nullable: false),
                    lock_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bonus_minutes_today = table.Column<int>(type: "integer", nullable: false),
                    bonus_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    used_minutes_today = table.Column<int>(type: "integer", nullable: false),
                    last_usage_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lock_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    schedule_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kid_screen_times", x => x.id);
                    table.ForeignKey(
                        name: "FK_kid_screen_times_kids_kid_id",
                        column: x => x.kid_id,
                        principalTable: "kids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kid_screen_times_kid_id",
                table: "kid_screen_times",
                column: "kid_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kid_screen_times");
        }
    }
}
