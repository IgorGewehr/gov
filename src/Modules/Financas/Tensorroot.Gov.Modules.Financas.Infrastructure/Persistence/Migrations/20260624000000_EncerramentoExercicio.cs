using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EncerramentoExercicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "encerramento_exercicio",
                schema: "financas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exercicio = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IniciadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EncerradoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AberturaConcluidaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_encerramento_exercicio", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_encerramento_exercicio_TenantId_Exercicio",
                schema: "financas",
                table: "encerramento_exercicio",
                columns: ["TenantId", "Exercicio"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.DropTable(name: "encerramento_exercicio", schema: "financas");
        }
    }
}
