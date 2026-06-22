using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Interceptor que, a cada SaveChanges, gera registros imutáveis de <see cref="AuditTrail"/>
/// (valores antes/depois em JSON, usuário, IP e timestamp) para o Tribunal de Contas.
/// <para>
/// Deve ser registrado DEPOIS do <c>TenantSaveChangesInterceptor</c> (ver
/// <see cref="ModuleInterceptorRegistration"/>): assim o <c>TenantId</c> já está carimbado quando a
/// trilha o lê. Cada linha inserida é SELADA numa cadeia de hash por tenant
/// (<see cref="AuditHashChain"/>): lê-se o último selo do tenant e encadeia-se a nova linha,
/// tornando a trilha à prova de adulteração/remoção (detecção via verificador).
/// </para>
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            AddAuditEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            await AddAuditEntriesAsync(eventData.Context, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private void AddAuditEntries(DbContext context)
    {
        var entries = BuildEntries(context);
        if (entries.Count == 0)
        {
            return;
        }

        EncadearHash(entries, tenantId => UltimoSeloDoTenant(context, tenantId));
        context.Set<AuditTrail>().AddRange(entries);
    }

    private async Task AddAuditEntriesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entries = BuildEntries(context);
        if (entries.Count == 0)
        {
            return;
        }

        // Pré-carrega o último selo de cada tenant presente no lote (uma consulta por tenant),
        // depois encadeia em memória. Evita I/O dentro do laço de encadeamento.
        var ultimos = new Dictionary<Guid, (long Sequencia, string Hash)>();
        foreach (var tenantId in entries.Select(linha => linha.TenantId).Distinct())
        {
            ultimos[tenantId] = await UltimoSeloDoTenantAsync(context, tenantId, cancellationToken).ConfigureAwait(false);
        }

        EncadearHash(entries, tenantId => ultimos[tenantId]);
        context.Set<AuditTrail>().AddRange(entries);
    }

    /// <summary>Constrói as linhas de trilha do change-tracker (sem ainda selar a cadeia).</summary>
    private List<AuditTrail> BuildEntries(DbContext context)
    {
        // Trunca para MILISSEGUNDOS: o conteúdo canônico da cadeia inclui o timestamp e precisa ser
        // ESTÁVEL ao round-trip de persistência (alguns provedores, ex. SQLite, não preservam os 7
        // dígitos fracionários). Sem isto, o verificador recomputaria um selo diferente do gravado.
        var nowUtc = TruncarParaMilissegundos(timeProvider.GetUtcNow().UtcDateTime);
        var entries = new List<AuditTrail>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditTrail or OutboxMessage)
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var oldValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            var newValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            var affected = new List<string>();
            object? primaryKey = null;

            // LG-3: redacao DENY-BY-DEFAULT de PII. Material cifrado (Cofre) continua via opt-in, mas
            // CPF/NIS/CNS/dado clinico sao redigidos por CONVENCAO (atributo/nome) mesmo sem opt-in —
            // a politica unica decide por propriedade (PoliticaRedacaoAuditoria). CLAUDE.md §6.
            var colunasCifradas = entry.Entity is IHasRedactedAuditFields sensivel
                ? sensivel.ColunasAuditoriaRedactadas
                : null;

            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    primaryKey = property.CurrentValue;
                }

                var redactar = PoliticaRedacaoAuditoria.DeveRedigir(property.Metadata, colunasCifradas);

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        oldValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.OriginalValue;
                        break;
                    case EntityState.Modified when property.IsModified:
                        oldValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.OriginalValue;
                        newValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.CurrentValue;
                        affected.Add(name);
                        break;
                    default:
                        break;
                }
            }

            // Com a ordem CORRETA dos interceptors (Tenant -> Audit), o TenantId JA foi carimbado pelo
            // TenantSaveChangesInterceptor mesmo nas entidades cujo factory nao o define (W0.2).
            var tenantId = entry.Entity is IMustHaveTenant tenant ? tenant.TenantId : Guid.Empty;

            entries.Add(new AuditTrail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = primaryKey is null ? null : Convert.ToString(primaryKey, CultureInfo.InvariantCulture),
                Action = entry.State.ToString(),
                // LG-3: a redacao por-propriedade acima cobre colunas escalares (CPF/NIS/CNS). PII
                // guardada DENTRO de um Value Object OWNED serializado como sub-objeto (ex.: o Paciente
                // grava Identificacao como JSON aninhado com Cpf.Digitos) escaparia da checagem por
                // nome de propriedade EF. Passamos o blob final pela mesma politica RECURSIVA do
                // visualizador para mascarar PII em qualquer profundidade ANTES de selar/encadear —
                // deny-by-default, nada de PII em claro na trilha.
                OldValues = oldValues.Count > 0 ? PoliticaRedacaoAuditoria.MascararJson(JsonSerializer.Serialize(oldValues)) : null,
                NewValues = newValues.Count > 0 ? PoliticaRedacaoAuditoria.MascararJson(JsonSerializer.Serialize(newValues)) : null,
                AffectedColumns = affected.Count > 0 ? JsonSerializer.Serialize(affected) : null,
                UserId = currentUser.UserId,
                IpAddress = currentUser.IpAddress,
                TimestampUtc = nowUtc,
            });
        }

        return entries;
    }

    /// <summary>
    /// Encadeia o hash de cada linha por tenant: a 1ª linha do lote parte do último selo persistido
    /// do tenant (ou do genesis), e cada linha seguinte parte do selo da anterior. Como
    /// <see cref="AuditTrail"/> é <c>init</c>-only, recria-se a linha com os campos da cadeia.
    /// </summary>
    private static void EncadearHash(
        List<AuditTrail> entries,
        Func<Guid, (long Sequencia, string Hash)> ultimoSelo)
    {
        // Estado corrente por tenant (sequência + último hash), iniciado pelo persistido.
        var estado = new Dictionary<Guid, (long Sequencia, string Hash)>();

        for (var i = 0; i < entries.Count; i++)
        {
            var linha = entries[i];
            if (!estado.TryGetValue(linha.TenantId, out var atual))
            {
                atual = ultimoSelo(linha.TenantId);
                estado[linha.TenantId] = atual;
            }

            var sequencia = atual.Sequencia + 1;
            var hashAnterior = atual.Hash;

            // Conteúdo canônico (inclui Sequencia) — fonte única em AuditHashChain.
            var conteudo = AuditHashChain.ConteudoCanonico(
                linha.Id,
                linha.TenantId,
                sequencia,
                linha.EntityName,
                linha.EntityId,
                linha.Action,
                linha.OldValues,
                linha.NewValues,
                linha.AffectedColumns,
                linha.UserId,
                linha.IpAddress,
                linha.TimestampUtc);
            var hashAtual = AuditHashChain.Selar(hashAnterior, conteudo);

            entries[i] = new AuditTrail
            {
                Id = linha.Id,
                TenantId = linha.TenantId,
                EntityName = linha.EntityName,
                EntityId = linha.EntityId,
                Action = linha.Action,
                OldValues = linha.OldValues,
                NewValues = linha.NewValues,
                AffectedColumns = linha.AffectedColumns,
                UserId = linha.UserId,
                IpAddress = linha.IpAddress,
                TimestampUtc = linha.TimestampUtc,
                Sequencia = sequencia,
                HashAnterior = hashAnterior,
                HashAtual = hashAtual,
            };

            estado[linha.TenantId] = (sequencia, hashAtual);
        }
    }

    /// <summary>Trunca um instante UTC para milissegundos (estabilidade do selo ao round-trip).</summary>
    private static DateTime TruncarParaMilissegundos(DateTime valor)
        => new(valor.Ticks - (valor.Ticks % TimeSpan.TicksPerMillisecond), valor.Kind);

    private static (long Sequencia, string Hash) UltimoSeloDoTenant(DbContext context, Guid tenantId)
    {
        var ultimo = context.Set<AuditTrail>()
            .Where(linha => linha.TenantId == tenantId && linha.Sequencia > 0)
            .OrderByDescending(linha => linha.Sequencia)
            .Select(linha => new { linha.Sequencia, linha.HashAtual })
            .FirstOrDefault();

        return ultimo is null || string.IsNullOrEmpty(ultimo.HashAtual)
            ? (0L, AuditHashChain.HashGenesis)
            : (ultimo.Sequencia, ultimo.HashAtual);
    }

    private static async Task<(long Sequencia, string Hash)> UltimoSeloDoTenantAsync(
        DbContext context,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var ultimo = await context.Set<AuditTrail>()
            .Where(linha => linha.TenantId == tenantId && linha.Sequencia > 0)
            .OrderByDescending(linha => linha.Sequencia)
            .Select(linha => new { linha.Sequencia, linha.HashAtual })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return ultimo is null || string.IsNullOrEmpty(ultimo.HashAtual)
            ? (0L, AuditHashChain.HashGenesis)
            : (ultimo.Sequencia, ultimo.HashAtual);
    }
}
