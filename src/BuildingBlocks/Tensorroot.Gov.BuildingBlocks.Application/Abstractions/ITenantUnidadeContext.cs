namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Fornece o ESCOPO DE UNIDADE ORGANIZACIONAL (UO) da requisicao atual — o conjunto de UOs que o
/// sujeito pode LER, derivado das suas atribuicoes de papel (escopo efetivo). Espelha o
/// <see cref="ITenantContext"/>: assim como o filtro de tenant restringe por <c>TenantId</c>, o
/// filtro de UO (MODELO §5) restringe leituras a estas UOs, aplicado por reflexao sobre
/// <c>IMustHaveUnidade</c> no <c>ModuleDbContext</c>.
/// </summary>
/// <remarks>
/// Igual a logica do <c>TenantOverride</c> para jobs/sem-usuario: quando
/// <see cref="DeveFiltrarPorUnidade"/> e <c>false</c> (sem sujeito resolvido — Workers,
/// provisionamento, Outbox), o filtro NAO e aplicado. Entidades que NAO implementam
/// <c>IMustHaveUnidade</c> jamais sao filtradas (compatibilidade com os modulos atuais).
/// </remarks>
public interface ITenantUnidadeContext
{
    /// <summary>
    /// Indica se o filtro de UO deve ser aplicado nesta requisicao. <c>false</c> em execucoes sem
    /// sujeito (jobs/sistema) — nao filtra, igual ao comportamento de override de tenant.
    /// </summary>
    bool DeveFiltrarPorUnidade { get; }

    /// <summary>
    /// Conjunto de UOs (ids) que o sujeito pode ler. Avaliado por consulta. Vazio com
    /// <see cref="DeveFiltrarPorUnidade"/> verdadeiro = nega tudo (deny-by-default, invariante I2).
    /// </summary>
    IReadOnlyCollection<Guid> UnidadesPermitidas { get; }
}
