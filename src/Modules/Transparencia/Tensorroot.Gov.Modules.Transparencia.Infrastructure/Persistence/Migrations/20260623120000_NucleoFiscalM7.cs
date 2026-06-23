using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Núcleo fiscal compartilhado do M7.0 (Saúde 15% ASPS · Educação 25% MDE): classificador setorial
    /// por função/fonte, calendário federal, parecer de conselho, projeção de execução e percentuais
    /// versionados por tenant+vigência. Tabelas no schema "transparencia".
    /// </summary>
    public partial class NucleoFiscalM7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "FontesRecursoVinculado",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Setor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComputaNoMinimo = table.Column<bool>(type: "bit", nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FontesRecursoVinculado", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CalendariosFederais",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Periodo = table.Column<int>(type: "int", nullable: true),
                    DataLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_CalendariosFederais", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PareceresConselho",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Conselho = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Setor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    DataParecer = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroResolucao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_PareceresConselho", x => x.Id));

            migrationBuilder.CreateTable(
                name: "LinhasExecucaoFiscal",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    FonteRecurso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrigemHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LinhasExecucaoFiscal", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ParametrosFiscaisVigentes",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ParametrosFiscaisVigentes", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_FontesRecursoVinculado_TenantId_Funcao_FonteRecurso_VigenciaInicio",
                schema: "transparencia",
                table: "FontesRecursoVinculado",
                columns: new[] { "TenantId", "Funcao", "FonteRecurso", "VigenciaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendariosFederais_TenantId_Exercicio_Chave",
                schema: "transparencia",
                table: "CalendariosFederais",
                columns: new[] { "TenantId", "Exercicio", "Chave" });

            migrationBuilder.CreateIndex(
                name: "IX_PareceresConselho_TenantId_Exercicio_Conselho",
                schema: "transparencia",
                table: "PareceresConselho",
                columns: new[] { "TenantId", "Exercicio", "Conselho" });

            migrationBuilder.CreateIndex(
                name: "IX_LinhasExecucaoFiscal_TenantId_OrigemHash",
                schema: "transparencia",
                table: "LinhasExecucaoFiscal",
                columns: new[] { "TenantId", "OrigemHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasExecucaoFiscal_TenantId_Exercicio",
                schema: "transparencia",
                table: "LinhasExecucaoFiscal",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosFiscaisVigentes_TenantId_Chave_VigenciaInicio",
                schema: "transparencia",
                table: "ParametrosFiscaisVigentes",
                columns: new[] { "TenantId", "Chave", "VigenciaInicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(name: "FontesRecursoVinculado", schema: "transparencia");
            migrationBuilder.DropTable(name: "CalendariosFederais", schema: "transparencia");
            migrationBuilder.DropTable(name: "PareceresConselho", schema: "transparencia");
            migrationBuilder.DropTable(name: "LinhasExecucaoFiscal", schema: "transparencia");
            migrationBuilder.DropTable(name: "ParametrosFiscaisVigentes", schema: "transparencia");
        }
    }
}
