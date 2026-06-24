using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Prorrogacao de vigencia: nova data-fim, justificativa e marca de prorrogacao DE OFICIO (quando a
/// Administracao deu causa ao atraso na liberacao de recursos — Dec. 11.531/2023; Lei 13.019 art. 55).
/// </summary>
/// <param name="NovaFim">Nova data-fim apos a prorrogacao.</param>
/// <param name="Justificativa">Motivacao do ato (obrigatoria).</param>
/// <param name="DeOficio">Verdadeiro quando a prorrogacao e de oficio (atraso imputavel a Administracao).</param>
/// <param name="DataRegistro">Data do registro da prorrogacao.</param>
public readonly record struct ProrrogacaoVigencia(
    DateOnly NovaFim,
    string Justificativa,
    bool DeOficio,
    DateOnly DataRegistro);

/// <summary>
/// Vigencia do instrumento (convenio ou parceria): periodo <c>[Inicio, Fim]</c> com historico de
/// prorrogacoes. Imutavel — cada prorrogacao produz uma NOVA <see cref="Vigencia"/> (o agregado substitui a
/// referencia). A data-fim corrente e sempre a da ultima prorrogacao (ou <see cref="Inicio"/>..<see cref="FimOriginal"/>).
/// </summary>
public sealed class Vigencia : ValueObject
{
    private readonly List<ProrrogacaoVigencia> _prorrogacoes;

    private Vigencia(DateOnly inicio, DateOnly fimOriginal, IReadOnlyList<ProrrogacaoVigencia> prorrogacoes)
    {
        Inicio = inicio;
        FimOriginal = fimOriginal;
        _prorrogacoes = [.. prorrogacoes];
    }

    /// <summary>Inicio da vigencia.</summary>
    public DateOnly Inicio { get; }

    /// <summary>Fim originalmente pactuado (nao muda com prorrogacao — fica para auditoria).</summary>
    public DateOnly FimOriginal { get; }

    /// <summary>Prorrogacoes aplicadas, em ordem cronologica.</summary>
    public IReadOnlyList<ProrrogacaoVigencia> Prorrogacoes => _prorrogacoes;

    /// <summary>Data-fim vigente (a da ultima prorrogacao, ou <see cref="FimOriginal"/> se nao houver).</summary>
    public DateOnly Fim => _prorrogacoes.Count > 0 ? _prorrogacoes[^1].NovaFim : FimOriginal;

    /// <summary>
    /// Cria uma vigencia com <c>Fim &gt;= Inicio</c>.
    /// </summary>
    /// <param name="inicio">Inicio da vigencia.</param>
    /// <param name="fim">Fim pactuado.</param>
    /// <returns>Nova <see cref="Vigencia"/>.</returns>
    /// <exception cref="ArgumentException">Se <paramref name="fim"/> for anterior a <paramref name="inicio"/>.</exception>
    public static Vigencia Criar(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
        {
            throw new ArgumentException("Fim da vigencia nao pode ser anterior ao inicio.", nameof(fim));
        }

        return new Vigencia(inicio, fim, []);
    }

    /// <summary>Reidrata a vigencia a partir de valores persistidos (sem revalidar).</summary>
    /// <param name="inicio">Inicio original.</param>
    /// <param name="fimOriginal">Fim originalmente pactuado.</param>
    /// <param name="prorrogacoes">Historico de prorrogacoes.</param>
    /// <returns>Vigencia reconstruida.</returns>
    public static Vigencia Reidratar(DateOnly inicio, DateOnly fimOriginal, IReadOnlyList<ProrrogacaoVigencia> prorrogacoes)
        => new(inicio, fimOriginal, prorrogacoes ?? []);

    /// <summary>
    /// Prorroga a vigencia para <paramref name="novaFim"/> (deve ser posterior ao <see cref="Fim"/> atual).
    /// Devolve uma NOVA vigencia (imutabilidade).
    /// </summary>
    /// <param name="novaFim">Nova data-fim (posterior ao fim atual).</param>
    /// <param name="justificativa">Motivacao do ato (obrigatoria).</param>
    /// <param name="deOficio">Indica prorrogacao de oficio (atraso imputavel a Administracao).</param>
    /// <param name="dataRegistro">Data do registro.</param>
    /// <returns>Nova <see cref="Vigencia"/> com a prorrogacao acrescentada.</returns>
    /// <exception cref="ArgumentException">Se a justificativa for vazia ou a nova data nao for posterior ao fim atual.</exception>
    public Vigencia Prorrogar(DateOnly novaFim, string justificativa, bool deOficio, DateOnly dataRegistro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);
        if (novaFim <= Fim)
        {
            throw new ArgumentException("A prorrogacao exige nova data-fim posterior ao fim vigente.", nameof(novaFim));
        }

        var prorrogacoes = new List<ProrrogacaoVigencia>(_prorrogacoes)
        {
            new(novaFim, justificativa.Trim(), deOficio, dataRegistro),
        };
        return new Vigencia(Inicio, FimOriginal, prorrogacoes);
    }

    /// <summary>Verdadeiro se a vigencia ja se encerrou em <paramref name="hoje"/> (<c>hoje &gt; Fim</c>).</summary>
    /// <param name="hoje">Data de referencia (relogio externo).</param>
    /// <returns><c>true</c> se encerrada.</returns>
    public bool Encerrada(DateOnly hoje) => hoje > Fim;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return FimOriginal;
        foreach (var prorrogacao in _prorrogacoes)
        {
            yield return prorrogacao;
        }
    }
}
