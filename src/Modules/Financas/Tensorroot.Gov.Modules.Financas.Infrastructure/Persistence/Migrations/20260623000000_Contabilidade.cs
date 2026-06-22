using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Contabilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContasContabeis",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Funcao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Funcionamento = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NaturezaInformacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NaturezaSaldo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Classe = table.Column<int>(type: "int", nullable: false),
                    ContaPaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IndicadorSuperavitFinanceiro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Encerramento = table.Column<bool>(type: "bit", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ContasContabeis", x => x.Id));

            migrationBuilder.CreateTable(
                name: "EventosContabeis",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fato = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExercicioVigenciaInicio = table.Column<int>(type: "int", nullable: false),
                    ExercicioVigenciaFim = table.Column<int>(type: "int", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_EventosContabeis", x => x.Id));

            migrationBuilder.CreateTable(
                name: "LancamentosContabeis",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "date", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    PeriodoMes = table.Column<int>(type: "int", nullable: false),
                    Historico = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NaturezaInformacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrigemReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventoContabilId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estornado = table.Column<bool>(type: "bit", nullable: false),
                    LancamentoEstornoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_LancamentosContabeis", x => x.Id));

            migrationBuilder.CreateTable(
                name: "LinhasRoteiroContabil",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventoContabilId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NaturezaInformacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodigoContaFixo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Papel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BaseValor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasRoteiroContabil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinhasRoteiroContabil_EventosContabeis_EventoContabilId",
                        column: x => x.EventoContabilId,
                        principalSchema: "financas",
                        principalTable: "EventosContabeis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartidasContabeis",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LancamentoContabilId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoConta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NaturezaInformacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Lado = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasContabeis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidasContabeis_LancamentosContabeis_LancamentoContabilId",
                        column: x => x.LancamentoContabilId,
                        principalSchema: "financas",
                        principalTable: "LancamentosContabeis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "balancete_conta",
                schema: "financas",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    PeriodoMes = table.Column<int>(type: "int", nullable: false),
                    CodigoConta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NaturezaSaldo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NaturezaInformacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    SaldoAnterior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDebitos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCreditos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoAtual = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table => table.PrimaryKey(
                    "PK_balancete_conta",
                    x => new { x.TenantId, x.ContaId, x.Exercicio, x.PeriodoMes }));

            migrationBuilder.CreateIndex(
                name: "IX_ContasContabeis_TenantId_Codigo",
                schema: "financas",
                table: "ContasContabeis",
                columns: ["TenantId", "Codigo"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosContabeis_TenantId_Fato_ExercicioVigenciaInicio",
                schema: "financas",
                table: "EventosContabeis",
                columns: ["TenantId", "Fato", "ExercicioVigenciaInicio"]);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasRoteiroContabil_EventoContabilId",
                schema: "financas",
                table: "LinhasRoteiroContabil",
                column: "EventoContabilId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosContabeis_TenantId_Exercicio_PeriodoMes",
                schema: "financas",
                table: "LancamentosContabeis",
                columns: ["TenantId", "Exercicio", "PeriodoMes"]);

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosContabeis_TenantId_OrigemReferenciaId_EventoContabilId",
                schema: "financas",
                table: "LancamentosContabeis",
                columns: ["TenantId", "OrigemReferenciaId", "EventoContabilId"]);

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContabeis_ContaId",
                schema: "financas",
                table: "PartidasContabeis",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasContabeis_LancamentoContabilId",
                schema: "financas",
                table: "PartidasContabeis",
                column: "LancamentoContabilId");

            migrationBuilder.CreateIndex(
                name: "IX_balancete_conta_TenantId_Exercicio_PeriodoMes",
                schema: "financas",
                table: "balancete_conta",
                columns: ["TenantId", "Exercicio", "PeriodoMes"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ContasContabeis", schema: "financas");
            migrationBuilder.DropTable(name: "LinhasRoteiroContabil", schema: "financas");
            migrationBuilder.DropTable(name: "PartidasContabeis", schema: "financas");
            migrationBuilder.DropTable(name: "balancete_conta", schema: "financas");
            migrationBuilder.DropTable(name: "EventosContabeis", schema: "financas");
            migrationBuilder.DropTable(name: "LancamentosContabeis", schema: "financas");
        }
    }
}
