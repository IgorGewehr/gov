namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Resultado da verificação da cadeia de hash da trilha de auditoria de um tenant.
/// <para>
/// <see cref="Integra"/> verdadeiro = cadeia consistente (nenhuma linha adulterada/removida desde o
/// genesis). Caso contrário, <see cref="PrimeiraDivergencia"/> aponta a 1ª linha onde a recomputação
/// falhou e <see cref="Motivo"/> classifica a falha (hash inválido, elo quebrado, lacuna na sequência).
/// </para>
/// </summary>
/// <param name="TenantId">Tenant verificado.</param>
/// <param name="Integra">Indica se a cadeia inteira é íntegra.</param>
/// <param name="LinhasVerificadas">Quantidade de linhas (na cadeia) percorridas.</param>
/// <param name="LinhasLegadasIgnoradas">Linhas com Sequencia=0 (anteriores à cadeia), fora da verificação.</param>
/// <param name="PrimeiraDivergencia">Id da 1ª linha divergente (nulo quando íntegra).</param>
/// <param name="SequenciaDivergente">Sequência esperada na 1ª divergência (nulo quando íntegra).</param>
/// <param name="Motivo">Descrição da 1ª divergência (nulo quando íntegra).</param>
public sealed record VerificacaoTrilhaResultado(
    Guid TenantId,
    bool Integra,
    long LinhasVerificadas,
    long LinhasLegadasIgnoradas,
    Guid? PrimeiraDivergencia,
    long? SequenciaDivergente,
    string? Motivo)
{
    /// <summary>Cria um resultado de cadeia ÍNTEGRA.</summary>
    public static VerificacaoTrilhaResultado Ok(Guid tenantId, long verificadas, long legadas)
        => new(tenantId, Integra: true, verificadas, legadas, null, null, null);

    /// <summary>Cria um resultado de cadeia ADULTERADA, apontando a 1ª divergência.</summary>
    public static VerificacaoTrilhaResultado Adulterada(
        Guid tenantId,
        long verificadas,
        long legadas,
        Guid linha,
        long sequencia,
        string motivo)
        => new(tenantId, Integra: false, verificadas, legadas, linha, sequencia, motivo);
}
