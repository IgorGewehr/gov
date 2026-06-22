using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnidadesEAtribuicoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AtribuicoesPapel",
                schema: "identidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PapelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncluiSubunidades = table.Column<bool>(type: "bit", nullable: false),
                    VigenciaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VigenciaFim = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OrigemTipo = table.Column<int>(type: "int", nullable: false),
                    OrigemConcedentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtribuicoesPapel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtribuicoesPapel_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "identidade",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesOrganizacionais",
                schema: "identidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    UnidadePaiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesOrganizacionais", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtribuicoesPapel_UsuarioId_PapelId_UnidadeId",
                schema: "identidade",
                table: "AtribuicoesPapel",
                columns: new[] { "UsuarioId", "PapelId", "UnidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesOrganizacionais_TenantId_Codigo",
                schema: "identidade",
                table: "UnidadesOrganizacionais",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesOrganizacionais_TenantId_UnidadePaiId",
                schema: "identidade",
                table: "UnidadesOrganizacionais",
                columns: new[] { "TenantId", "UnidadePaiId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtribuicoesPapel",
                schema: "identidade");

            migrationBuilder.DropTable(
                name: "UnidadesOrganizacionais",
                schema: "identidade");
        }
    }
}
