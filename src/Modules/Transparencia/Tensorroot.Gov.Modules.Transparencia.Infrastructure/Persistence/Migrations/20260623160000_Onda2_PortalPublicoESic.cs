using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Onda 2 — Portal Publico + e-SIC + Dados Abertos. Cria: configuracao do portal publico (slug do
    /// ente), read models publicos de transparencia ativa (despesa/receita/contrato/folha nominal,
    /// materializados de Integration Events ja publicados — I-13) e o agregado e-SIC
    /// (PedidoInformacaoSic, LAI 12.527/2011, com VOs owned Protocolo/Solicitante/Resposta/Recurso).
    /// Tabelas no schema "transparencia".
    /// </summary>
    public partial class Onda2_PortalPublicoESic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "PortalPublicoConfig",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeEnte = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PortalPublicoConfig", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PublicacaoDespesa",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemEventoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Fase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroEmpenho = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CredorNomeOuRazao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CredorDocMascarado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FuncaoSubfuncao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PublicacaoDespesa", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PublicacaoReceita",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemEventoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    RubricaReceita = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PublicacaoReceita", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PublicacaoContrato",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemEventoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    NumeroContrato = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Fornecedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Objeto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Modalidade = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    NumeroContratoPncp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_PublicacaoContrato", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PublicacaoFolhaNominal",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrigemEventoId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Competencia = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    ServidorNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CargoDescricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Lotacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RemuneracaoBruta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descontos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Liquido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PublicacaoFolhaNominal", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PedidoInformacaoSic",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FormaResposta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataAbertura = table.Column<DateOnly>(type: "date", nullable: false),
                    PrazoResposta = table.Column<DateOnly>(type: "date", nullable: false),
                    ProrrogadoAte = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoProrrogacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FundamentoIndeferimento = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProtocoloAno = table.Column<int>(type: "int", nullable: false),
                    ProtocoloSequencial = table.Column<int>(type: "int", nullable: false),
                    Protocolo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SolicitanteNome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SolicitanteDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SolicitanteContato = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    SolicitanteAnonimo = table.Column<bool>(type: "bit", nullable: false),
                    RespostaTexto = table.Column<string>(type: "nvarchar(8000)", maxLength: 8000, nullable: true),
                    RespostaAnexo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RespostaData = table.Column<DateOnly>(type: "date", nullable: true),
                    RecursoInstancia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RecursoFundamento = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RecursoDataInterposicao = table.Column<DateOnly>(type: "date", nullable: true),
                    RecursoResultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RecursoDecisao = table.Column<string>(type: "nvarchar(8000)", maxLength: 8000, nullable: true),
                    RecursoDataDecisao = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_PedidoInformacaoSic", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_PortalPublicoConfig_TenantId_Slug",
                schema: "transparencia",
                table: "PortalPublicoConfig",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoDespesa_TenantId_OrigemEventoId",
                schema: "transparencia",
                table: "PublicacaoDespesa",
                columns: new[] { "TenantId", "OrigemEventoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoDespesa_TenantId_Exercicio_Fase",
                schema: "transparencia",
                table: "PublicacaoDespesa",
                columns: new[] { "TenantId", "Exercicio", "Fase" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoReceita_TenantId_OrigemEventoId",
                schema: "transparencia",
                table: "PublicacaoReceita",
                columns: new[] { "TenantId", "OrigemEventoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoReceita_TenantId_Exercicio",
                schema: "transparencia",
                table: "PublicacaoReceita",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoContrato_TenantId_OrigemEventoId",
                schema: "transparencia",
                table: "PublicacaoContrato",
                columns: new[] { "TenantId", "OrigemEventoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoContrato_TenantId_Exercicio",
                schema: "transparencia",
                table: "PublicacaoContrato",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoFolhaNominal_TenantId_OrigemEventoId",
                schema: "transparencia",
                table: "PublicacaoFolhaNominal",
                columns: new[] { "TenantId", "OrigemEventoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacaoFolhaNominal_TenantId_Competencia",
                schema: "transparencia",
                table: "PublicacaoFolhaNominal",
                columns: new[] { "TenantId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInformacaoSic_TenantId_Situacao",
                schema: "transparencia",
                table: "PedidoInformacaoSic",
                columns: new[] { "TenantId", "Situacao" });

            // Protocolo unico por banco DEDICADO do tenant (database-per-tenant ⇒ unicidade ja e por tenant).
            migrationBuilder.CreateIndex(
                name: "IX_PedidoInformacaoSic_Protocolo",
                schema: "transparencia",
                table: "PedidoInformacaoSic",
                column: "Protocolo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(name: "PortalPublicoConfig", schema: "transparencia");
            migrationBuilder.DropTable(name: "PublicacaoDespesa", schema: "transparencia");
            migrationBuilder.DropTable(name: "PublicacaoReceita", schema: "transparencia");
            migrationBuilder.DropTable(name: "PublicacaoContrato", schema: "transparencia");
            migrationBuilder.DropTable(name: "PublicacaoFolhaNominal", schema: "transparencia");
            migrationBuilder.DropTable(name: "PedidoInformacaoSic", schema: "transparencia");
        }
    }
}
