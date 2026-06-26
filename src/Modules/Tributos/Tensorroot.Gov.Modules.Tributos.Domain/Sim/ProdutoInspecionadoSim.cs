using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Sim;

/// <summary>Identificador forte da entidade <see cref="ProdutoInspecionadoSim"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProdutoInspecionadoSimId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProdutoInspecionadoSimId"/>.</returns>
    public static ProdutoInspecionadoSimId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Produto inspecionado habilitado sob um título do S.I.M.: denominação de venda, classificação sanitária
/// e número do rótulo aprovado (controle de rotulagem — Decreto 9.013/2017). Entidade-filha de
/// <see cref="TituloRegistroSim"/>.
/// </summary>
public sealed class ProdutoInspecionadoSim : Entity<ProdutoInspecionadoSimId>
{
    private ProdutoInspecionadoSim()
    {
    }

    private ProdutoInspecionadoSim(
        ProdutoInspecionadoSimId id,
        TituloRegistroSimId tituloId,
        string denominacao,
        string classificacao,
        string numeroRotulo)
        : base(id)
    {
        TituloRegistroSimId = tituloId;
        Denominacao = denominacao;
        Classificacao = classificacao;
        NumeroRotulo = numeroRotulo;
    }

    /// <summary>Título ao qual o produto pertence.</summary>
    public TituloRegistroSimId TituloRegistroSimId { get; private set; }

    /// <summary>Denominação de venda do produto (ex.: "Queijo Colonial").</summary>
    public string Denominacao { get; private set; } = default!;

    /// <summary>Classificação sanitária do produto (categoria).</summary>
    public string Classificacao { get; private set; } = default!;

    /// <summary>Número do rótulo aprovado (controle de rotulagem).</summary>
    public string NumeroRotulo { get; private set; } = default!;

    /// <summary>Cria um produto inspecionado habilitado.</summary>
    /// <param name="tituloId">Título proprietário.</param>
    /// <param name="denominacao">Denominação de venda.</param>
    /// <param name="classificacao">Classificação sanitária.</param>
    /// <param name="numeroRotulo">Número do rótulo aprovado.</param>
    /// <returns>Novo <see cref="ProdutoInspecionadoSim"/>.</returns>
    public static ProdutoInspecionadoSim Criar(
        TituloRegistroSimId tituloId,
        string denominacao,
        string classificacao,
        string numeroRotulo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(denominacao);
        ArgumentException.ThrowIfNullOrWhiteSpace(classificacao);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroRotulo);

        return new ProdutoInspecionadoSim(
            ProdutoInspecionadoSimId.New(),
            tituloId,
            denominacao.Trim(),
            classificacao.Trim(),
            numeroRotulo.Trim());
    }
}
