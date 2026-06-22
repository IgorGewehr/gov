using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Identificador forte do agregado <see cref="JornadaTrabalho"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct JornadaTrabalhoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="JornadaTrabalhoId"/>.</returns>
    public static JornadaTrabalhoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Jornada/escala de trabalho de um servidor: carga horaria diaria contratada (em minutos), intervalo
/// intrajornada (em minutos) e tolerancia de marcacao. E a referencia da apuracao (horas devidas,
/// extras, atrasos/faltas). Vigente a partir de uma data; uma jornada por servidor por periodo.
/// Para o estatutario a jornada e regida por lei municipal/RJU; o <see cref="RegimeJornada"/> indica
/// se a Portaria 671 (AFD/AEJ) se aplica — // TODO(validar-oficial) por tenant. Raiz de agregado.
/// </summary>
public sealed class JornadaTrabalho : AggregateRoot<JornadaTrabalhoId>, IMustHaveTenant
{
    /// <summary>Minutos em um dia (limite superior de carga/intervalo diario).</summary>
    public const int MinutosNoDia = 24 * 60;

    /// <summary>Tolerancia diaria padrao de marcacao (CLT art. 58 §1: 10 min/dia). // TODO(validar-oficial: aplicabilidade ao estatutario).</summary>
    public const int ToleranciaPadraoMinutos = 10;

    private JornadaTrabalho()
    {
    }

    private JornadaTrabalho(
        JornadaTrabalhoId id,
        Guid tenantId,
        Guid servidorId,
        int cargaDiariaMinutos,
        int intervaloMinutos,
        int toleranciaMinutos,
        RegimeJornada regime,
        DateOnly vigenciaInicio)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        CargaDiariaMinutos = cargaDiariaMinutos;
        IntervaloMinutos = intervaloMinutos;
        ToleranciaMinutos = toleranciaMinutos;
        Regime = regime;
        VigenciaInicio = vigenciaInicio;
        Ativa = true;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor a que a jornada pertence.</summary>
    public Guid ServidorId { get; private set; }

    /// <summary>Carga horaria diaria contratada, em minutos (base das horas devidas).</summary>
    public int CargaDiariaMinutos { get; private set; }

    /// <summary>Intervalo intrajornada previsto, em minutos (descanso/refeicao; nao computado como trabalho).</summary>
    public int IntervaloMinutos { get; private set; }

    /// <summary>Tolerancia diaria de marcacao, em minutos (nao gera extra nem atraso ate o limite).</summary>
    public int ToleranciaMinutos { get; private set; }

    /// <summary>Regime de jornada (estatutario/celetista) — define aplicabilidade da Portaria 671.</summary>
    public RegimeJornada Regime { get; private set; }

    /// <summary>Data inicial de vigencia da jornada.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Indica se a jornada esta ativa (false = substituida por nova vigencia).</summary>
    public bool Ativa { get; private set; }

    /// <summary>Carga semanal estimada (5 dias uteis) em minutos, para conferencia de limites legais.</summary>
    public int CargaSemanalEstimadaMinutos => CargaDiariaMinutos * 5;

    /// <summary>
    /// Define a jornada de um servidor. Carga e intervalo em minutos, dentro do dia; tolerancia nao
    /// negativa. Nasce ativa e vigente a partir de <paramref name="vigenciaInicio"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor da jornada.</param>
    /// <param name="cargaDiariaMinutos">Carga diaria contratada (minutos, 1..1440).</param>
    /// <param name="intervaloMinutos">Intervalo intrajornada (minutos, 0..1440).</param>
    /// <param name="regime">Regime de jornada (estatutario/celetista).</param>
    /// <param name="vigenciaInicio">Data inicial de vigencia.</param>
    /// <param name="toleranciaMinutos">Tolerancia diaria de marcacao (minutos; default legal).</param>
    /// <returns>Nova <see cref="JornadaTrabalho"/> ativa.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se carga/intervalo/tolerancia estiverem fora dos limites.</exception>
    public static JornadaTrabalho Definir(
        Guid tenantId,
        Guid servidorId,
        int cargaDiariaMinutos,
        int intervaloMinutos,
        RegimeJornada regime,
        DateOnly vigenciaInicio,
        int toleranciaMinutos = ToleranciaPadraoMinutos)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cargaDiariaMinutos);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cargaDiariaMinutos, MinutosNoDia);
        ArgumentOutOfRangeException.ThrowIfNegative(intervaloMinutos);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(intervaloMinutos, MinutosNoDia);
        ArgumentOutOfRangeException.ThrowIfNegative(toleranciaMinutos);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(toleranciaMinutos, MinutosNoDia);
        if (cargaDiariaMinutos + intervaloMinutos > MinutosNoDia)
        {
            throw new ArgumentOutOfRangeException(nameof(intervaloMinutos), "Carga + intervalo nao cabe em um dia.");
        }

        if (!Enum.IsDefined(regime))
        {
            throw new ArgumentOutOfRangeException(nameof(regime), "Regime de jornada invalido.");
        }

        return new JornadaTrabalho(
            JornadaTrabalhoId.New(),
            tenantId,
            servidorId,
            cargaDiariaMinutos,
            intervaloMinutos,
            toleranciaMinutos,
            regime,
            vigenciaInicio);
    }

    /// <summary>Desativa a jornada (substituicao por nova vigencia; preserva o historico).</summary>
    public void Desativar() => Ativa = false;
}
