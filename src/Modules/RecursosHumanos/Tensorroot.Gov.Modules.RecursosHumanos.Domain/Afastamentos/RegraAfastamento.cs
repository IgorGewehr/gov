using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;

/// <summary>Identificador forte do agregado <see cref="RegraAfastamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegraAfastamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegraAfastamentoId"/>.</returns>
    public static RegraAfastamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// REGRA legal de um <see cref="TipoAfastamento"/>, parametrizada por tenant e por vigencia (nunca
/// <em>hardcoded</em> — CLAUDE.md S7/S16): define QUEM PAGA / quanto do provento o ente mantem
/// (<see cref="PercentualRemuneracao"/>), se SUSPENDE proventos, por quantos dias o ENTE ainda paga
/// (<see cref="DiasPagosPeloEnte"/> — caso doenca: 15d), se CONTA TEMPO de servico, a duracao legal
/// padrao (sugestao de fim previsto) e o codigo do evento eSocial. O usuario escolhe o TIPO; o efeito
/// na folha vem desta regra (versionada por <see cref="VigenciaInicio"/>). Raiz de agregado.
/// </summary>
public sealed class RegraAfastamento : AggregateRoot<RegraAfastamentoId>, IMustHaveTenant
{
    /// <summary>Codigo eSocial padrao do afastamento temporario (evento S-2230).</summary>
    public const string CodigoEventoESocialPadrao = "S-2230";

    private RegraAfastamento()
    {
    }

    private RegraAfastamento(
        RegraAfastamentoId id,
        Guid tenantId,
        TipoAfastamento tipo,
        Competencia vigenciaInicio,
        bool suspendeProventos,
        decimal percentualRemuneracao,
        int diasPagosPeloEnte,
        bool contaTempo,
        int? duracaoPadraoDias,
        string codigoEventoESocial)
        : base(id)
    {
        TenantId = tenantId;
        Tipo = tipo;
        VigenciaInicio = vigenciaInicio;
        SuspendeProventos = suspendeProventos;
        PercentualRemuneracao = percentualRemuneracao;
        DiasPagosPeloEnte = diasPagosPeloEnte;
        ContaTempo = contaTempo;
        DuracaoPadraoDias = duracaoPadraoDias;
        CodigoEventoESocial = codigoEventoESocial;
    }

    /// <summary>Tenant (ente publico) dono da regra.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Tipo de afastamento ao qual a regra se aplica.</summary>
    public TipoAfastamento Tipo { get; private set; }

    /// <summary>Competencia a partir da qual a regra vigora (versionamento por vigencia).</summary>
    public Competencia VigenciaInicio { get; private set; } = default!;

    /// <summary>Se <c>true</c>, o ente NAO paga o provento durante o afastamento (apos os dias pagos pelo ente).</summary>
    public bool SuspendeProventos { get; private set; }

    /// <summary>Percentual (0..100) do provento-base que o ente MANTEM enquanto paga (100 = integral).</summary>
    public decimal PercentualRemuneracao { get; private set; }

    /// <summary>Dias iniciais pagos pelo ENTE antes de o beneficio passar ao INSS (ex.: doenca = 15d). Zero = nao se aplica.</summary>
    public int DiasPagosPeloEnte { get; private set; }

    /// <summary>Se <c>true</c>, o periodo conta como tempo de servico (estabilidade/aposentadoria).</summary>
    public bool ContaTempo { get; private set; }

    /// <summary>Duracao legal padrao em dias (sugestao de fim previsto); nula quando indeterminada.</summary>
    public int? DuracaoPadraoDias { get; private set; }

    /// <summary>Codigo do evento eSocial associado (S-2230, S-2231, etc.).</summary>
    public string CodigoEventoESocial { get; private set; } = CodigoEventoESocialPadrao;

    /// <summary>
    /// Cria/define uma regra de afastamento de um tipo, vigente a partir de uma competencia, para o tenant.
    /// </summary>
    /// <param name="tenantId">Tenant dono da regra.</param>
    /// <param name="tipo">Tipo de afastamento.</param>
    /// <param name="vigenciaInicio">Competencia inicial de vigencia.</param>
    /// <param name="suspendeProventos">Se o ente suspende o provento (apos os dias pagos pelo ente).</param>
    /// <param name="percentualRemuneracao">Percentual (0..100) do provento mantido pelo ente.</param>
    /// <param name="diasPagosPeloEnte">Dias iniciais pagos pelo ente (>= 0).</param>
    /// <param name="contaTempo">Se conta tempo de servico.</param>
    /// <param name="duracaoPadraoDias">Duracao legal padrao em dias (opcional, > 0 quando informado).</param>
    /// <param name="codigoEventoESocial">Codigo eSocial (default S-2230).</param>
    /// <returns>Nova <see cref="RegraAfastamento"/> valida.</returns>
    /// <exception cref="ArgumentNullException">Se a competencia for nula.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o percentual estiver fora de 0..100, dias negativos ou duracao nao-positiva.</exception>
    public static RegraAfastamento Definir(
        Guid tenantId,
        TipoAfastamento tipo,
        Competencia vigenciaInicio,
        bool suspendeProventos,
        decimal percentualRemuneracao,
        int diasPagosPeloEnte,
        bool contaTempo,
        int? duracaoPadraoDias,
        string? codigoEventoESocial = null)
    {
        ArgumentNullException.ThrowIfNull(vigenciaInicio);
        if (percentualRemuneracao is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualRemuneracao), "Percentual de remuneracao deve estar entre 0 e 100.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(diasPagosPeloEnte);
        if (duracaoPadraoDias is { } dur)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dur);
        }

        var codigo = string.IsNullOrWhiteSpace(codigoEventoESocial) ? CodigoEventoESocialPadrao : codigoEventoESocial.Trim();
        return new RegraAfastamento(
            RegraAfastamentoId.New(),
            tenantId,
            tipo,
            vigenciaInicio,
            suspendeProventos,
            percentualRemuneracao,
            diasPagosPeloEnte,
            contaTempo,
            duracaoPadraoDias,
            codigo);
    }
}
