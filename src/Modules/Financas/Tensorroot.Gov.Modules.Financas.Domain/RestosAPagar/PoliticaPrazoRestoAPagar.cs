namespace Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

/// <summary>
/// Política de prazo (decadência) para cancelamento de Restos a Pagar.
/// O número de exercícios de validade após a inscrição é <b>parametrizável por tenant</b>
/// (Decreto 93.872/86 e normas locais TCE-RS) — nunca hardcoded no domínio.
/// </summary>
public sealed record PoliticaPrazoRestoAPagar
{
    private PoliticaPrazoRestoAPagar(
        int validadeExerciciosNaoProcessado,
        int validadeExerciciosProcessado)
    {
        ValidadeExerciciosNaoProcessado = validadeExerciciosNaoProcessado;
        ValidadeExerciciosProcessado = validadeExerciciosProcessado;
    }

    /// <summary>
    /// Quantidade de exercícios de validade (após o exercício de inscrição) de um RAP
    /// Não Processado antes da decadência. Configurado pelo tenant.
    /// </summary>
    public int ValidadeExerciciosNaoProcessado { get; }

    /// <summary>
    /// Quantidade de exercícios de validade (após o exercício de inscrição) de um RAP
    /// Processado antes da decadência. Configurado pelo tenant.
    /// </summary>
    public int ValidadeExerciciosProcessado { get; }

    /// <summary>
    /// Cria a política com os prazos (em exercícios) configurados pelo tenant.
    /// </summary>
    /// <param name="validadeExerciciosNaoProcessado">Validade, em exercícios, do RAP Não Processado.</param>
    /// <param name="validadeExerciciosProcessado">Validade, em exercícios, do RAP Processado.</param>
    /// <returns>Política de prazo válida.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum prazo for negativo.</exception>
    public static PoliticaPrazoRestoAPagar De(
        int validadeExerciciosNaoProcessado,
        int validadeExerciciosProcessado)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(validadeExerciciosNaoProcessado);
        ArgumentOutOfRangeException.ThrowIfNegative(validadeExerciciosProcessado);
        return new PoliticaPrazoRestoAPagar(validadeExerciciosNaoProcessado, validadeExerciciosProcessado);
    }

    /// <summary>
    /// Último exercício em que o RAP da classificação informada permanece vigente
    /// (limite de decadência, inclusive).
    /// </summary>
    /// <param name="exercicioInscricao">Exercício de inscrição do RAP.</param>
    /// <param name="classificacao">Classificação (Processado / Não Processado).</param>
    /// <returns>Exercício-limite de vigência.</returns>
    public int ExercicioLimiteVigencia(int exercicioInscricao, ClassificacaoRestoAPagar classificacao)
    {
        var validade = classificacao == ClassificacaoRestoAPagar.Processado
            ? ValidadeExerciciosProcessado
            : ValidadeExerciciosNaoProcessado;
        return exercicioInscricao + validade;
    }
}
