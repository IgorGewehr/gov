using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Leitura ATÔMICA do último selo (Sequencia + HashAtual) da cadeia de hash de um tenant — fonte
/// ÚNICA compartilhada pelos dois pontos que alocam uma nova posição na trilha imutável:
/// o <see cref="AuditSaveChangesInterceptor"/> (mutações) e o <see cref="RegistroAcessoSensivel"/>
/// (acessos sensíveis LG-2).
/// <para>
/// P0-1 (robustez da fundação): em SqlServer (produção) a leitura é feita sob
/// <c>UPDLOCK, HOLDLOCK</c> na conexão/transação ATUAL do contexto. O HOLDLOCK (range lock
/// serializable) impede que duas requisições CONCORRENTES do mesmo tenant leiam a mesma "cabeça"
/// da cadeia antes do INSERT — fechando a corrida que bifurcava a trilha e disparava FALSO alarme de
/// adulteração ao TCE. Demais provedores (SQLite/testes, escrita serializada) usam a leitura LINQ;
/// o índice ÚNICO FILTRADO em <c>(TenantId, Sequencia)</c> é o backstop em qualquer provedor.
/// </para>
/// </summary>
internal static class UltimoSeloReader
{
    private const string ProviderSqlServer = "Microsoft.EntityFrameworkCore.SqlServer";

    public static async Task<(long Sequencia, string Hash)> LerAsync(
        DbContext context,
        Guid tenantId,
        bool async,
        CancellationToken cancellationToken)
    {
        if (EhSqlServer(context))
        {
            return await LerComLockAsync(context, tenantId, async, cancellationToken).ConfigureAwait(false);
        }

        var ultimo = async
            ? await context.Set<AuditTrail>()
                .Where(linha => linha.TenantId == tenantId && linha.Sequencia > 0)
                .OrderByDescending(linha => linha.Sequencia)
                .Select(linha => new { linha.Sequencia, linha.HashAtual })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : context.Set<AuditTrail>()
                .Where(linha => linha.TenantId == tenantId && linha.Sequencia > 0)
                .OrderByDescending(linha => linha.Sequencia)
                .Select(linha => new { linha.Sequencia, linha.HashAtual })
                .FirstOrDefault();

        return ultimo is null || string.IsNullOrEmpty(ultimo.HashAtual)
            ? (0L, AuditHashChain.HashGenesis)
            : (ultimo.Sequencia, ultimo.HashAtual);
    }

    private static bool EhSqlServer(DbContext context)
        => string.Equals(context.Database.ProviderName, ProviderSqlServer, StringComparison.Ordinal);

    private static async Task<(long Sequencia, string Hash)> LerComLockAsync(
        DbContext context,
        Guid tenantId,
        bool async,
        CancellationToken cancellationToken)
    {
        var entityType = context.Model.FindEntityType(typeof(AuditTrail))
            ?? throw new InvalidOperationException("Entidade AuditTrail não mapeada no modelo.");
        var schema = entityType.GetSchema();
        var tabela = entityType.GetTableName() ?? "AuditTrail";
        var nomeQualificado = schema is null ? $"[{tabela}]" : $"[{schema}].[{tabela}]";

        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        var abrira = false;
        if (connection.State != ConnectionState.Open)
        {
            if (async)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                connection.Open();
            }

            abrira = true;
        }

        try
        {
            await using var comando = connection.CreateCommand();
            comando.Transaction = transaction;
            comando.CommandText =
                $"SELECT TOP 1 [Sequencia], [HashAtual] FROM {nomeQualificado} WITH (UPDLOCK, HOLDLOCK) "
                + "WHERE [TenantId] = @tenantId AND [Sequencia] > 0 ORDER BY [Sequencia] DESC";

            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@tenantId";
            parametro.Value = tenantId;
            comando.Parameters.Add(parametro);

            if (async)
            {
                await using var leitor = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await leitor.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return LerLinha(leitor);
                }
            }
            else
            {
                using var leitor = comando.ExecuteReader();
                if (leitor.Read())
                {
                    return LerLinha(leitor);
                }
            }

            return (0L, AuditHashChain.HashGenesis);
        }
        finally
        {
            // Só fecha se ESTE método abriu a conexão (sem transação ambiente). Com transação ambiente a
            // conexão pertence à UoW e deve permanecer aberta para o SaveChanges subsequente.
            if (abrira && transaction is null)
            {
                if (async)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
                else
                {
                    connection.Close();
                }
            }
        }
    }

    private static (long Sequencia, string Hash) LerLinha(DbDataReader leitor)
    {
        var sequencia = leitor.GetInt64(0);
        var hash = leitor.IsDBNull(1) ? null : leitor.GetString(1);
        return string.IsNullOrEmpty(hash)
            ? (0L, AuditHashChain.HashGenesis)
            : (sequencia, hash);
    }
}
