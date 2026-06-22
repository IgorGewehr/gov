using Microsoft.EntityFrameworkCore;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;

/// <summary>
/// Cria/atualiza o schema de um módulo no banco DEDICADO do tenant.
/// <para>
/// Em <b>SqlServer</b> usa migrations (<c>Migrate</c>). Em <b>SQLite</b> (desenvolvimento), como
/// vários módulos COMPARTILHAM o mesmo arquivo do tenant, <c>EnsureCreated</c> só cria tabelas
/// quando o banco ainda não existe (vira no-op a partir do 2º módulo). Por isso aplicamos o
/// script de criação de tabelas de forma IDEMPOTENTE (<c>CREATE ... IF NOT EXISTS</c>),
/// garantindo que cada módulo materialize suas tabelas no arquivo compartilhado.
/// </para>
/// </summary>
public static class SchemaProvisioner
{
    /// <summary>Aplica o schema do contexto no banco do tenant.</summary>
    /// <param name="contexto">DbContext do módulo, já apontando para o banco do tenant.</param>
    /// <param name="ehSqlServer">Verdadeiro para SqlServer (usa migrations); falso para SQLite.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public static async Task AplicarAsync(DbContext contexto, bool ehSqlServer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (ehSqlServer)
        {
            await contexto.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await contexto.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var script = contexto.Database.GenerateCreateScript()
            .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ", StringComparison.Ordinal)
            .Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ", StringComparison.Ordinal)
            .Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ", StringComparison.Ordinal);

        await contexto.Database.ExecuteSqlRawAsync(script, cancellationToken).ConfigureAwait(false);

        // CREATE TABLE IF NOT EXISTS é no-op quando a tabela JÁ existe — então colunas NOVAS de
        // tabelas pré-existentes (ex.: a resiliência do Outbox: AttemptCount/NextAttemptUtc/
        // DeadLetteredOnUtc) não seriam materializadas em bancos de tenant já criados. Aplicamos as
        // alterações de forma IDEMPOTENTE no SQLite (PRAGMA + ALTER TABLE ADD COLUMN só quando falta).
        await GarantirColunasOutboxAsync(contexto, cancellationToken).ConfigureAwait(false);
    }

    private static async Task GarantirColunasOutboxAsync(DbContext contexto, CancellationToken cancellationToken)
    {
        var conexao = contexto.Database.GetDbConnection();
        if (conexao.State != System.Data.ConnectionState.Open)
        {
            await conexao.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var pragma = conexao.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info('OutboxMessages');";
            await using var reader = await pragma.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Coluna 1 do PRAGMA table_info é o nome.
                existentes.Add(reader.GetString(1));
            }
        }

        if (existentes.Count == 0)
        {
            return; // Tabela ainda não existe (será criada pelo script acima na primeira passagem).
        }

        await AdicionarColunaSeFaltarAsync(conexao, existentes, "AttemptCount", "INTEGER NOT NULL DEFAULT 0", cancellationToken).ConfigureAwait(false);
        await AdicionarColunaSeFaltarAsync(conexao, existentes, "NextAttemptUtc", "TEXT NULL", cancellationToken).ConfigureAwait(false);
        await AdicionarColunaSeFaltarAsync(conexao, existentes, "DeadLetteredOnUtc", "TEXT NULL", cancellationToken).ConfigureAwait(false);
    }

    private static async Task AdicionarColunaSeFaltarAsync(
        System.Data.Common.DbConnection conexao,
        HashSet<string> existentes,
        string coluna,
        string definicao,
        CancellationToken cancellationToken)
    {
        if (existentes.Contains(coluna))
        {
            return;
        }

        await using var comando = conexao.CreateCommand();
        // Nomes de coluna/definição são literais controlados (não há entrada do usuário) — sem risco de injeção.
        comando.CommandText = $"ALTER TABLE \"OutboxMessages\" ADD COLUMN \"{coluna}\" {definicao};";
        await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
