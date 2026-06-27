using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SstSaudeOcupacionalEBancoDeHoras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BancosDeHoras",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaldoMinutos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BancosDeHoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SstComunicacoesAcidente",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoCat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoAcidente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataHoraAcidente = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HouveObito = table.Column<bool>(type: "bit", nullable: false),
                    DataObito = table.Column<DateOnly>(type: "date", nullable: true),
                    DescricaoSituacao = table.Column<string>(type: "nvarchar(999)", maxLength: 999, nullable: false),
                    Cid = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ParteCorpoAtingida = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AgenteCausador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CatOrigem = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SstComunicacoesAcidente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SstExamesOcupacionais",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataExame = table.Column<DateOnly>(type: "date", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MedicoNome = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    MedicoNrConselho = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    MedicoUfConselho = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    DataProximoExame = table.Column<DateOnly>(type: "date", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExamesComplementares = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SstExamesOcupacionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SstExposicoesAgenteNocivo",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServidorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InicioExposicao = table.Column<DateOnly>(type: "date", nullable: false),
                    FimExposicao = table.Column<DateOnly>(type: "date", nullable: true),
                    SetorAtividade = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SstExposicoesAgenteNocivo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BancosDeHorasLancamentos",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Minutos = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BancoDeHorasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BancosDeHorasLancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BancosDeHorasLancamentos_BancosDeHoras_BancoDeHorasId",
                        column: x => x.BancoDeHorasId,
                        principalSchema: "recursoshumanos",
                        principalTable: "BancosDeHoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SstExposicaoAgentes",
                schema: "recursoshumanos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(999)", maxLength: 999, nullable: false),
                    Intensidade = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UtilizaEpc = table.Column<bool>(type: "bit", nullable: false),
                    UtilizaEpi = table.Column<bool>(type: "bit", nullable: false),
                    ExposicaoAgenteNocivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SstExposicaoAgentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SstExposicaoAgentes_SstExposicoesAgenteNocivo_ExposicaoAgenteNocivoId",
                        column: x => x.ExposicaoAgenteNocivoId,
                        principalSchema: "recursoshumanos",
                        principalTable: "SstExposicoesAgenteNocivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BancosDeHoras_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "BancosDeHoras",
                columns: new[] { "TenantId", "ServidorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BancosDeHorasLancamentos_BancoDeHorasId_Referencia",
                schema: "recursoshumanos",
                table: "BancosDeHorasLancamentos",
                columns: new[] { "BancoDeHorasId", "Referencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SstComunicacoesAcidente_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "SstComunicacoesAcidente",
                columns: new[] { "TenantId", "ServidorId" });

            migrationBuilder.CreateIndex(
                name: "IX_SstExamesOcupacionais_TenantId_DataProximoExame",
                schema: "recursoshumanos",
                table: "SstExamesOcupacionais",
                columns: new[] { "TenantId", "DataProximoExame" });

            migrationBuilder.CreateIndex(
                name: "IX_SstExamesOcupacionais_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "SstExamesOcupacionais",
                columns: new[] { "TenantId", "ServidorId" });

            migrationBuilder.CreateIndex(
                name: "IX_SstExposicaoAgentes_ExposicaoAgenteNocivoId",
                schema: "recursoshumanos",
                table: "SstExposicaoAgentes",
                column: "ExposicaoAgenteNocivoId");

            migrationBuilder.CreateIndex(
                name: "IX_SstExposicoesAgenteNocivo_TenantId_ServidorId",
                schema: "recursoshumanos",
                table: "SstExposicoesAgenteNocivo",
                columns: new[] { "TenantId", "ServidorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BancosDeHorasLancamentos",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "SstComunicacoesAcidente",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "SstExamesOcupacionais",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "SstExposicaoAgentes",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "BancosDeHoras",
                schema: "recursoshumanos");

            migrationBuilder.DropTable(
                name: "SstExposicoesAgenteNocivo",
                schema: "recursoshumanos");
        }
    }
}
