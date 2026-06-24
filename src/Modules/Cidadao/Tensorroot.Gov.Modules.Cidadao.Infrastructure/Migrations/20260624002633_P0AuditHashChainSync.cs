using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class P0AuditHashChainSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cidadao");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "cidadao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sequencia = table.Column<long>(type: "bigint", nullable: false),
                    HashAnterior = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HashAtual = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contas",
                schema: "cidadao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SenhaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SeloGovBr = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "cidadao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "cidadao",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" },
                unique: true,
                filter: "[Sequencia] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "cidadao",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Contas_TenantId_Documento",
                schema: "cidadao",
                table: "Contas",
                columns: new[] { "TenantId", "Documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_DeadLetteredOnUtc_NextAttemptUtc",
                schema: "cidadao",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "DeadLetteredOnUtc", "NextAttemptUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "cidadao");

            migrationBuilder.DropTable(
                name: "Contas",
                schema: "cidadao");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "cidadao");
        }
    }
}
