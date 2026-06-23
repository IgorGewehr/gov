using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResumoFolhaTceRemessa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "transparencia",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "transparencia",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "transparencia",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ResumosFolhaTce",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolhaDePagamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    TipoFolha = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumosFolhaTce", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResumosFolhaTceLancamentos",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoRegistroServidor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodigoRubrica = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Operacao = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResumoFolhaTceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumosFolhaTceLancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumosFolhaTceLancamentos_ResumosFolhaTce_ResumoFolhaTceId",
                        column: x => x.ResumoFolhaTceId,
                        principalSchema: "transparencia",
                        principalTable: "ResumosFolhaTce",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumosFolhaTceRubricas",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Operacao = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    IncideIrrf = table.Column<bool>(type: "bit", nullable: false),
                    IncideRpps = table.Column<bool>(type: "bit", nullable: false),
                    IncideInss = table.Column<bool>(type: "bit", nullable: false),
                    BaseLegal = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContaPlanoFolhaTce = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ResumoFolhaTceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumosFolhaTceRubricas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumosFolhaTceRubricas_ResumosFolhaTce_ResumoFolhaTceId",
                        column: x => x.ResumoFolhaTceId,
                        principalSchema: "transparencia",
                        principalTable: "ResumosFolhaTce",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumosFolhaTceServidores",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoRegistro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    DataAdmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataDemissao = table.Column<DateOnly>(type: "date", nullable: true),
                    CodigoCargo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NomeCargo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Regime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ResumoFolhaTceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumosFolhaTceServidores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResumosFolhaTceServidores_ResumosFolhaTce_ResumoFolhaTceId",
                        column: x => x.ResumoFolhaTceId,
                        principalSchema: "transparencia",
                        principalTable: "ResumosFolhaTce",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "transparencia",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumosFolhaTce_TenantId_Exercicio_Mes",
                schema: "transparencia",
                table: "ResumosFolhaTce",
                columns: new[] { "TenantId", "Exercicio", "Mes" });

            migrationBuilder.CreateIndex(
                name: "IX_ResumosFolhaTce_TenantId_FolhaDePagamentoId",
                schema: "transparencia",
                table: "ResumosFolhaTce",
                columns: new[] { "TenantId", "FolhaDePagamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumosFolhaTceLancamentos_ResumoFolhaTceId",
                schema: "transparencia",
                table: "ResumosFolhaTceLancamentos",
                column: "ResumoFolhaTceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResumosFolhaTceRubricas_ResumoFolhaTceId",
                schema: "transparencia",
                table: "ResumosFolhaTceRubricas",
                column: "ResumoFolhaTceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResumosFolhaTceServidores_ResumoFolhaTceId",
                schema: "transparencia",
                table: "ResumosFolhaTceServidores",
                column: "ResumoFolhaTceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResumosFolhaTceLancamentos",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "ResumosFolhaTceRubricas",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "ResumosFolhaTceServidores",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "ResumosFolhaTce",
                schema: "transparencia");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "transparencia",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "transparencia",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "transparencia",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "transparencia",
                table: "AuditTrail");
        }
    }
}
