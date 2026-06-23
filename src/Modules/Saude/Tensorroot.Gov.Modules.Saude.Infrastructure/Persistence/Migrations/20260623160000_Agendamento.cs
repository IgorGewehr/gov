using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Agendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "AgendaVagas",
                schema: "saude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgendaProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataMarcacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OrigemCancelamento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AtendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
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
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataEntrada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataConvocacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilasEspera", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendasProfissional_TenantId_ProfissionalId_Data",
                schema: "saude",
                table: "AgendasProfissional",
                columns: new[] { "TenantId", "ProfissionalId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendasProfissional_TenantId_EstabelecimentoId_Data",
                schema: "saude",
                table: "AgendasProfissional",
                columns: new[] { "TenantId", "EstabelecimentoId", "Data" });

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
                name: "IX_FilasEspera_TenantId_EstabelecimentoId_Situacao",
                schema: "saude",
                table: "FilasEspera",
                columns: new[] { "TenantId", "EstabelecimentoId", "Situacao" });

            migrationBuilder.CreateIndex(
                name: "IX_FilasEspera_TenantId_ProfissionalId_Situacao",
                schema: "saude",
                table: "FilasEspera",
                columns: new[] { "TenantId", "ProfissionalId", "Situacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendaVagas",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "Agendamentos",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "FilasEspera",
                schema: "saude");

            migrationBuilder.DropTable(
                name: "AgendasProfissional",
                schema: "saude");
        }
    }
}
