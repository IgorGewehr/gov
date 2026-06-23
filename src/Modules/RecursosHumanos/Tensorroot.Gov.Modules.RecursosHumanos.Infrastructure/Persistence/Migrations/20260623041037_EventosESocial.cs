using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventosESocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "recursoshumanos",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "recursoshumanos",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "recursoshumanos",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "EventosESocial",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChaveTipoEvento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChaveIdNegocio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChaveCompetencia = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    IdEvento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ambiente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Xml = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    HashXmlGerado = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XmlAssinado = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ThumbprintCertificado = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ProtocoloLote = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NumeroRecibo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CodigoErro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DescricaoErro = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GeradoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssinadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TransmitidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetornoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosESocial", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "recursoshumanos",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosESocial_ChaveTipoEvento_ChaveIdNegocio_ChaveCompetencia",
                schema: "recursoshumanos",
                table: "EventosESocial",
                columns: new[] { "ChaveTipoEvento", "ChaveIdNegocio", "ChaveCompetencia" },
                unique: true,
                filter: "[ChaveCompetencia] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosESocial_TenantId_Estado",
                schema: "recursoshumanos",
                table: "EventosESocial",
                columns: new[] { "TenantId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosESocial",
                schema: "recursoshumanos");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "recursoshumanos",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "recursoshumanos",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "recursoshumanos",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "recursoshumanos",
                table: "AuditTrail");
        }
    }
}
