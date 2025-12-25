using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HbcRest.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nyc_open_data_311_call_center_inquiry",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    hbc_unique_key = table.Column<string>(type: "TEXT", nullable: true),
                    unique_id = table.Column<string>(type: "TEXT", nullable: true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    time = table.Column<string>(type: "TEXT", nullable: true),
                    date_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    agency = table.Column<string>(type: "TEXT", nullable: true),
                    agency_name = table.Column<string>(type: "TEXT", nullable: true),
                    inquiry_name = table.Column<string>(type: "TEXT", nullable: true),
                    brief_description = table.Column<string>(type: "TEXT", nullable: true),
                    call_resolution = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nyc_open_data_311_call_center_inquiry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nyc_open_data_311_customer_satisfaction_survey",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    hbc_unique_key = table.Column<string>(type: "TEXT", nullable: true),
                    year = table.Column<string>(type: "TEXT", nullable: true),
                    campaign = table.Column<string>(type: "TEXT", nullable: true),
                    channel = table.Column<string>(type: "TEXT", nullable: true),
                    survey_type = table.Column<string>(type: "TEXT", nullable: true),
                    start_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    completion_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    survey_language = table.Column<string>(type: "TEXT", nullable: true),
                    overall_satisfaction = table.Column<string>(type: "TEXT", nullable: true),
                    wait_time = table.Column<string>(type: "TEXT", nullable: true),
                    agent_customer_service = table.Column<string>(type: "TEXT", nullable: true),
                    agent_job_knowledge = table.Column<string>(type: "TEXT", nullable: true),
                    answer_satisfaction = table.Column<string>(type: "TEXT", nullable: true),
                    nps = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nyc_open_data_311_customer_satisfaction_survey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nyc_open_data_311_service_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    hbc_unique_key = table.Column<string>(type: "TEXT", nullable: true),
                    address_type = table.Column<string>(type: "TEXT", nullable: true),
                    agency = table.Column<string>(type: "TEXT", nullable: true),
                    agency_name = table.Column<string>(type: "TEXT", nullable: true),
                    borough = table.Column<string>(type: "TEXT", nullable: true),
                    bridge_highway_direction = table.Column<string>(type: "TEXT", nullable: true),
                    bridge_highway_name = table.Column<string>(type: "TEXT", nullable: true),
                    bridge_highway_segment = table.Column<string>(type: "TEXT", nullable: true),
                    city = table.Column<string>(type: "TEXT", nullable: true),
                    closed_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    community_board = table.Column<string>(type: "TEXT", nullable: true),
                    complaint_type = table.Column<string>(type: "TEXT", nullable: true),
                    created_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    cross_street_1 = table.Column<string>(type: "TEXT", nullable: true),
                    cross_street_2 = table.Column<string>(type: "TEXT", nullable: true),
                    descriptor = table.Column<string>(type: "TEXT", nullable: true),
                    due_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    facility_type = table.Column<string>(type: "TEXT", nullable: true),
                    ferry_direction = table.Column<string>(type: "TEXT", nullable: true),
                    ferry_terminal_name = table.Column<string>(type: "TEXT", nullable: true),
                    garage_lot_name = table.Column<string>(type: "TEXT", nullable: true),
                    incident_address = table.Column<string>(type: "TEXT", nullable: true),
                    incident_zip = table.Column<string>(type: "TEXT", nullable: true),
                    intersection_street_1 = table.Column<string>(type: "TEXT", nullable: true),
                    intersection_street_2 = table.Column<string>(type: "TEXT", nullable: true),
                    landmark = table.Column<string>(type: "TEXT", nullable: true),
                    latitude = table.Column<double>(type: "REAL", nullable: true),
                    location = table.Column<string>(type: "TEXT", nullable: true),
                    location_type = table.Column<string>(type: "TEXT", nullable: true),
                    longitude = table.Column<double>(type: "REAL", nullable: true),
                    park_borough = table.Column<string>(type: "TEXT", nullable: true),
                    park_facility_name = table.Column<string>(type: "TEXT", nullable: true),
                    resolution_action_updated_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    road_ramp = table.Column<string>(type: "TEXT", nullable: true),
                    school_address = table.Column<string>(type: "TEXT", nullable: true),
                    school_city = table.Column<string>(type: "TEXT", nullable: true),
                    school_code = table.Column<string>(type: "TEXT", nullable: true),
                    school_name = table.Column<string>(type: "TEXT", nullable: true),
                    school_not_found = table.Column<string>(type: "TEXT", nullable: true),
                    school_number = table.Column<string>(type: "TEXT", nullable: true),
                    school_or_citywide_complaint = table.Column<string>(type: "TEXT", nullable: true),
                    school_phone_number = table.Column<string>(type: "TEXT", nullable: true),
                    school_region = table.Column<string>(type: "TEXT", nullable: true),
                    school_state = table.Column<string>(type: "TEXT", nullable: true),
                    school_zip = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: true),
                    street_name = table.Column<string>(type: "TEXT", nullable: true),
                    taxi_company_borough = table.Column<string>(type: "TEXT", nullable: true),
                    taxi_pick_up_location = table.Column<string>(type: "TEXT", nullable: true),
                    unique_key = table.Column<string>(type: "TEXT", nullable: true),
                    vehicle_type = table.Column<string>(type: "TEXT", nullable: true),
                    x_coordinate_state_plane_ = table.Column<double>(type: "REAL", nullable: true),
                    y_coordinate_state_plane_ = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nyc_open_data_311_service_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nyc_open_data_311_call_center_inquiry_hbc_unique_key",
                table: "nyc_open_data_311_call_center_inquiry",
                column: "hbc_unique_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nyc_open_data_311_customer_satisfaction_survey_hbc_unique_key",
                table: "nyc_open_data_311_customer_satisfaction_survey",
                column: "hbc_unique_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nyc_open_data_311_service_requests_hbc_unique_key",
                table: "nyc_open_data_311_service_requests",
                column: "hbc_unique_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nyc_open_data_311_call_center_inquiry");

            migrationBuilder.DropTable(
                name: "nyc_open_data_311_customer_satisfaction_survey");

            migrationBuilder.DropTable(
                name: "nyc_open_data_311_service_requests");
        }
    }
}
