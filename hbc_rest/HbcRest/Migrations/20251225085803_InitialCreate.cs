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

            migrationBuilder.CreateIndex(
                name: "IX_nyc_open_data_311_customer_satisfaction_survey_hbc_unique_key",
                table: "nyc_open_data_311_customer_satisfaction_survey",
                column: "hbc_unique_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nyc_open_data_311_customer_satisfaction_survey");
        }
    }
}
