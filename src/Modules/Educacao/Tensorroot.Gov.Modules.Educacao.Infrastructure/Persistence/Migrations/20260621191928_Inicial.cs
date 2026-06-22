using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "educacao");

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                schema: "educacao",
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
                name: "DiariosClasse",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatriculaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CargaHorariaTotal = table.Column<int>(type: "int", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosClasse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Escolas",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoInep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DependenciaAdministrativa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EnderecoCep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    EnderecoLatitude = table.Column<double>(type: "float", nullable: false),
                    EnderecoLogradouro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnderecoLongitude = table.Column<double>(type: "float", nullable: false),
                    EnderecoMunicipio = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EnderecoUf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    InfraNumeroDependencias = table.Column<int>(type: "int", nullable: false),
                    InfraNumeroSalas = table.Column<int>(type: "int", nullable: false),
                    InfraPossuiAcessibilidade = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escolas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matriculas",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscolaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "date", nullable: false),
                    Situacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SituacaoRendimento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SituacaoMovimento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matriculas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "educacao",
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
                name: "DiariosClasseAulas",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DiaLetivo = table.Column<bool>(type: "bit", nullable: false),
                    DiarioClasseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosClasseAulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosClasseAulas_DiariosClasse_DiarioClasseId",
                        column: x => x.DiarioClasseId,
                        principalSchema: "educacao",
                        principalTable: "DiariosClasse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiariosClasseFrequencias",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Presente = table.Column<bool>(type: "bit", nullable: false),
                    CargaHorariaAula = table.Column<int>(type: "int", nullable: false),
                    DiarioClasseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosClasseFrequencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosClasseFrequencias_DiariosClasse_DiarioClasseId",
                        column: x => x.DiarioClasseId,
                        principalSchema: "educacao",
                        principalTable: "DiariosClasse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiariosClasseNotas",
                schema: "educacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Componente = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DiarioClasseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiariosClasseNotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiariosClasseNotas_DiariosClasse_DiarioClasseId",
                        column: x => x.DiarioClasseId,
                        principalSchema: "educacao",
                        principalTable: "DiariosClasse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_TimestampUtc",
                schema: "educacao",
                table: "AuditTrail",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DiariosClasse_TenantId_MatriculaId",
                schema: "educacao",
                table: "DiariosClasse",
                columns: new[] { "TenantId", "MatriculaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiariosClasseAulas_DiarioClasseId",
                schema: "educacao",
                table: "DiariosClasseAulas",
                column: "DiarioClasseId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosClasseFrequencias_DiarioClasseId",
                schema: "educacao",
                table: "DiariosClasseFrequencias",
                column: "DiarioClasseId");

            migrationBuilder.CreateIndex(
                name: "IX_DiariosClasseNotas_DiarioClasseId",
                schema: "educacao",
                table: "DiariosClasseNotas",
                column: "DiarioClasseId");

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_TenantId_CodigoInep",
                schema: "educacao",
                table: "Escolas",
                columns: new[] { "TenantId", "CodigoInep" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_TenantId_AlunoId",
                schema: "educacao",
                table: "Matriculas",
                columns: new[] { "TenantId", "AlunoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_TenantId_TurmaId_DataReferencia",
                schema: "educacao",
                table: "Matriculas",
                columns: new[] { "TenantId", "TurmaId", "DataReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                schema: "educacao",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrail",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DiariosClasseAulas",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DiariosClasseFrequencias",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DiariosClasseNotas",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "Escolas",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "Matriculas",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "educacao");

            migrationBuilder.DropTable(
                name: "DiariosClasse",
                schema: "educacao");
        }
    }
}
