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
            // Imutabilidade WORM da trilha: trigger INSTEAD OF UPDATE/DELETE (A2). Idempotente —
            // só SqlServer; no SQLite (dev) a detecção fica a cargo da cadeia de hash + verificador.
            await GarantirTriggerImutabilidadeAuditTrailAsync(contexto, cancellationToken).ConfigureAwait(false);
            // INBOX (W9.7): rede de segurança ADITIVA e idempotente. As migrations por módulo já criam a
            // tabela InboxMessages; esta garantia cobre bancos de tenant SqlServer já provisionados ANTES
            // da migration chegar — a fundação NUNCA pode travar por tabela ausente. No-op quando a tabela
            // já existe (criada pela migration).
            await GarantirTabelaInboxSqlServerAsync(contexto, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Cria (idempotente) os triggers <c>INSTEAD OF UPDATE/DELETE</c> na tabela <c>AuditTrail</c> do
    /// schema do módulo, bloqueando QUALQUER alteração/remoção da trilha no nível do banco (WORM).
    /// Em conjunto com a cadeia de hash, dá imutabilidade real exigida pelo Tribunal de Contas.
    /// <para>
    /// // TODO(prod): só SqlServer. Em dev (SQLite) não há trigger — a detecção de adulteração fica
    /// a cargo da hash-chain + verificador. Em SqlServer, considerar evoluir para ledger/temporal table.
    /// </para>
    /// </summary>
    private static async Task GarantirTriggerImutabilidadeAuditTrailAsync(DbContext contexto, CancellationToken cancellationToken)
    {
        // Schema isolado do módulo (ex.: "financas"). ModuleDbContext expõe Schema; fallback "dbo".
        var schema = (contexto as ModuleDbContext)?.Schema ?? "dbo";

        // Nomes são literais controlados (schema vem do código do módulo, não de entrada do usuário).
        // QUOTENAME protege a montagem do trigger; o EXEC roda DDL dentro do batch dinâmico.
        var sql = $"""
            IF OBJECT_ID(N'[{schema}].[TR_AuditTrail_NoUpdate]', N'TR') IS NULL
            BEGIN
                EXEC(N'CREATE TRIGGER [{schema}].[TR_AuditTrail_NoUpdate]
                    ON [{schema}].[AuditTrail]
                    INSTEAD OF UPDATE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        THROW 51001, ''AuditTrail e imutavel (WORM): UPDATE bloqueado.'', 1;
                    END');
            END;

            IF OBJECT_ID(N'[{schema}].[TR_AuditTrail_NoDelete]', N'TR') IS NULL
            BEGIN
                EXEC(N'CREATE TRIGGER [{schema}].[TR_AuditTrail_NoDelete]
                    ON [{schema}].[AuditTrail]
                    INSTEAD OF DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        THROW 51002, ''AuditTrail e imutavel (WORM): DELETE bloqueado.'', 1;
                    END');
            END;
            """;

        await contexto.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cria (idempotente) a tabela <c>InboxMessages</c> e o índice ÚNICO (EventId, Handler, TenantId) no
    /// schema do módulo, em SqlServer. Rede de segurança ADITIVA para bancos de tenant já provisionados
    /// antes da migration de Inbox: o consumo de Integration Events depende desta tabela, e a fundação não
    /// pode falhar com "Invalid object name". No-op quando a tabela já existe (criada por migration).
    /// </summary>
    private static async Task GarantirTabelaInboxSqlServerAsync(DbContext contexto, CancellationToken cancellationToken)
    {
        var schema = (contexto as ModuleDbContext)?.Schema ?? "dbo";

        // schema é literal controlado (vem do código do módulo). QUOTENAME protege a montagem do DDL.
        var sql = $"""
            IF OBJECT_ID(N'[{schema}].[InboxMessages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{schema}].[InboxMessages] (
                    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_InboxMessages] PRIMARY KEY,
                    [TenantId] UNIQUEIDENTIFIER NOT NULL,
                    [EventId] UNIQUEIDENTIFIER NOT NULL,
                    [Handler] NVARCHAR(500) NOT NULL,
                    [EventType] NVARCHAR(500) NULL,
                    [ProcessedOnUtc] DATETIME2 NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InboxMessages_EventId_Handler_TenantId'
                AND object_id = OBJECT_ID(N'[{schema}].[InboxMessages]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_InboxMessages_EventId_Handler_TenantId]
                    ON [{schema}].[InboxMessages] ([EventId], [Handler], [TenantId]);
            END;
            """;

        await contexto.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
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
