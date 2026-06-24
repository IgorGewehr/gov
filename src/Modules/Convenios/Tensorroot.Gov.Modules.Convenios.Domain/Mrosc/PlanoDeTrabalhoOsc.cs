using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

/// <summary>Meta do plano de trabalho da OSC, com indicador de aferição (Lei 13.019 art. 22).</summary>
/// <param name="Descricao">Descricao da meta.</param>
/// <param name="Indicador">Indicador de afericao da meta.</param>
/// <param name="ParametroEsperado">Parametro/valor esperado do indicador.</param>
public readonly record struct MetaOsc(string Descricao, string Indicador, string ParametroEsperado);

/// <summary>Parcela do cronograma de desembolso da parceria (fluxo B), com condicionantes.</summary>
/// <param name="NumeroOrdem">Numero de ordem.</param>
/// <param name="Valor">Valor previsto.</param>
/// <param name="DataPrevista">Data prevista de repasse.</param>
/// <param name="Condicionantes">Condicionantes da liberacao (ex.: "PC parcial aprovada").</param>
public readonly record struct ParcelaRepasse(int NumeroOrdem, Dinheiro Valor, DateOnly DataPrevista, string Condicionantes);

/// <summary>
/// Plano de trabalho da parceria OSC (entidade filha do agregado <see cref="ParceriaOsc"/>): metas com
/// indicadores e cronograma de desembolso (<see cref="ParcelaRepasse"/>). Pre-requisito do repasse (B-INV-4).
/// Para Acordo de Cooperacao (sem repasse) o cronograma e vazio (B-INV-2).
/// </summary>
public sealed class PlanoDeTrabalhoOsc : Entity<Guid>
{
    private readonly List<MetaOsc> _metas;
    private readonly List<ParcelaRepasse> _parcelas;

    private PlanoDeTrabalhoOsc()
    {
        _metas = [];
        _parcelas = [];
    }

    private PlanoDeTrabalhoOsc(Guid id, string objeto, Dinheiro valorGlobal, IReadOnlyList<MetaOsc> metas, IReadOnlyList<ParcelaRepasse> parcelas)
        : base(id)
    {
        Objeto = objeto;
        ValorGlobal = valorGlobal;
        _metas = [.. metas];
        _parcelas = [.. parcelas];
        Aprovado = false;
    }

    /// <summary>Objeto/finalidade da parceria.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Valor global da parceria (zero no Acordo de Cooperacao).</summary>
    public Dinheiro ValorGlobal { get; private set; } = default!;

    /// <summary>Metas com indicadores.</summary>
    public IReadOnlyList<MetaOsc> Metas => _metas;

    /// <summary>Cronograma de desembolso (parcelas de repasse).</summary>
    public IReadOnlyList<ParcelaRepasse> Parcelas => _parcelas;

    /// <summary>Aprovado pela Administracao (pre-requisito do repasse — B-INV-4).</summary>
    public bool Aprovado { get; private set; }

    /// <summary>
    /// Cria o plano da OSC. Quando <paramref name="permiteRepasse"/> e false (Acordo de Cooperacao), o
    /// cronograma DEVE ser vazio e o valor global zero (B-INV-2). Caso contrario, confere
    /// Sigma(parcelas) = valor global (tolerancia de centavos).
    /// </summary>
    /// <param name="objeto">Objeto (obrigatorio).</param>
    /// <param name="valorGlobal">Valor global.</param>
    /// <param name="metas">Metas (ao menos uma).</param>
    /// <param name="parcelas">Cronograma de desembolso.</param>
    /// <param name="permiteRepasse">Falso para Acordo de Cooperacao (sem repasse — B-INV-2).</param>
    /// <returns>Novo <see cref="PlanoDeTrabalhoOsc"/>.</returns>
    /// <exception cref="ArgumentException">Se objeto vazio, sem metas, ou as regras de repasse/somas forem violadas.</exception>
    public static PlanoDeTrabalhoOsc Criar(
        string objeto,
        Dinheiro valorGlobal,
        IReadOnlyList<MetaOsc> metas,
        IReadOnlyList<ParcelaRepasse> parcelas,
        bool permiteRepasse)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        ArgumentNullException.ThrowIfNull(valorGlobal);
        ArgumentNullException.ThrowIfNull(metas);
        ArgumentNullException.ThrowIfNull(parcelas);

        if (metas.Count == 0)
        {
            throw new ArgumentException("O plano de trabalho exige ao menos uma meta.", nameof(metas));
        }

        if (!permiteRepasse)
        {
            // B-INV-2: Acordo de Cooperacao nao tem repasse nem valor global.
            if (parcelas.Count > 0 || valorGlobal.Valor > 0m)
            {
                throw new ArgumentException("Acordo de Cooperacao nao admite cronograma de repasse nem valor global (B-INV-2).", nameof(parcelas));
            }
        }
        else
        {
            if (parcelas.Count == 0)
            {
                throw new ArgumentException("Parceria com repasse exige ao menos uma parcela de desembolso.", nameof(parcelas));
            }

            var soma = parcelas.Aggregate(Dinheiro.Zero, (acc, parcela) => acc.Somar(parcela.Valor));
            if (Math.Abs(soma.Valor - valorGlobal.Valor) > Dinheiro.ToleranciaCentavos)
            {
                throw new ArgumentException("A soma das parcelas deve igualar o valor global da parceria.", nameof(parcelas));
            }
        }

        return new PlanoDeTrabalhoOsc(Guid.NewGuid(), objeto.Trim(), valorGlobal, metas, parcelas);
    }

    /// <summary>Marca o plano como aprovado pela Administracao (B-INV-4).</summary>
    public void Aprovar() => Aprovado = true;
}
