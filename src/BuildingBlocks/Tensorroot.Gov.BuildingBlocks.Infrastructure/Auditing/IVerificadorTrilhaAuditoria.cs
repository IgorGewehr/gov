using Microsoft.EntityFrameworkCore;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Verifica a integridade da CADEIA DE HASH da trilha de auditoria de um tenant: recomputa, na
/// ordem de gravação, <c>HashAtual = SHA-256(HashAnterior ‖ conteúdo canônico)</c> e detecta a
/// primeira linha adulterada ou removida (lacuna de sequência / elo quebrado).
/// </summary>
public interface IVerificadorTrilhaAuditoria
{
    /// <summary>
    /// Verifica a cadeia do <paramref name="tenantId"/> sobre a fonte de linhas informada.
    /// </summary>
    /// <param name="trilha">Consulta sobre as linhas de <see cref="AuditTrail"/> (ex.: um DbSet).</param>
    /// <param name="tenantId">Tenant a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da verificação (íntegra ou 1ª divergência).</returns>
    Task<VerificacaoTrilhaResultado> VerificarAsync(
        IQueryable<AuditTrail> trilha,
        Guid tenantId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementação padrão do <see cref="IVerificadorTrilhaAuditoria"/>: lê as linhas SELADAS do tenant
/// (Sequencia &gt; 0) em ordem crescente e recomputa a cadeia a partir do genesis. É detecção apenas —
/// não altera nada — adequada a auditoria independente e ao endpoint admin.
/// </summary>
public sealed class VerificadorTrilhaAuditoria : IVerificadorTrilhaAuditoria
{
    /// <inheritdoc />
    public async Task<VerificacaoTrilhaResultado> VerificarAsync(
        IQueryable<AuditTrail> trilha,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trilha);

        // Linhas legadas (Sequencia=0) ficam fora da cadeia — contadas, não verificadas.
        var legadas = await trilha
            .Where(linha => linha.TenantId == tenantId && linha.Sequencia == 0)
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);

        var seladas = trilha
            .Where(linha => linha.TenantId == tenantId && linha.Sequencia > 0)
            .OrderBy(linha => linha.Sequencia)
            .AsAsyncEnumerable();

        long verificadas = 0;
        long sequenciaEsperada = 1;
        var hashAnteriorEsperado = AuditHashChain.HashGenesis;

        await foreach (var linha in seladas.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Lacuna ou repetição na sequência → linha removida/inserida (adulteração estrutural).
            if (linha.Sequencia != sequenciaEsperada)
            {
                return VerificacaoTrilhaResultado.Adulterada(
                    tenantId, verificadas, legadas, linha.Id, sequenciaEsperada,
                    $"Lacuna na sequência: esperada {sequenciaEsperada}, encontrada {linha.Sequencia} (linha removida ou inserida).");
            }

            // Elo quebrado: o HashAnterior gravado não bate com o selo recomputado da linha anterior.
            if (!string.Equals(linha.HashAnterior, hashAnteriorEsperado, StringComparison.Ordinal))
            {
                return VerificacaoTrilhaResultado.Adulterada(
                    tenantId, verificadas, legadas, linha.Id, linha.Sequencia,
                    "Elo quebrado: HashAnterior não corresponde ao selo da linha anterior.");
            }

            // Conteúdo adulterado: o selo recomputado não bate com o HashAtual gravado.
            var hashRecomputado = AuditHashChain.Recomputar(linha, hashAnteriorEsperado);
            if (!string.Equals(linha.HashAtual, hashRecomputado, StringComparison.Ordinal))
            {
                return VerificacaoTrilhaResultado.Adulterada(
                    tenantId, verificadas, legadas, linha.Id, linha.Sequencia,
                    "Conteúdo adulterado: HashAtual não corresponde ao conteúdo canônico recomputado.");
            }

            verificadas++;
            sequenciaEsperada++;
            hashAnteriorEsperado = linha.HashAtual!;
        }

        return VerificacaoTrilhaResultado.Ok(tenantId, verificadas, legadas);
    }
}
