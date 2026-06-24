using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>
/// Base de calculo do art. 29-A: somatorio da RECEITA TRIBUTARIA e das TRANSFERENCIAS efetivamente
/// REALIZADAS no EXERCICIO ANTERIOR (CF/88 art. 29-A, caput). VO imutavel que guarda as duas parcelas
/// discriminadas (para o demonstrativo) e o exercicio de referencia da arrecadacao.
/// <para>
/// IMPORTANTE: o art. 29-A toma a receita do <b>exercicio anterior</b> ao do orcamento da Camara. Por
/// isso o VO carrega o <see cref="ExercicioReferencia"/> (o ano da arrecadacao); a apuracao registra
/// separadamente o exercicio orcamentario sob teto.
/// </para>
/// </summary>
public sealed class ReceitaBaseArt29A : ValueObject
{
    private ReceitaBaseArt29A(int exercicioReferencia, decimal receitaTributaria, decimal transferencias)
    {
        ExercicioReferencia = exercicioReferencia;
        ReceitaTributaria = receitaTributaria;
        Transferencias = transferencias;
    }

    /// <summary>Exercicio (ano) da arrecadacao que serve de base (exercicio anterior ao orcamento sob teto).</summary>
    public int ExercicioReferencia { get; }

    /// <summary>Receita tributaria realizada no exercicio de referencia (impostos, taxas, contribuicoes).</summary>
    public decimal ReceitaTributaria { get; }

    /// <summary>Transferencias constitucionais/legais realizadas no exercicio de referencia (FPM, ICMS, etc.).</summary>
    public decimal Transferencias { get; }

    /// <summary>Base total = receita tributaria + transferencias.</summary>
    public decimal Total => ReceitaTributaria + Transferencias;

    /// <summary>
    /// Cria a base de calculo validada (parcelas nao negativas e total positivo).
    /// </summary>
    /// <param name="exercicioReferencia">Exercicio da arrecadacao (positivo).</param>
    /// <param name="receitaTributaria">Receita tributaria realizada (&gt;= 0).</param>
    /// <param name="transferencias">Transferencias realizadas (&gt;= 0).</param>
    /// <returns>Base de calculo do art. 29-A.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se parcelas forem negativas, total nao positivo ou exercicio invalido.</exception>
    public static ReceitaBaseArt29A De(int exercicioReferencia, decimal receitaTributaria, decimal transferencias)
    {
        if (exercicioReferencia <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exercicioReferencia), "Exercicio de referencia deve ser positivo.");
        }

        if (receitaTributaria < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(receitaTributaria), "Receita tributaria nao pode ser negativa.");
        }

        if (transferencias < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(transferencias), "Transferencias nao podem ser negativas.");
        }

        if (receitaTributaria + transferencias <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(receitaTributaria), "A base (receita + transferencias) deve ser positiva.");
        }

        return new ReceitaBaseArt29A(exercicioReferencia, receitaTributaria, transferencias);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExercicioReferencia;
        yield return ReceitaTributaria;
        yield return Transferencias;
    }
}
