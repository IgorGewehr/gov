using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NucleoFiscalAssistenciaM7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "assistenciasocial",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "assistenciasocial",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "assistenciasocial",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CriteriosBeneficioEventualMunicipal",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    MultiploRendaSalarioMinimo = table.Column<decimal>(type: "decimal(18,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriosBeneficioEventualMunicipal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundosMunicipaisAssistencia",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundosMunicipaisAssistencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosMensaisAtendimento",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeAtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoUnidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Competencia = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FechadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosMensaisAtendimento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContasCofinanciamentoSuas",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FundoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bloco = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Piso = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalRecebido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExecutado = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasCofinanciamentoSuas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasCofinanciamentoSuas_FundosMunicipaisAssistencia_FundoId",
                        column: x => x.FundoId,
                        principalSchema: "assistenciasocial",
                        principalTable: "FundosMunicipaisAssistencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinhasRmaServico",
                schema: "assistenciasocial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Servico = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasRmaServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinhasRmaServico_RegistrosMensaisAtendimento_RmaId",
                        column: x => x.RmaId,
                        principalSchema: "assistenciasocial",
                        principalTable: "RegistrosMensaisAtendimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "assistenciasocial",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_ContasCofinanciamentoSuas_FundoId_Bloco_Piso_FonteRecurso",
                schema: "assistenciasocial",
                table: "ContasCofinanciamentoSuas",
                columns: new[] { "FundoId", "Bloco", "Piso", "FonteRecurso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CriteriosBeneficioEventualMunicipal_TenantId_Modalidade_VigenciaInicio",
                schema: "assistenciasocial",
                table: "CriteriosBeneficioEventualMunicipal",
                columns: new[] { "TenantId", "Modalidade", "VigenciaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundosMunicipaisAssistencia_TenantId",
                schema: "assistenciasocial",
                table: "FundosMunicipaisAssistencia",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasRmaServico_RmaId_Servico",
                schema: "assistenciasocial",
                table: "LinhasRmaServico",
                columns: new[] { "RmaId", "Servico" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RMA_Tenant_Unidade_Competencia",
                schema: "assistenciasocial",
                table: "RegistrosMensaisAtendimento",
                columns: new[] { "TenantId", "UnidadeAtendimentoId", "Competencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasCofinanciamentoSuas",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "CriteriosBeneficioEventualMunicipal",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "LinhasRmaServico",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "FundosMunicipaisAssistencia",
                schema: "assistenciasocial");

            migrationBuilder.DropTable(
                name: "RegistrosMensaisAtendimento",
                schema: "assistenciasocial");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "assistenciasocial",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "assistenciasocial",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "assistenciasocial",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "assistenciasocial",
                table: "AuditTrail");
        }
    }
}
