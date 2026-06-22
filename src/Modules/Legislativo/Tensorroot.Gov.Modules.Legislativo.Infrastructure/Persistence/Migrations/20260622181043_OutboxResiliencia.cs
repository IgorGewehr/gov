using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxResiliencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "legislativo",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "legislativo",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredOnUtc",
                schema: "legislativo",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptUtc",
                schema: "legislativo",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptUtc",
                schema: "legislativo",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptUtc",
                schema: "legislativo",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "legislativo",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredOnUtc",
                schema: "legislativo",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptUtc",
                schema: "legislativo",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "legislativo",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");
        }
    }
}
