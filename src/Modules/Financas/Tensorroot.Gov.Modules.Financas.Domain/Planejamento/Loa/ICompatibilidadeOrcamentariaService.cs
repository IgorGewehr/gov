namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>
/// Resultado da checagem de compatibilidade LOA ⊆ LDO ⊆ PPA + equilíbrio (Lei 4.320/64 art. 2º;
/// CF 167, I e §1º; CF 165 §2º). Quando incompatível, lista os motivos para correção.
/// </summary>
/// <param name="Compativel">Indica se a LOA é compatível.</param>
/// <param name="Motivos">Motivos de incompatibilidade (vazio se compatível).</param>
public sealed record ResultadoCompatibilidade(bool Compativel, IReadOnlyList<string> Motivos)
{
    /// <summary>Resultado compatível (sem motivos).</summary>
    public static ResultadoCompatibilidade Ok() => new(true, []);

    /// <summary>Resultado incompatível com os motivos informados.</summary>
    /// <param name="motivos">Motivos detectados.</param>
    /// <returns>Resultado incompatível.</returns>
    public static ResultadoCompatibilidade Incompativel(IReadOnlyList<string> motivos) => new(false, motivos);
}

/// <summary>
/// Serviço de domínio que valida a cadeia de compatibilidade do orçamento. Implementado na
/// camada de Application (acessa os repositórios de PPA/LDO/LOA do próprio módulo).
/// </summary>
public interface ICompatibilidadeOrcamentariaService
{
    /// <summary>Verifica a compatibilidade da LOA com a LDO e o PPA vinculados, e o equilíbrio.</summary>
    /// <param name="loa">LOA a validar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da checagem.</returns>
    Task<ResultadoCompatibilidade> VerificarAsync(LeiOrcamentariaAnual loa, CancellationToken cancellationToken);
}
