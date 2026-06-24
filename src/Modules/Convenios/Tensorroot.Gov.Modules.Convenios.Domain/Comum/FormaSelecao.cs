using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Forma de selecao da OSC (fluxo B — MROSC): chamamento publico (regra geral), dispensa (art. 30) ou
/// inexigibilidade (art. 31), MUTUAMENTE EXCLUSIVAS, sempre fundamentadas. No chamamento ha
/// <see cref="ProcessoId"/>/<see cref="Edital"/> e marca de homologacao; nas diretas ha
/// <see cref="FundamentoLegal"/> + <see cref="Justificativa"/> obrigatorios (B-INV-1).
/// </summary>
public sealed class FormaSelecao : ValueObject
{
    private FormaSelecao(
        TipoFormaSelecao tipo,
        Guid? processoId,
        string? edital,
        bool editalHomologado,
        string? fundamentoLegal,
        string? justificativa)
    {
        Tipo = tipo;
        ProcessoId = processoId;
        Edital = edital;
        EditalHomologado = editalHomologado;
        FundamentoLegal = fundamentoLegal;
        Justificativa = justificativa;
    }

    /// <summary>Tipo da selecao (Chamamento/Dispensa/Inexigibilidade).</summary>
    public TipoFormaSelecao Tipo { get; }

    /// <summary>Processo de chamamento (nulo nas diretas).</summary>
    public Guid? ProcessoId { get; }

    /// <summary>Edital do chamamento (nulo nas diretas).</summary>
    public string? Edital { get; }

    /// <summary>Verdadeiro se o edital do chamamento foi homologado (pre-requisito da celebracao no chamamento).</summary>
    public bool EditalHomologado { get; }

    /// <summary>Fundamento legal da contratacao direta (art. 30/31; nulo no chamamento).</summary>
    public string? FundamentoLegal { get; }

    /// <summary>Justificativa textual da contratacao direta (nula no chamamento).</summary>
    public string? Justificativa { get; }

    /// <summary>
    /// Cria a forma "chamamento publico" (art. 24). A homologacao do edital pode vir depois
    /// (<see cref="Homologar"/>).
    /// </summary>
    /// <param name="processoId">Processo de chamamento.</param>
    /// <param name="edital">Identificacao do edital (obrigatoria).</param>
    /// <param name="homologado">Indica se o edital ja foi homologado.</param>
    /// <returns>Forma de selecao por chamamento.</returns>
    /// <exception cref="ArgumentException">Se o edital for vazio.</exception>
    public static FormaSelecao PorChamamento(Guid processoId, string edital, bool homologado = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(edital);
        return new FormaSelecao(TipoFormaSelecao.Chamamento, processoId, edital.Trim(), homologado, null, null);
    }

    /// <summary>Cria a forma "dispensa de chamamento" (art. 30), com fundamento + justificativa.</summary>
    /// <param name="fundamentoLegal">Hipotese legal (ex.: "art. 30, I").</param>
    /// <param name="justificativa">Justificativa textual (obrigatoria).</param>
    /// <returns>Forma de selecao por dispensa.</returns>
    /// <exception cref="ArgumentException">Se fundamento ou justificativa forem vazios.</exception>
    public static FormaSelecao PorDispensa(string fundamentoLegal, string justificativa)
        => Direta(TipoFormaSelecao.Dispensa, fundamentoLegal, justificativa);

    /// <summary>Cria a forma "inexigibilidade de chamamento" (art. 31), com fundamento + justificativa.</summary>
    /// <param name="fundamentoLegal">Hipotese legal (ex.: "art. 31, caput").</param>
    /// <param name="justificativa">Justificativa textual (obrigatoria).</param>
    /// <returns>Forma de selecao por inexigibilidade.</returns>
    /// <exception cref="ArgumentException">Se fundamento ou justificativa forem vazios.</exception>
    public static FormaSelecao PorInexigibilidade(string fundamentoLegal, string justificativa)
        => Direta(TipoFormaSelecao.Inexigibilidade, fundamentoLegal, justificativa);

    private static FormaSelecao Direta(TipoFormaSelecao tipo, string fundamentoLegal, string justificativa)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);
        return new FormaSelecao(tipo, null, null, false, fundamentoLegal.Trim(), justificativa.Trim());
    }

    /// <summary>Reidrata a forma de selecao a partir de valores persistidos.</summary>
    /// <param name="tipo">Tipo da selecao.</param>
    /// <param name="processoId">Processo (chamamento).</param>
    /// <param name="edital">Edital (chamamento).</param>
    /// <param name="editalHomologado">Marca de homologacao.</param>
    /// <param name="fundamentoLegal">Fundamento (direta).</param>
    /// <param name="justificativa">Justificativa (direta).</param>
    /// <returns>Forma de selecao reconstruida.</returns>
    public static FormaSelecao Reidratar(
        TipoFormaSelecao tipo,
        Guid? processoId,
        string? edital,
        bool editalHomologado,
        string? fundamentoLegal,
        string? justificativa)
        => new(tipo, processoId, edital, editalHomologado, fundamentoLegal, justificativa);

    /// <summary>Homologa o edital do chamamento (devolve nova forma — imutabilidade).</summary>
    /// <returns>Nova forma com o edital homologado.</returns>
    /// <exception cref="InvalidOperationException">Se nao for chamamento.</exception>
    public FormaSelecao Homologar()
    {
        if (Tipo != TipoFormaSelecao.Chamamento)
        {
            throw new InvalidOperationException("Apenas o chamamento publico admite homologacao de edital.");
        }

        return new FormaSelecao(Tipo, ProcessoId, Edital, true, FundamentoLegal, Justificativa);
    }

    /// <summary>
    /// Verdadeiro se a selecao esta apta a celebracao (B-INV-1): chamamento com edital homologado, ou direta
    /// (dispensa/inexigibilidade) com fundamento + justificativa preenchidos.
    /// </summary>
    public bool AptaParaCelebrar => Tipo == TipoFormaSelecao.Chamamento
        ? EditalHomologado
        : !string.IsNullOrWhiteSpace(FundamentoLegal) && !string.IsNullOrWhiteSpace(Justificativa);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Tipo;
        yield return ProcessoId;
        yield return Edital;
        yield return EditalHomologado;
        yield return FundamentoLegal;
        yield return Justificativa;
    }
}
