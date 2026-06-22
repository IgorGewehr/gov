using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>Identificador forte de um <see cref="EventoFolha"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EventoFolhaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EventoFolhaId"/>.</returns>
    public static EventoFolhaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Evento de folha: lancamento de um provento ou desconto de um servidor numa competencia.
/// Entidade-filha de <see cref="FolhaDePagamento"/>, exposta somente atraves da raiz.
/// </summary>
public sealed class EventoFolha : Entity<EventoFolhaId>
{
    private EventoFolha()
    {
    }

    private EventoFolha(
        EventoFolhaId id,
        Guid servidorId,
        Rubrica rubrica,
        TipoEvento tipo,
        BaseCalculo baseCalculo,
        decimal valor,
        RegimePrevidenciario regimePrevidenciario)
        : base(id)
    {
        ServidorId = servidorId;
        Rubrica = rubrica;
        Tipo = tipo;
        BaseCalculo = baseCalculo;
        Valor = valor;
        RegimePrevidenciario = regimePrevidenciario;
    }

    /// <summary>Servidor a que o evento pertence.</summary>
    public Guid ServidorId { get; private set; }

    /// <summary>Codigo/classificacao da verba (S-1010).</summary>
    public Rubrica Rubrica { get; private set; } = default!;

    /// <summary>Natureza do evento (provento ou desconto).</summary>
    public TipoEvento Tipo { get; private set; }

    /// <summary>Valor de incidencia (base de calculo).</summary>
    public BaseCalculo BaseCalculo { get; private set; } = default!;

    /// <summary>Valor apurado da verba.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Regime previdenciario do evento (RPPS/RGPS — herdado do servidor — I-4).</summary>
    public RegimePrevidenciario RegimePrevidenciario { get; private set; }

    /// <summary>Indica se a verba reduz a remuneracao (abate-teto/consignacao/desconto).</summary>
    public bool EhDesconto => Tipo == TipoEvento.Desconto;

    /// <summary>Lanca um novo evento de folha.</summary>
    /// <param name="servidorId">Servidor do lancamento.</param>
    /// <param name="rubrica">Rubrica (S-1010).</param>
    /// <param name="tipo">Provento ou desconto.</param>
    /// <param name="baseCalculo">Base de incidencia.</param>
    /// <param name="valor">Valor apurado (maior que zero).</param>
    /// <param name="regimePrevidenciario">Regime previdenciario herdado do servidor.</param>
    /// <returns>Novo <see cref="EventoFolha"/>.</returns>
    /// <exception cref="ArgumentNullException">Se rubrica ou base de calculo forem nulas.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for menor ou igual a zero.</exception>
    public static EventoFolha Lancar(
        Guid servidorId,
        Rubrica rubrica,
        TipoEvento tipo,
        BaseCalculo baseCalculo,
        decimal valor,
        RegimePrevidenciario regimePrevidenciario)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(baseCalculo);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        return new EventoFolha(
            EventoFolhaId.New(),
            servidorId,
            rubrica,
            tipo,
            baseCalculo,
            decimal.Round(valor, 2, MidpointRounding.AwayFromZero),
            regimePrevidenciario);
    }
}
