using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Adaptador (LG-2) que sela um ACESSO de LEITURA sensivel na mesma trilha imutavel das mutacoes:
/// grava uma linha de <see cref="AuditTrail"/> com <c>Action = "Read"</c> no <see cref="ModuleDbContext"/>
/// ativo do escopo (resolvido pelo <see cref="ScopeDbContextHolder"/>, populado pelo handler ao
/// consultar o repositorio), encadeando-a na cadeia de hash do tenant via <see cref="AuditHashChain"/>.
/// <para>
/// O <see cref="AuditSaveChangesInterceptor"/> IGNORA entidades <see cref="AuditTrail"/> (nao as
/// re-sela), entao o encadeamento e feito AQUI, com a mesma logica de "ultimo selo do tenant".
/// Os metadados gravados sao <c>{Tenant, UserId, Ip, Entidade, EntityId, BaseLegal, Ts}</c>.
/// </para>
/// </summary>
public sealed class RegistroAcessoSensivel(
    ScopeDbContextHolder holder,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    TimeProvider timeProvider)
    : IRegistroAcessoSensivel
{
    /// <summary>Acao gravada na trilha para diferenciar a LEITURA sensivel das mutacoes.</summary>
    public const string AcaoLeitura = "Read";

    /// <inheritdoc />
    public async Task RegistrarAsync(
        string entidade,
        string? entidadeId,
        BaseLegalLgpd baseLegal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entidade);

        var contexto = holder.Atual
            ?? throw new InvalidOperationException(
                "Nenhum DbContext de modulo foi resolvido neste escopo; a trilha de acesso sensivel (LG-2) " +
                "nao tem onde ser selada. A query sensivel deve consultar o repositorio do modulo.");

        // Tenant do PRINCIPAL (JWT), nunca de entrada — espelha o que o interceptor faz nas mutacoes.
        var tenantId = tenantContext.HasTenant ? tenantContext.TenantId : Guid.Empty;

        var agoraUtc = TruncarParaMilissegundos(timeProvider.GetUtcNow().UtcDateTime);

        var (sequenciaAnterior, hashAnterior) = await UltimoSeloDoTenantAsync(contexto, tenantId, cancellationToken)
            .ConfigureAwait(false);
        var sequencia = sequenciaAnterior + 1;

        var id = Guid.NewGuid();
        // O detalhe do acesso (base legal) vai em NewValues, como objeto JSON — sem PII.
        var newValues = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["BaseLegal"] = baseLegal.ToString(),
        });

        var conteudo = AuditHashChain.ConteudoCanonico(
            id, tenantId, sequencia, entidade, entidadeId, AcaoLeitura,
            oldValues: null, newValues: newValues, affectedColumns: null,
            currentUser.UserId, currentUser.IpAddress, agoraUtc);
        var hashAtual = AuditHashChain.Selar(hashAnterior, conteudo);

        contexto.Set<AuditTrail>().Add(new AuditTrail
        {
            Id = id,
            TenantId = tenantId,
            EntityName = entidade,
            EntityId = entidadeId,
            Action = AcaoLeitura,
            OldValues = null,
            NewValues = newValues,
            AffectedColumns = null,
            UserId = currentUser.UserId,
            IpAddress = currentUser.IpAddress,
            TimestampUtc = agoraUtc,
            Sequencia = sequencia,
            HashAnterior = hashAnterior,
            HashAtual = hashAtual,
        });

        await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DateTime TruncarParaMilissegundos(DateTime valor)
        => new(valor.Ticks - (valor.Ticks % TimeSpan.TicksPerMillisecond), valor.Kind);

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
