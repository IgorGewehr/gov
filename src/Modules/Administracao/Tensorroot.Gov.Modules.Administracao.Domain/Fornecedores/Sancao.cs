using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

/// <summary>Identificador forte de uma <see cref="Sancao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct SancaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="SancaoId"/>.</returns>
    public static SancaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Sancao administrativa aplicada a um fornecedor (Lei 14.133/2021, art. 156), sob devido
/// processo legal: exige processo administrativo e fundamentacao. Entidade-filha do agregado
/// <see cref="Fornecedor"/>.
/// </summary>
public sealed class Sancao : Entity<SancaoId>
{
    private Sancao()
    {
    }

    private Sancao(
        SancaoId id,
        TipoSancao tipo,
        DateOnly dataInicio,
        DateOnly? dataFim,
        string processoAdministrativo,
        string fundamentacao,
        ValorMonetario? valorMulta)
        : base(id)
    {
        Tipo = tipo;
        DataInicio = dataInicio;
        DataFim = dataFim;
        ProcessoAdministrativo = processoAdministrativo;
        Fundamentacao = fundamentacao;
        ValorMulta = valorMulta;
    }

    /// <summary>Tipo da sancao aplicada.</summary>
    public TipoSancao Tipo { get; private set; }

    /// <summary>Inicio da vigencia da sancao.</summary>
    public DateOnly DataInicio { get; private set; }

    /// <summary>Termo final da vigencia; nulo = sem termo final definido (vigente indefinidamente).</summary>
    public DateOnly? DataFim { get; private set; }

    /// <summary>Processo administrativo que embasa a sancao (devido processo legal).</summary>
    public string ProcessoAdministrativo { get; private set; } = default!;

    /// <summary>Fundamentacao/motivacao do ato sancionatorio.</summary>
    public string Fundamentacao { get; private set; } = default!;

    /// <summary>Valor da multa, quando o tipo for <see cref="TipoSancao.Multa"/>; caso contrario, nulo.</summary>
    public ValorMonetario? ValorMulta { get; private set; }

    /// <summary>Indica se o tipo desta sancao e impeditivo (impedimento ou inidoneidade).</summary>
    public bool EImpeditiva => Tipo is TipoSancao.Impedimento or TipoSancao.Inidoneidade;

    /// <summary>Avalia se a sancao esta vigente na data informada (DataFim inclusivo; nulo = sem termo).</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se <c>DataInicio &lt;= hoje &amp;&amp; (DataFim == null || hoje &lt;= DataFim)</c>.</returns>
    public bool EstaVigente(DateOnly hoje)
        => DataInicio <= hoje && (DataFim is null || hoje <= DataFim.Value);

    /// <summary>Registra uma sancao administrativa, validando os campos do devido processo legal.</summary>
    /// <param name="tipo">Tipo da sancao.</param>
    /// <param name="dataInicio">Inicio da vigencia.</param>
    /// <param name="dataFim">Termo final (opcional).</param>
    /// <param name="processoAdministrativo">Processo administrativo (obrigatorio).</param>
    /// <param name="fundamentacao">Fundamentacao do ato (obrigatorio).</param>
    /// <param name="valorMulta">Valor da multa, exigido (positivo) somente quando <see cref="TipoSancao.Multa"/>.</param>
    /// <returns>Nova <see cref="Sancao"/>.</returns>
    /// <exception cref="ArgumentException">Se processo/fundamentacao forem vazios (I-10).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se DataFim for anterior a DataInicio, ou se a multa exigir valor positivo (I-9).</exception>
    public static Sancao Registrar(
        TipoSancao tipo,
        DateOnly dataInicio,
        DateOnly? dataFim,
        string processoAdministrativo,
        string fundamentacao,
        ValorMonetario? valorMulta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processoAdministrativo);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentacao);

        if (dataFim is not null && dataFim.Value < dataInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(dataFim), "Data fim nao pode ser anterior ao inicio.");
        }

        // I-9: Multa exige ValorMulta positivo; demais tipos nao exigem valor.
        if (tipo == TipoSancao.Multa)
        {
            if (valorMulta is null || valorMulta.Valor <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(valorMulta), "Valor da multa deve ser positivo.");
            }
        }
        else
        {
            valorMulta = null;
        }

        return new Sancao(SancaoId.New(), tipo, dataInicio, dataFim, processoAdministrativo, fundamentacao, valorMulta);
    }
}
