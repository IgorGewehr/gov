using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

/// <summary>Identificador forte de uma <see cref="DivergenciaInventario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DivergenciaInventarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DivergenciaInventarioId"/>.</returns>
    public static DivergenciaInventarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Divergência apurada na conciliação físico × contábil (entidade-filha de <see cref="Inventario"/>):
/// fonte de auditoria do achado (falta/sobra/localização/estado/valor) e da recomendação de efetivação
/// (baixa/transferência/incorporação/reavaliação) preservada para o TCE.
/// </summary>
public sealed class DivergenciaInventario : Entity<DivergenciaInventarioId>
{
    private DivergenciaInventario()
    {
    }

    private DivergenciaInventario(
        DivergenciaInventarioId id,
        TipoDivergencia tipo,
        BemPatrimonialId? bemPatrimonialId,
        string descricao,
        RecomendacaoDivergencia recomendacao)
        : base(id)
    {
        Tipo = tipo;
        BemPatrimonialId = bemPatrimonialId;
        Descricao = descricao;
        Recomendacao = recomendacao;
    }

    /// <summary>Tipo da divergência (falta/sobra/localização/estado/valor).</summary>
    public TipoDivergencia Tipo { get; private set; }

    /// <summary>Bem relacionado; nulo para sobras (achados sem tombo).</summary>
    public BemPatrimonialId? BemPatrimonialId { get; private set; }

    /// <summary>Descrição do achado (referencia tombo/localização/valor conforme o caso).</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Recomendação de efetivação downstream (não muta o bem diretamente).</summary>
    public RecomendacaoDivergencia Recomendacao { get; private set; }

    /// <summary>Registra uma divergência apurada na conciliação.</summary>
    /// <param name="tipo">Tipo da divergência.</param>
    /// <param name="bemPatrimonialId">Bem relacionado (nulo para sobra).</param>
    /// <param name="descricao">Descrição do achado.</param>
    /// <param name="recomendacao">Recomendação de efetivação.</param>
    /// <returns>Nova <see cref="DivergenciaInventario"/>.</returns>
    /// <exception cref="ArgumentException">Se a descrição for vazia.</exception>
    public static DivergenciaInventario Registrar(
        TipoDivergencia tipo,
        BemPatrimonialId? bemPatrimonialId,
        string descricao,
        RecomendacaoDivergencia recomendacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        return new DivergenciaInventario(DivergenciaInventarioId.New(), tipo, bemPatrimonialId, descricao, recomendacao);
    }
}
