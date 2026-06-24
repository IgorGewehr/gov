using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IssMunicipioIncidenciaLc116Art3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // T-W2 — código IBGE do município do tenant na tabela de ISS, para confronto com o
            // município de incidência da NFS-e (LC 116/2003 art. 3º). Opcional/parametrizável.
            migrationBuilder.AddColumn<string>(
                name: "MunicipioIbge",
                schema: "tributos",
                table: "TabelasAliquotaIss",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MunicipioIbge",
                schema: "tributos",
                table: "TabelasAliquotaIss");
        }
    }
}
