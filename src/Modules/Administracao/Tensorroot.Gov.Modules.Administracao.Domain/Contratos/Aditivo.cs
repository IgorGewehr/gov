using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>Identificador forte de um <see cref="Aditivo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AditivoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AditivoId"/>.</returns>
    public static AditivoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Termo aditivo do contrato — alteracao quantitativa, qualitativa, de prazo ou reequilibrio
/// (Lei 14.133/2021, art. 124 a 136). Entidade-filha do agregado <see cref="Contrato"/>.
/// </summary>
public sealed class Aditivo : Entity<AditivoId>
{
    private Aditivo()
    {
    }

    private Aditivo(
        AditivoId id,
        int numero,
        TipoAditivo tipo,
        decimal percentualSobreValorOriginal,
        ValorMonetario valorDelta,
        DateOnly? novaVigenciaFim,
        string justificativa,
        DateOnly dataCelebracao)
        : base(id)
    {
        Numero = numero;
        Tipo = tipo;
        PercentualSobreValorOriginal = percentualSobreValorOriginal;
        ValorDelta = valorDelta;
        NovaVigenciaFim = novaVigenciaFim;
        Justificativa = justificativa;
        PublicadoNoPncp = false;
        DataCelebracao = dataCelebracao;
    }

    /// <summary>Numero sequencial do aditivo no contrato.</summary>
    public int Numero { get; private set; }

    /// <summary>Tipo do aditivo.</summary>
    public TipoAditivo Tipo { get; private set; }

    /// <summary>Percentual sobre o valor original do contrato (quantitativos contam para o limite legal).</summary>
    public decimal PercentualSobreValorOriginal { get; private set; }

    /// <summary>Variacao de valor (delta) resultante do aditivo.</summary>
    public ValorMonetario ValorDelta { get; private set; } = default!;

    /// <summary>Nova data-fim de vigencia (quando o aditivo for de prazo).</summary>
    public DateOnly? NovaVigenciaFim { get; private set; }

    /// <summary>Justificativa do aditivo (motivacao do ato administrativo).</summary>
    public string Justificativa { get; private set; } = default!;

    /// <summary>Eficacia obtida pela divulgacao do aditivo no PNCP (Lei 14.133/2021, art. 94 — caput abrange contratos e seus aditamentos; o art. 174 institui o PNCP).</summary>
    public bool PublicadoNoPncp { get; private set; }

    /// <summary>Data de celebracao do termo aditivo.</summary>
    public DateOnly DataCelebracao { get; private set; }

    /// <summary>Indica se o aditivo e quantitativo (acrescimo/supressao), contando para o limite legal (I-9).</summary>
    public bool EhQuantitativo => Tipo is TipoAditivo.Acrescimo or TipoAditivo.Supressao;

    /// <summary>Registra um termo aditivo.</summary>
    /// <param name="numero">Numero sequencial.</param>
    /// <param name="tipo">Tipo do aditivo.</param>
    /// <param name="percentualSobreValorOriginal">Percentual sobre o valor original.</param>
    /// <param name="valorDelta">Variacao de valor.</param>
    /// <param name="novaVigenciaFim">Nova data-fim de vigencia (opcional).</param>
    /// <param name="justificativa">Justificativa do aditivo (obrigatoria).</param>
    /// <param name="dataCelebracao">Data de celebracao.</param>
    /// <returns>Novo <see cref="Aditivo"/>.</returns>
    /// <exception cref="ArgumentException">Se a justificativa for vazia.</exception>
    internal static Aditivo Registrar(
        int numero,
        TipoAditivo tipo,
        decimal percentualSobreValorOriginal,
        ValorMonetario valorDelta,
        DateOnly? novaVigenciaFim,
        string justificativa,
        DateOnly dataCelebracao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);
        ArgumentNullException.ThrowIfNull(valorDelta);
        return new Aditivo(
            AditivoId.New(),
            numero,
            tipo,
            percentualSobreValorOriginal,
            valorDelta,
            novaVigenciaFim,
            justificativa,
            dataCelebracao);
    }

    /// <summary>Marca o aditivo como publicado no PNCP (condicao de sua eficacia — Lei 14.133/2021, art. 94).</summary>
    internal void MarcarPublicado() => PublicadoNoPncp = true;
}
