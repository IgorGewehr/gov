using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Tema 1.113/STJ: a base do ITBI passa a ser o VALOR DECLARADO (presunção de veracidade). Remove o
    /// booleano <c>BaseFoiValorVenal</c> de <c>TransmissoesImobiliarias</c> e adiciona a ORIGEM da base
    /// (Declarada/ArbitradaArt148) + vínculo ao processo. Cria a tabela <c>ProcessosArbitramentoItbi</c>
    /// (arbitramento CTN art. 148, máquina de estados auditada).
    /// </summary>
    /// <inheritdoc />
    public partial class ItbiTema1113Arbitramento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseFoiValorVenal",
                schema: "tributos",
                table: "TransmissoesImobiliarias");

            migrationBuilder.AddColumn<string>(
                name: "Origem",
                schema: "tributos",
                table: "TransmissoesImobiliarias",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Declarada");

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessoArbitramentoId",
                schema: "tributos",
                table: "TransmissoesImobiliarias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessosArbitramentoItbi",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransmissaoImobiliariaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroProcesso = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    MotivoInstauracao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValorPropostoFisco = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FundamentacaoFisco = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    JustificativaContribuinte = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ValorArbitradoFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInstauracao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataAberturaContraditorio = table.Column<DateOnly>(type: "date", nullable: true),
                    DataApresentacaoContraditorio = table.Column<DateOnly>(type: "date", nullable: true),
                    DataDesfecho = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessosArbitramentoItbi", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessosArbitramentoItbi_TenantId_NumeroProcesso",
                schema: "tributos",
                table: "ProcessosArbitramentoItbi",
                columns: new[] { "TenantId", "NumeroProcesso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessosArbitramentoItbi_TenantId_TransmissaoImobiliariaId",
                schema: "tributos",
                table: "ProcessosArbitramentoItbi",
                columns: new[] { "TenantId", "TransmissaoImobiliariaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessosArbitramentoItbi",
                schema: "tributos");

            migrationBuilder.DropColumn(
                name: "Origem",
                schema: "tributos",
                table: "TransmissoesImobiliarias");

            migrationBuilder.DropColumn(
                name: "ProcessoArbitramentoId",
                schema: "tributos",
                table: "TransmissoesImobiliarias");

            migrationBuilder.AddColumn<bool>(
                name: "BaseFoiValorVenal",
                schema: "tributos",
                table: "TransmissoesImobiliarias",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
