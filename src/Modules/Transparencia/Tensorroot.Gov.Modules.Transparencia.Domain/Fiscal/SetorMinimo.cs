namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>
/// Setor de aplicação sujeito a mínimo constitucional, ancorado na <b>função</b> de governo
/// (Portaria MOG 42/1999): a função é o discriminador primário da despesa computável.
/// <para>
/// Os códigos de função NÃO são hardcoded em regra de negócio: são o default legal da função de
/// governo e ficam parametrizáveis por tenant+vigência via <see cref="FonteRecursoVinculado"/>
/// (que mapeia <c>(funcao, fonte) → setor</c>). // TODO(validar-oficial): tabela de funções MOG 42/1999.
/// </para>
/// </summary>
public enum SetorMinimo
{
    /// <summary>Não vinculado a mínimo constitucional (não computável).</summary>
    Nenhum = 0,

    /// <summary>Saúde — função 10 (ASPS, LC 141/2012, mínimo 15%).</summary>
    Saude = 10,

    /// <summary>Educação — função 12 (MDE, CF art. 212, mínimo 25%).</summary>
    Educacao = 12,
}
