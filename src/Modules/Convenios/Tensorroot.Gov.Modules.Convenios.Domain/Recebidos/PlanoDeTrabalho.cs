using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

/// <summary>Etapa/fase fisico-financeira do plano de trabalho (fluxo A).</summary>
/// <param name="Ordem">Ordem da etapa.</param>
/// <param name="Descricao">Descricao/meta da etapa.</param>
/// <param name="Valor">Valor previsto da etapa.</param>
/// <param name="InicioPrevisto">Inicio fisico-financeiro previsto.</param>
/// <param name="FimPrevisto">Fim fisico-financeiro previsto.</param>
public readonly record struct EtapaPlanoTrabalho(
    int Ordem,
    string Descricao,
    Dinheiro Valor,
    DateOnly InicioPrevisto,
    DateOnly FimPrevisto);

/// <summary>Parcela do cronograma de desembolso prevista no plano (fluxo A).</summary>
/// <param name="NumeroOrdem">Numero de ordem da parcela.</param>
/// <param name="Valor">Valor previsto da parcela.</param>
/// <param name="DataPrevista">Data prevista de liberacao.</param>
public readonly record struct ParcelaPrevista(int NumeroOrdem, Dinheiro Valor, DateOnly DataPrevista);

/// <summary>
/// Plano de trabalho do convenio recebido (entidade filha do agregado <see cref="ConvenioRecebido"/>): metas,
/// etapas fisico-financeiras e cronograma de desembolso (parcelas previstas). Carrega o <see cref="ValorGlobal"/>
/// (repasse + contrapartida) e a marca de aprovacao pelo concedente (pre-requisito da celebracao — A-INV-1).
/// </summary>
public sealed class PlanoDeTrabalho : Entity<Guid>
{
    private readonly List<EtapaPlanoTrabalho> _etapas;
    private readonly List<ParcelaPrevista> _parcelas;

    private PlanoDeTrabalho()
    {
        _etapas = [];
        _parcelas = [];
    }

    private PlanoDeTrabalho(
        Guid id,
        string objeto,
        Dinheiro valorRepasse,
        Dinheiro valorContrapartida,
        IReadOnlyList<EtapaPlanoTrabalho> etapas,
        IReadOnlyList<ParcelaPrevista> parcelas)
        : base(id)
    {
        Objeto = objeto;
        ValorRepasse = valorRepasse;
        ValorContrapartida = valorContrapartida;
        _etapas = [.. etapas];
        _parcelas = [.. parcelas];
        Aprovado = false;
    }

    /// <summary>Objeto/finalidade do plano.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Valor do repasse previsto (recurso do concedente).</summary>
    public Dinheiro ValorRepasse { get; private set; } = default!;

    /// <summary>Valor da contrapartida prevista no plano.</summary>
    public Dinheiro ValorContrapartida { get; private set; } = default!;

    /// <summary>Etapas fisico-financeiras.</summary>
    public IReadOnlyList<EtapaPlanoTrabalho> Etapas => _etapas;

    /// <summary>Cronograma de desembolso (parcelas previstas).</summary>
    public IReadOnlyList<ParcelaPrevista> Parcelas => _parcelas;

    /// <summary>Aprovado pelo concedente (pre-requisito da celebracao — A-INV-1).</summary>
    public bool Aprovado { get; private set; }

    /// <summary>Valor global do plano (repasse + contrapartida).</summary>
    public Dinheiro ValorGlobal => ValorRepasse.Somar(ValorContrapartida);

    /// <summary>
    /// Cria o plano de trabalho conferindo a coerencia das somas (A-INV-3): Sigma(parcelas) = repasse e
    /// Sigma(etapas) = valor global (com tolerancia de centavos).
    /// </summary>
    /// <param name="objeto">Objeto/finalidade (obrigatorio).</param>
    /// <param name="valorRepasse">Valor do repasse.</param>
    /// <param name="valorContrapartida">Valor da contrapartida.</param>
    /// <param name="etapas">Etapas fisico-financeiras (obrigatorias).</param>
    /// <param name="parcelas">Cronograma de desembolso (obrigatorio).</param>
    /// <returns>Novo <see cref="PlanoDeTrabalho"/>.</returns>
    /// <exception cref="ArgumentException">Se objeto vazio, listas vazias ou somas incoerentes (A-INV-3).</exception>
    public static PlanoDeTrabalho Criar(
        string objeto,
        Dinheiro valorRepasse,
        Dinheiro valorContrapartida,
        IReadOnlyList<EtapaPlanoTrabalho> etapas,
        IReadOnlyList<ParcelaPrevista> parcelas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        ArgumentNullException.ThrowIfNull(valorRepasse);
        ArgumentNullException.ThrowIfNull(valorContrapartida);
        ArgumentNullException.ThrowIfNull(etapas);
        ArgumentNullException.ThrowIfNull(parcelas);

        if (etapas.Count == 0)
        {
            throw new ArgumentException("O plano de trabalho exige ao menos uma etapa.", nameof(etapas));
        }

        if (parcelas.Count == 0)
        {
            throw new ArgumentException("O plano de trabalho exige ao menos uma parcela de desembolso.", nameof(parcelas));
        }

        // A-INV-3: Sigma das parcelas previstas = valor do repasse (tolerancia de centavos).
        var somaParcelas = parcelas.Aggregate(Dinheiro.Zero, (acc, parcela) => acc.Somar(parcela.Valor));
        if (Math.Abs(somaParcelas.Valor - valorRepasse.Valor) > Dinheiro.ToleranciaCentavos)
        {
            throw new ArgumentException(
                "A soma das parcelas previstas deve igualar o valor do repasse (A-INV-3).", nameof(parcelas));
        }

        // A-INV-3: Sigma das etapas = valor global (repasse + contrapartida).
        var somaEtapas = etapas.Aggregate(Dinheiro.Zero, (acc, etapa) => acc.Somar(etapa.Valor));
        var valorGlobal = valorRepasse.Somar(valorContrapartida);
        if (Math.Abs(somaEtapas.Valor - valorGlobal.Valor) > Dinheiro.ToleranciaCentavos)
        {
            throw new ArgumentException(
                "A soma das etapas deve igualar o valor global do plano (A-INV-3).", nameof(etapas));
        }

        return new PlanoDeTrabalho(Guid.NewGuid(), objeto.Trim(), valorRepasse, valorContrapartida, etapas, parcelas);
    }

    /// <summary>Marca o plano como aprovado pelo concedente (A-INV-1).</summary>
    public void Aprovar() => Aprovado = true;
}
