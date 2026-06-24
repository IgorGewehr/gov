using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaudeOnda3CadastrosAgendaFarmaciaVisaEAuditHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "saude",
                table: "AuditTrail");

            migrationBuilder.CreateTable(
                name: "Agendamentos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Prioridade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataMarcacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrigemCancelamento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgendasProfissional",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFim = table.Column<TimeOnly>(type: "time", nullable: false),
                    DuracaoSlotMinutos = table.Column<int>(type: "int", nullable: false),
                    CapacidadeVagas = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendasProfissional", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutosVisa",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoFiscalizavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Fundamentacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataLavratura = table.Column<DateOnly>(type: "date", nullable: false),
                    PrazoFinal = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorMulta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Defesa = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutosVisa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarteirasVacinacao",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarteirasVacinacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dispensacoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescricaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispensacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estabelecimentos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cnes = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estabelecimentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstabelecimentosFiscalizaveis",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoPersistido = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RazaoSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Ramo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Risco = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstabelecimentosFiscalizaveis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstoquesMedicamento",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PontoDeRessuprimento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstoquesMedicamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilasEspera",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Especialidade = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Prioridade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataEntrada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataConvocacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilasEspera", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Imunobiologicos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Sigla = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalDoses = table.Column<int>(type: "int", nullable: false),
                    IntervaloDiasProximaDose = table.Column<int>(type: "int", nullable: false),
                    DoseUnica = table.Column<bool>(type: "bit", nullable: false),
                    MedicamentoEstoqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imunobiologicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inspecoes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoFiscalizavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInspecao = table.Column<DateOnly>(type: "date", nullable: false),
                    FiscalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Roteiro = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspecoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicencasSanitarias",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoFiscalizavelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmitidaEm = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidadeAte = table.Column<DateOnly>(type: "date", nullable: false),
                    InspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicencasSanitarias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medicamentos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipioAtivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Apresentacao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Concentracao = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Forma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Unidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Controle = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CodigoCatmat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Cns = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Registro = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgendaVagas",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgendaProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaVagas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendaVagas_AgendasProfissional_AgendaProfissionalId",
                        column: x => x.AgendaProfissionalId,
                        principalSchema: "saude",
                        principalTable: "AgendasProfissional",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarteirasVacinacaoDoses",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImunobiologicoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroDose = table.Column<int>(type: "int", nullable: false),
                    Lote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AplicadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataAplicacao = table.Column<DateOnly>(type: "date", nullable: false),
                    ProximaDoseAprazada = table.Column<DateOnly>(type: "date", nullable: true),
                    CarteiraVacinacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarteirasVacinacaoDoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarteirasVacinacaoDoses_CarteirasVacinacao_CarteiraVacinacaoId",
                        column: x => x.CarteiraVacinacaoId,
                        principalSchema: "saude",
                        principalTable: "CarteirasVacinacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispensacoesItens",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Posologia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DispensacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensacoesItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispensacoesItens_Dispensacoes_DispensacaoId",
                        column: x => x.DispensacaoId,
                        principalSchema: "saude",
                        principalTable: "Dispensacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstoquesMedicamentoLotes",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Validade = table.Column<DateOnly>(type: "date", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantidadeEntrada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    EstoqueMedicamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstoquesMedicamentoLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstoquesMedicamentoLotes_EstoquesMedicamento_EstoqueMedicamentoId",
                        column: x => x.EstoqueMedicamentoId,
                        principalSchema: "saude",
                        principalTable: "EstoquesMedicamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspecoesItens",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Requisito = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Conformidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspecaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspecoesItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspecoesItens_Inspecoes_InspecaoId",
                        column: x => x.InspecaoId,
                        principalSchema: "saude",
                        principalTable: "Inspecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfissionaisVinculos",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cbo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfissionaisVinculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfissionaisVinculos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalSchema: "saude",
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispensacoesItensBaixas",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLote = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Validade = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ItemDispensadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispensacoesItensBaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispensacoesItensBaixas_DispensacoesItens_ItemDispensadoId",
                        column: x => x.ItemDispensadoId,
                        principalSchema: "saude",
                        principalTable: "DispensacoesItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "saude",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" },
                unique: true,
                filter: "[Sequencia] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_TenantId_PacienteId_DataHora",
                schema: "saude",
                table: "Agendamentos",
                columns: new[] { "TenantId", "PacienteId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_TenantId_ProfissionalId_DataHora",
                schema: "saude",
                table: "Agendamentos",
                columns: new[] { "TenantId", "ProfissionalId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_TenantId_Situacao",
                schema: "saude",
                table: "Agendamentos",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendasProfissional_TenantId_EstabelecimentoId_Data",
                schema: "saude",
                table: "AgendasProfissional",
                columns: new[] { "TenantId", "EstabelecimentoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendasProfissional_TenantId_ProfissionalId_Data",
                schema: "saude",
                table: "AgendasProfissional",
                columns: new[] { "TenantId", "ProfissionalId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaVagas_AgendaProfissionalId_Situacao",
                schema: "saude",
                table: "AgendaVagas",
                columns: new[] { "AgendaProfissionalId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaVagas_DataHora",
                schema: "saude",
                table: "AgendaVagas",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_AutosVisa_TenantId_Numero",
                schema: "saude",
                table: "AutosVisa",
                columns: new[] { "TenantId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutosVisa_TenantId_PrazoFinal",
                schema: "saude",
                table: "AutosVisa",
                columns: new[] { "TenantId", "PrazoFinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AutosVisa_TenantId_Situacao",
                schema: "saude",
                table: "AutosVisa",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasVacinacao_TenantId_PacienteId",
                schema: "saude",
                table: "CarteirasVacinacao",
                columns: new[] { "TenantId", "PacienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasVacinacaoDoses_CarteiraVacinacaoId",
                schema: "saude",
                table: "CarteirasVacinacaoDoses",
                column: "CarteiraVacinacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarteirasVacinacaoDoses_ProximaDoseAprazada",
                schema: "saude",
                table: "CarteirasVacinacaoDoses",
                column: "ProximaDoseAprazada");

            migrationBuilder.CreateIndex(
                name: "IX_Dispensacoes_TenantId_PacienteId",
                schema: "saude",
                table: "Dispensacoes",
                columns: new[] { "TenantId", "PacienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispensacoesItens_DispensacaoId",
                schema: "saude",
                table: "DispensacoesItens",
                column: "DispensacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DispensacoesItensBaixas_ItemDispensadoId",
                schema: "saude",
                table: "DispensacoesItensBaixas",
                column: "ItemDispensadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_TenantId_Cnes",
                schema: "saude",
                table: "Estabelecimentos",
                columns: new[] { "TenantId", "Cnes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_TenantId_Nome",
                schema: "saude",
                table: "Estabelecimentos",
                columns: new[] { "TenantId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentosFiscalizaveis_TenantId_DocumentoPersistido_RazaoSocial",
                schema: "saude",
                table: "EstabelecimentosFiscalizaveis",
                columns: new[] { "TenantId", "DocumentoPersistido", "RazaoSocial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentosFiscalizaveis_TenantId_Situacao",
                schema: "saude",
                table: "EstabelecimentosFiscalizaveis",
                columns: new[] { "TenantId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_EstoquesMedicamento_TenantId_EstabelecimentoId_MedicamentoId",
                schema: "saude",
                table: "EstoquesMedicamento",
                columns: new[] { "TenantId", "EstabelecimentoId", "MedicamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstoquesMedicamentoLotes_EstoqueMedicamentoId",
                schema: "saude",
                table: "EstoquesMedicamentoLotes",
                column: "EstoqueMedicamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_FilasEspera_TenantId_EstabelecimentoId_Situacao",
                schema: "saude",
                table: "FilasEspera",
                columns: new[] { "TenantId", "EstabelecimentoId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_FilasEspera_TenantId_ProfissionalId_Situacao",
                schema: "saude",
                table: "FilasEspera",
                columns: new[] { "TenantId", "ProfissionalId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Imunobiologicos_TenantId_Sigla",
                schema: "saude",
                table: "Imunobiologicos",
                columns: new[] { "TenantId", "Sigla" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspecoes_TenantId_DataInspecao",
                schema: "saude",
                table: "Inspecoes",
                columns: new[] { "TenantId", "DataInspecao" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspecoes_TenantId_EstabelecimentoFiscalizavelId",
                schema: "saude",
                table: "Inspecoes",
                columns: new[] { "TenantId", "EstabelecimentoFiscalizavelId" });

            migrationBuilder.CreateIndex(
                name: "IX_InspecoesItens_InspecaoId",
                schema: "saude",
                table: "InspecoesItens",
                column: "InspecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LicencasSanitarias_TenantId_EstabelecimentoFiscalizavelId",
                schema: "saude",
                table: "LicencasSanitarias",
                columns: new[] { "TenantId", "EstabelecimentoFiscalizavelId" });

            migrationBuilder.CreateIndex(
                name: "IX_LicencasSanitarias_TenantId_Situacao_ValidadeAte",
                schema: "saude",
                table: "LicencasSanitarias",
                columns: new[] { "TenantId", "Situacao", "ValidadeAte" });

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_TenantId_PrincipioAtivo",
                schema: "saude",
                table: "Medicamentos",
                columns: new[] { "TenantId", "PrincipioAtivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_TenantId_Cpf",
                schema: "saude",
                table: "Profissionais",
                columns: new[] { "TenantId", "Cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_TenantId_Nome",
                schema: "saude",
                table: "Profissionais",
                columns: new[] { "TenantId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionaisVinculos_ProfissionalId_EstabelecimentoId",
                schema: "saude",
                table: "ProfissionaisVinculos",
                columns: new[] { "ProfissionalId", "EstabelecimentoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agendamentos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AgendaVagas",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AutosVisa",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "CarteirasVacinacaoDoses",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "DispensacoesItensBaixas",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Estabelecimentos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "EstabelecimentosFiscalizaveis",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "EstoquesMedicamentoLotes",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "FilasEspera",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Imunobiologicos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "InspecoesItens",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "LicencasSanitarias",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Medicamentos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "ProfissionaisVinculos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AgendasProfissional",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "CarteirasVacinacao",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "DispensacoesItens",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "EstoquesMedicamento",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Inspecoes",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Profissionais",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Dispensacoes",
                schema: "saude");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "saude",
                table: "AuditTrail");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "saude",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });
        }
    }
}
