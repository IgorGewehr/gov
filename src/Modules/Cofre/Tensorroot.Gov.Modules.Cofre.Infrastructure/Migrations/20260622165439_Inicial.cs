using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cofre");

            migrationBuilder.CreateTable(
                name: "AssinaturaAuditLog",
                schema: "cofre",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Titular = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HashArtefatoSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sucesso = table.Column<bool>(type: "bit", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturaAuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "cofre",
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
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificadosA1",
                schema: "cofre",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titular = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CnpjTitular = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NotBeforeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotAfterUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Serie = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PfxCipher = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PfxNonce = table.Column<byte[]>(type: "varbinary(12)", maxLength: 12, nullable: false),
                    PfxTag = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: false),
                    SenhaCipher = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SenhaNonce = table.Column<byte[]>(type: "varbinary(12)", maxLength: 12, nullable: false),
                    SenhaTag = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: false),
                    DekWrapped = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    KekKeyId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CertificadoAnteriorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificadosA1", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "cofre",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturaAuditLog_TenantId_TimestampUtc",
                schema: "cofre",
                table: "AssinaturaAuditLog",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "cofre",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosA1_TenantId_Status",
                schema: "cofre",
                table: "CertificadosA1",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosA1_TenantId_Thumbprint",
                schema: "cofre",
                table: "CertificadosA1",
                columns: new[] { "TenantId", "Thumbprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "cofre",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssinaturaAuditLog",
                schema: "cofre");

            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "cofre");

            migrationBuilder.DropTable(
                name: "CertificadosA1",
                schema: "cofre");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "cofre");
        }
    }
}
