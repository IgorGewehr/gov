using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "transparencia");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "transparencia",
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
                name: "DeclaracoesFiscais",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDeclaracao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompetenciaAno = table.Column<int>(type: "int", nullable: true),
                    CompetenciaMes = table.Column<int>(type: "int", nullable: true),
                    BimestreAno = table.Column<int>(type: "int", nullable: true),
                    BimestreNumero = table.Column<int>(type: "int", nullable: true),
                    QuadrimestreAno = table.Column<int>(type: "int", nullable: true),
                    QuadrimestreNumero = table.Column<int>(type: "int", nullable: true),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    DataConsolidacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataTransmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    ProtocoloSiconfi = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    MotivoRejeicao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaracoesFiscais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "transparencia",
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
                name: "RemessasTce",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    PeriodoTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodoNumero = table.Column<int>(type: "int", nullable: false),
                    LeiauteCodigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LeiauteVersao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HashAlgoritmo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HashValor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DataLimite = table.Column<DateOnly>(type: "date", nullable: false),
                    DataGeracao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataEnvio = table.Column<DateOnly>(type: "date", nullable: true),
                    AlertaPrazoEmitido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasTce", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeclaracoesFiscaisMatrizes",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeclaracaoFiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaracoesFiscaisMatrizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclaracoesFiscaisMatrizes_DeclaracoesFiscais_DeclaracaoFiscalId",
                        column: x => x.DeclaracaoFiscalId,
                        principalSchema: "transparencia",
                        principalTable: "DeclaracoesFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemessasTceArquivos",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeArquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Conteudo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    HashAlgoritmo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HashValor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RemessaTceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasTceArquivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemessasTceArquivos_RemessasTce_RemessaTceId",
                        column: x => x.RemessaTceId,
                        principalSchema: "transparencia",
                        principalTable: "RemessasTce",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemessasTceResultadosValidacao",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PossuiErro = table.Column<bool>(type: "bit", nullable: false),
                    QuantidadeErros = table.Column<int>(type: "int", nullable: false),
                    QuantidadeAvisos = table.Column<int>(type: "int", nullable: false),
                    ValidadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LeiauteVersao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RemessaTceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasTceResultadosValidacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemessasTceResultadosValidacao_RemessasTce_RemessaTceId",
                        column: x => x.RemessaTceId,
                        principalSchema: "transparencia",
                        principalTable: "RemessasTce",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeclaracoesFiscaisLinhas",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaPcasp = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NaturezaSaldo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InformacaoComplementar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MatrizSaldosId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaracoesFiscaisLinhas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclaracoesFiscaisLinhas_DeclaracoesFiscaisMatrizes_MatrizSaldosId",
                        column: x => x.MatrizSaldosId,
                        principalSchema: "transparencia",
                        principalTable: "DeclaracoesFiscaisMatrizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemessasTceRegistros",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArquivoRemessaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasTceRegistros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemessasTceRegistros_RemessasTceArquivos_ArquivoRemessaId",
                        column: x => x.ArquivoRemessaId,
                        principalSchema: "transparencia",
                        principalTable: "RemessasTceArquivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemessasTceOcorrenciasValidacao",
                schema: "transparencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Arquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Linha = table.Column<int>(type: "int", nullable: false),
                    Severidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ResultadoValidacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasTceOcorrenciasValidacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemessasTceOcorrenciasValidacao_RemessasTceResultadosValidacao_ResultadoValidacaoId",
                        column: x => x.ResultadoValidacaoId,
                        principalSchema: "transparencia",
                        principalTable: "RemessasTceResultadosValidacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "transparencia",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclaracoesFiscais_TenantId_Exercicio_TipoDeclaracao",
                schema: "transparencia",
                table: "DeclaracoesFiscais",
                columns: new[] { "TenantId", "Exercicio", "TipoDeclaracao" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclaracoesFiscaisLinhas_MatrizSaldosId",
                schema: "transparencia",
                table: "DeclaracoesFiscaisLinhas",
                column: "MatrizSaldosId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclaracoesFiscaisMatrizes_DeclaracaoFiscalId",
                schema: "transparencia",
                table: "DeclaracoesFiscaisMatrizes",
                column: "DeclaracaoFiscalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "transparencia",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RemessasTce_TenantId",
                schema: "transparencia",
                table: "RemessasTce",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RemessasTceArquivos_RemessaTceId",
                schema: "transparencia",
                table: "RemessasTceArquivos",
                column: "RemessaTceId");

            migrationBuilder.CreateIndex(
                name: "IX_RemessasTceOcorrenciasValidacao_ResultadoValidacaoId",
                schema: "transparencia",
                table: "RemessasTceOcorrenciasValidacao",
                column: "ResultadoValidacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RemessasTceRegistros_ArquivoRemessaId",
                schema: "transparencia",
                table: "RemessasTceRegistros",
                column: "ArquivoRemessaId");

            migrationBuilder.CreateIndex(
                name: "IX_RemessasTceResultadosValidacao_RemessaTceId",
                schema: "transparencia",
                table: "RemessasTceResultadosValidacao",
                column: "RemessaTceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "DeclaracoesFiscaisLinhas",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "RemessasTceOcorrenciasValidacao",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "RemessasTceRegistros",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "DeclaracoesFiscaisMatrizes",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "RemessasTceResultadosValidacao",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "RemessasTceArquivos",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "DeclaracoesFiscais",
                schema: "transparencia");

            migrationBuilder.DropTable(
                name: "RemessasTce",
                schema: "transparencia");
        }
    }
}
