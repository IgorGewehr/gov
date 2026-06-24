using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProposicaoAprovacoesTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProposicoesAprovacoesTurno",
                schema: "legislativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposicaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    MaioriaAtingida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposicaoOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposicoesAprovacoesTurno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposicoesAprovacoesTurno_Proposicoes_ProposicaoOwnerId",
                        column: x => x.ProposicaoOwnerId,
                        principalSchema: "legislativo",
                        principalTable: "Proposicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposicoesAprovacoesTurno_ProposicaoOwnerId_Numero",
                schema: "legislativo",
                table: "ProposicoesAprovacoesTurno",
                columns: new[] { "ProposicaoOwnerId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposicoesAprovacoesTurno",
                schema: "legislativo");
        }
    }
}
