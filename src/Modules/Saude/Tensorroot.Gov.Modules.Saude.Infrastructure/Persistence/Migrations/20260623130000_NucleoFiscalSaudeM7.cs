using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Núcleo fiscal de Saúde do M7 (S-1/S-2): classificação ASPS versionada (LC 141/2012 arts. 3º/4º),
    /// Fundo Municipal de Saúde por bloco de financiamento (Custeio/Investimento, Port. 3.992/2017) com
    /// execução segregada, projeção de execução fiscal e percentuais versionados por tenant+vigência.
    /// Tabelas no schema "saude".
    /// </summary>
    public partial class NucleoFiscalSaudeM7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "RegrasClassificacaoAsps",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Subfuncao = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Efeito = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_RegrasClassificacaoAsps", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FundosMunicipaisSaude",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FundosMunicipaisSaude", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ContasBlocoFinanciamentoSaude",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FundoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bloco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalRecebido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExecutado = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasBlocoFinanciamentoSaude", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasBlocoFinanciamentoSaude_FundosMunicipaisSaude_FundoId",
                        column: x => x.FundoId,
                        principalSchema: "saude",
                        principalTable: "FundosMunicipaisSaude",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinhasExecucaoSaude",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Subfuncao = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrigemHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LinhasExecucaoSaude", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ParametrosFiscaisSaude",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ParametrosFiscaisSaude", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_RegrasClassificacaoAsps_TenantId_Funcao_Subfuncao_FonteRecurso_VigenciaInicio",
                schema: "saude",
                table: "RegrasClassificacaoAsps",
                columns: new[] { "TenantId", "Funcao", "Subfuncao", "FonteRecurso", "VigenciaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_FundosMunicipaisSaude_TenantId",
                schema: "saude",
                table: "FundosMunicipaisSaude",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBlocoFinanciamentoSaude_FundoId_Bloco_FonteRecurso",
                schema: "saude",
                table: "ContasBlocoFinanciamentoSaude",
                columns: new[] { "FundoId", "Bloco", "FonteRecurso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasExecucaoSaude_TenantId_OrigemHash",
                schema: "saude",
                table: "LinhasExecucaoSaude",
                columns: new[] { "TenantId", "OrigemHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasExecucaoSaude_TenantId_Exercicio",
                schema: "saude",
                table: "LinhasExecucaoSaude",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosFiscaisSaude_TenantId_Chave_VigenciaInicio",
                schema: "saude",
                table: "ParametrosFiscaisSaude",
                columns: new[] { "TenantId", "Chave", "VigenciaInicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(name: "ContasBlocoFinanciamentoSaude", schema: "saude");
            migrationBuilder.DropTable(name: "RegrasClassificacaoAsps", schema: "saude");
            migrationBuilder.DropTable(name: "LinhasExecucaoSaude", schema: "saude");
            migrationBuilder.DropTable(name: "ParametrosFiscaisSaude", schema: "saude");
            migrationBuilder.DropTable(name: "FundosMunicipaisSaude", schema: "saude");
        }
    }
}
