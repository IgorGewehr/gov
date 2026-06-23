using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NucleoFiscalEducacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "educacao",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "educacao",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "educacao",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "DistribuicoesFundeb",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistribuicoesFundeb", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LinhasExecucaoEducacao",
                schema: "educacao",
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
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasExecucaoEducacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosFiscaisEducacao",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VigenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosFiscaisEducacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegrasClassificacaoMde",
                schema: "educacao",
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
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegrasClassificacaoMde", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemuneracoesMagisterio",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    RemuneracaoProfissionais = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemuneracoesMagisterio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContasOrigemFundeb",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistribuicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValorEsperado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRecebido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasOrigemFundeb", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasOrigemFundeb_DistribuicoesFundeb_DistribuicaoId",
                        column: x => x.DistribuicaoId,
                        principalSchema: "educacao",
                        principalTable: "DistribuicoesFundeb",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "educacao",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_ContasOrigemFundeb_DistribuicaoId_Origem",
                schema: "educacao",
                table: "ContasOrigemFundeb",
                columns: new[] { "DistribuicaoId", "Origem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistribuicoesFundeb_TenantId_Exercicio",
                schema: "educacao",
                table: "DistribuicoesFundeb",
                columns: new[] { "TenantId", "Exercicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasExecucaoEducacao_TenantId_Exercicio",
                schema: "educacao",
                table: "LinhasExecucaoEducacao",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_LinhasExecucaoEducacao_TenantId_OrigemHash",
                schema: "educacao",
                table: "LinhasExecucaoEducacao",
                columns: new[] { "TenantId", "OrigemHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosFiscaisEducacao_TenantId_Chave_VigenciaInicio",
                schema: "educacao",
                table: "ParametrosFiscaisEducacao",
                columns: new[] { "TenantId", "Chave", "VigenciaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegrasClassificacaoMde_TenantId_Funcao_Subfuncao_FonteRecurso_VigenciaInicio",
                schema: "educacao",
                table: "RegrasClassificacaoMde",
                columns: new[] { "TenantId", "Funcao", "Subfuncao", "FonteRecurso", "VigenciaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_RemuneracoesMagisterio_TenantId_Exercicio",
                schema: "educacao",
                table: "RemuneracoesMagisterio",
                columns: new[] { "TenantId", "Exercicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasOrigemFundeb",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "LinhasExecucaoEducacao",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "ParametrosFiscaisEducacao",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "RegrasClassificacaoMde",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "RemuneracoesMagisterio",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DistribuicoesFundeb",
                schema: "educacao");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "educacao",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "educacao",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "educacao",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "educacao",
                table: "AuditTrail");
        }
    }
}
