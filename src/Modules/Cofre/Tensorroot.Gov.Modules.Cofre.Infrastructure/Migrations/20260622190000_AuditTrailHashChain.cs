using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Migrations
{
    /// <summary>
    /// A2 — Imutabilidade da trilha: adiciona a CADEIA DE HASH à AuditTrail (Sequencia, HashAnterior,
    /// HashAtual) com DEFAULTS SEGUROS para linhas legadas (Sequencia=0, hashes vazios → "fora da
    /// cadeia"), índice (TenantId, Sequencia) e os triggers WORM INSTEAD OF UPDATE/DELETE (SqlServer).
    /// </summary>
    public partial class AuditTrailHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Sequencia",
                schema: "cofre",
                table: "AuditTrail",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                schema: "cofre",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAtual",
                schema: "cofre",
                table: "AuditTrail",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "cofre",
                table: "AuditTrail",
                columns: new[] { "TenantId", "Sequencia" });

            // WORM: bloqueia UPDATE/DELETE na trilha no nível do banco (complementa a hash-chain).
            // // TODO(prod): só SqlServer; em SQLite (dev) a detecção fica a cargo do verificador.
            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [cofre].[TR_AuditTrail_NoUpdate]
                    ON [cofre].[AuditTrail]
                    INSTEAD OF UPDATE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        THROW 51001, ''AuditTrail e imutavel (WORM): UPDATE bloqueado.'', 1;
                    END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [cofre].[TR_AuditTrail_NoDelete]
                    ON [cofre].[AuditTrail]
                    INSTEAD OF DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        THROW 51002, ''AuditTrail e imutavel (WORM): DELETE bloqueado.'', 1;
                    END');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[cofre].[TR_AuditTrail_NoUpdate]', N'TR') IS NOT NULL DROP TRIGGER [cofre].[TR_AuditTrail_NoUpdate];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[cofre].[TR_AuditTrail_NoDelete]', N'TR') IS NOT NULL DROP TRIGGER [cofre].[TR_AuditTrail_NoDelete];");

            migrationBuilder.DropIndex(
                name: "IX_AuditTrail_TenantId_Sequencia",
                schema: "cofre",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "Sequencia",
                schema: "cofre",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                schema: "cofre",
                table: "AuditTrail");

            migrationBuilder.DropColumn(
                name: "HashAtual",
                schema: "cofre",
                table: "AuditTrail");
        }
    }
}
