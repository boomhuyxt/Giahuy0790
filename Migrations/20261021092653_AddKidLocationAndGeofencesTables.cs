using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Giahuy0790.Migrations
{
    /// <inheritdoc />
    public partial class AddKidLocationAndGeofencesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kid_geofences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kid_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    radius_meters = table.Column<double>(type: "double precision", nullable: false, defaultValue: 200.0),
                    alert_on_exit = table.Column<bool>(type: "boolean", nullable: false),
                    alert_on_enter = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kid_geofences", x => x.id);
                    table.ForeignKey(
                        name: "FK_kid_geofences_kids_kid_id",
                        column: x => x.kid_id,
                        principalTable: "kids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kid_locations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kid_id = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    accuracy = table.Column<double>(type: "double precision", nullable: true),
                    speed = table.Column<double>(type: "double precision", nullable: true),
                    altitude = table.Column<double>(type: "double precision", nullable: true),
                    battery_level = table.Column<int>(type: "integer", nullable: true),
                    is_charging = table.Column<bool>(type: "boolean", nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kid_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_kid_locations_kids_kid_id",
                        column: x => x.kid_id,
                        principalTable: "kids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kid_geofences_kid_id",
                table: "kid_geofences",
                column: "kid_id");

            migrationBuilder.CreateIndex(
                name: "IX_kid_locations_kid_id",
                table: "kid_locations",
                column: "kid_id");

            migrationBuilder.CreateIndex(
                name: "IX_kid_locations_kid_id_recorded_at",
                table: "kid_locations",
                columns: new[] { "kid_id", "recorded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kid_geofences");

            migrationBuilder.DropTable(
                name: "kid_locations");
        }
    }
}
