using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "patrimonio");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bens",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroTombamento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValorInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorResidual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorContabil = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTerreno = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VidaUtilMeses = table.Column<int>(type: "int", nullable: false),
                    DataIncorporacao = table.Column<DateOnly>(type: "date", nullable: false),
                    EmCondicoesDeUso = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensEstoque",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PontoPedido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MetodoCusteio = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CustoMedio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorRealizavelLiquido = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ClassificacaoAbc = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEstoque", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumeroTombamento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ValorInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorResidual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorContabil = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VidaUtilMeses = table.Column<int>(type: "int", nullable: false),
                    DataIncorporacao = table.Column<DateOnly>(type: "date", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmCondicoesDeUso = table.Column<bool>(type: "bit", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Placa = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Renavam = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Odometro = table.Column<int>(type: "int", nullable: false),
                    Horimetro = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MotoristaAtualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BensHistoricosDepreciacao",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorDepreciado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorContabilResultante = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BensHistoricosDepreciacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BensHistoricosDepreciacao_Bens_BemPatrimonialId",
                        column: x => x.BemPatrimonialId,
                        principalSchema: "patrimonio",
                        principalTable: "Bens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BensImpairments",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorRecuperavel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PerdaReconhecida = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaudoUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BensImpairments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BensImpairments_Bens_BemPatrimonialId",
                        column: x => x.BemPatrimonialId,
                        principalSchema: "patrimonio",
                        principalTable: "Bens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BensMovimentacoes",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalizacaoOrigem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocalizacaoDestino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BensMovimentacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BensMovimentacoes_Bens_BemPatrimonialId",
                        column: x => x.BemPatrimonialId,
                        principalSchema: "patrimonio",
                        principalTable: "Bens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BensReavaliacoes",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    NovoValorJusto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LaudoUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BemPatrimonialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BensReavaliacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BensReavaliacoes_Bens_BemPatrimonialId",
                        column: x => x.BemPatrimonialId,
                        principalSchema: "patrimonio",
                        principalTable: "Bens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensEstoqueLotes",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantidadeEntrada = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantidadeRestante = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataEntrada = table.Column<DateOnly>(type: "date", nullable: false),
                    Validade = table.Column<DateOnly>(type: "date", nullable: true),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEstoqueLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensEstoqueLotes_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalSchema: "patrimonio",
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensEstoqueMovimentos",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEstoqueMovimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensEstoqueMovimentos_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalSchema: "patrimonio",
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensEstoqueRequisicoes",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolicitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensEstoqueRequisicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensEstoqueRequisicoes_ItensEstoque_ItemEstoqueId",
                        column: x => x.ItemEstoqueId,
                        principalSchema: "patrimonio",
                        principalTable: "ItensEstoque",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeiculosAbastecimentos",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Litros = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Odometro = table.Column<int>(type: "int", nullable: false),
                    Horimetro = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculosAbastecimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculosAbastecimentos_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalSchema: "patrimonio",
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeiculosLicenciamentos",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    ValorIpva = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTaxa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculosLicenciamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculosLicenciamentos_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalSchema: "patrimonio",
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeiculosMotoristas",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cnh = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CategoriaCnh = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ValidadeCnh = table.Column<DateOnly>(type: "date", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculosMotoristas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculosMotoristas_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalSchema: "patrimonio",
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeiculosMultas",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoInfracaoCtb = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataInfracao = table.Column<DateOnly>(type: "date", nullable: false),
                    MotoristaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculosMultas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculosMultas_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalSchema: "patrimonio",
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeiculosOrdensServico",
                schema: "patrimonio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CustoEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustoRealizado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Odometro = table.Column<int>(type: "int", nullable: false),
                    DataConclusao = table.Column<DateOnly>(type: "date", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeiculosOrdensServico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VeiculosOrdensServico_Veiculos_VeiculoId",
                        column: x => x.VeiculoId,
                        principalSchema: "patrimonio",
                        principalTable: "Veiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "patrimonio",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bens_TenantId_NumeroTombamento",
                schema: "patrimonio",
                table: "Bens",
                columns: new[] { "TenantId", "NumeroTombamento" });

            migrationBuilder.CreateIndex(
                name: "IX_BensHistoricosDepreciacao_BemPatrimonialId",
                schema: "patrimonio",
                table: "BensHistoricosDepreciacao",
                column: "BemPatrimonialId");

            migrationBuilder.CreateIndex(
                name: "IX_BensImpairments_BemPatrimonialId",
                schema: "patrimonio",
                table: "BensImpairments",
                column: "BemPatrimonialId");

            migrationBuilder.CreateIndex(
                name: "IX_BensMovimentacoes_BemPatrimonialId",
                schema: "patrimonio",
                table: "BensMovimentacoes",
                column: "BemPatrimonialId");

            migrationBuilder.CreateIndex(
                name: "IX_BensReavaliacoes_BemPatrimonialId",
                schema: "patrimonio",
                table: "BensReavaliacoes",
                column: "BemPatrimonialId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensEstoque_TenantId_Codigo",
                schema: "patrimonio",
                table: "ItensEstoque",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensEstoqueLotes_ItemEstoqueId",
                schema: "patrimonio",
                table: "ItensEstoqueLotes",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensEstoqueMovimentos_ItemEstoqueId",
                schema: "patrimonio",
                table: "ItensEstoqueMovimentos",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensEstoqueRequisicoes_ItemEstoqueId",
                schema: "patrimonio",
                table: "ItensEstoqueRequisicoes",
                column: "ItemEstoqueId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "patrimonio",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculos_TenantId_Renavam",
                schema: "patrimonio",
                table: "Veiculos",
                columns: new[] { "TenantId", "Renavam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VeiculosAbastecimentos_VeiculoId",
                schema: "patrimonio",
                table: "VeiculosAbastecimentos",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculosLicenciamentos_VeiculoId",
                schema: "patrimonio",
                table: "VeiculosLicenciamentos",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculosMotoristas_VeiculoId",
                schema: "patrimonio",
                table: "VeiculosMotoristas",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculosMultas_VeiculoId",
                schema: "patrimonio",
                table: "VeiculosMultas",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_VeiculosOrdensServico_VeiculoId",
                schema: "patrimonio",
                table: "VeiculosOrdensServico",
                column: "VeiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "BensHistoricosDepreciacao",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "BensImpairments",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "BensMovimentacoes",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "BensReavaliacoes",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ItensEstoqueLotes",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ItensEstoqueMovimentos",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ItensEstoqueRequisicoes",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "VeiculosAbastecimentos",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "VeiculosLicenciamentos",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "VeiculosMotoristas",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "VeiculosMultas",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "VeiculosOrdensServico",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "Bens",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "ItensEstoque",
                schema: "patrimonio");

            migrationBuilder.DropTable(
                name: "Veiculos",
                schema: "patrimonio");
        }
    }
}
