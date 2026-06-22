using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CadastroImobiliarioIptu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImovelId",
                schema: "tributos",
                table: "Lancamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Dams",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LancamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContribuinteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Imoveis",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProprietarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InscricaoMunicipal = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CibCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MatriculaRgi = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    SetorQuadraLote = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FaceQuadra = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ZonaFiscal = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AreaTerreno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AreaConstruida = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoUso = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PadraoConstrutivo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AnoConstrucao = table.Column<int>(type: "int", nullable: true),
                    FracaoIdeal = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imoveis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantasValores",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantasValores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelasAliquotaIptu",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Edificado = table.Column<bool>(type: "bit", nullable: false),
                    FundamentoLegal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasAliquotaIptu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parcelas",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Paga = table.Column<bool>(type: "bit", nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parcelas_Dams_DamId",
                        column: x => x.DamId,
                        principalSchema: "tributos",
                        principalTable: "Dams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PgvFatores",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantaValoresId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Multiplicador = table.Column<decimal>(type: "decimal(9,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PgvFatores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PgvFatores_PlantasValores_PlantaValoresId",
                        column: x => x.PlantaValoresId,
                        principalSchema: "tributos",
                        principalTable: "PlantasValores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PgvValoresZona",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantaValoresId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZonaFiscal = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ValorM2Terreno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorM2Construcao = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PgvValoresZona", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PgvValoresZona_PlantasValores_PlantaValoresId",
                        column: x => x.PlantaValoresId,
                        principalSchema: "tributos",
                        principalTable: "PlantasValores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaixasAliquotaIptu",
                schema: "tributos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TabelaAliquotaIptuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValorVenalMinimo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorVenalMaximo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AliquotaPercentual = table.Column<decimal>(type: "decimal(9,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaixasAliquotaIptu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaixasAliquotaIptu_TabelasAliquotaIptu_TabelaAliquotaIptuId",
                        column: x => x.TabelaAliquotaIptuId,
                        principalSchema: "tributos",
                        principalTable: "TabelasAliquotaIptu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_ImovelId",
                schema: "tributos",
                table: "Lancamentos",
                column: "ImovelId");

            migrationBuilder.CreateIndex(
                name: "IX_Dams_TenantId_LancamentoId",
                schema: "tributos",
                table: "Dams",
                columns: new[] { "TenantId", "LancamentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_FaixasAliquotaIptu_TabelaAliquotaIptuId",
                schema: "tributos",
                table: "FaixasAliquotaIptu",
                column: "TabelaAliquotaIptuId");

            migrationBuilder.CreateIndex(
                name: "IX_Imoveis_InscricaoMunicipal",
                schema: "tributos",
                table: "Imoveis",
                column: "InscricaoMunicipal");

            migrationBuilder.CreateIndex(
                name: "IX_Imoveis_TenantId_ProprietarioId",
                schema: "tributos",
                table: "Imoveis",
                columns: new[] { "TenantId", "ProprietarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcelas_DamId",
                schema: "tributos",
                table: "Parcelas",
                column: "DamId");

            migrationBuilder.CreateIndex(
                name: "IX_PgvFatores_PlantaValoresId_Tipo_Chave",
                schema: "tributos",
                table: "PgvFatores",
                columns: new[] { "PlantaValoresId", "Tipo", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PgvValoresZona_PlantaValoresId_ZonaFiscal",
                schema: "tributos",
                table: "PgvValoresZona",
                columns: new[] { "PlantaValoresId", "ZonaFiscal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantasValores_TenantId_Exercicio",
                schema: "tributos",
                table: "PlantasValores",
                columns: new[] { "TenantId", "Exercicio" });

            migrationBuilder.CreateIndex(
                name: "IX_TabelasAliquotaIptu_TenantId_Exercicio_Edificado",
                schema: "tributos",
                table: "TabelasAliquotaIptu",
                columns: new[] { "TenantId", "Exercicio", "Edificado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaixasAliquotaIptu",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "Imoveis",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "Parcelas",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "PgvFatores",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "PgvValoresZona",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "TabelasAliquotaIptu",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "Dams",
                schema: "tributos");

            migrationBuilder.DropTable(
                name: "PlantasValores",
                schema: "tributos");

            migrationBuilder.DropIndex(
                name: "IX_Lancamentos_ImovelId",
                schema: "tributos",
                table: "Lancamentos");

            migrationBuilder.DropColumn(
                name: "ImovelId",
                schema: "tributos",
                table: "Lancamentos");
        }
    }
}
