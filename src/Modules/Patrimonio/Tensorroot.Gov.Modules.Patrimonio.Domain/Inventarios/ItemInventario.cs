using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

/// <summary>Identificador forte de um <see cref="ItemInventario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemInventarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemInventarioId"/>.</returns>
    public static ItemInventarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha do inventário (entidade-filha de <see cref="Inventario"/>): congela o snapshot contábil
/// esperado de um bem (descrição, localização e valor no momento da abertura — imutável, evita drift)
/// e acumula a contagem física real apurada pela comissão.
/// </summary>
public sealed class ItemInventario : Entity<ItemInventarioId>
{
    private ItemInventario()
    {
    }

    private ItemInventario(
        ItemInventarioId id,
        BemPatrimonialId bemPatrimonialId,
        string? numeroTombamento,
        string descricaoSnapshot,
        string? localizacaoEsperada,
        ValorMonetario valorContabilSnapshot)
        : base(id)
    {
        BemPatrimonialId = bemPatrimonialId;
        NumeroTombamento = numeroTombamento;
        DescricaoSnapshot = descricaoSnapshot;
        LocalizacaoEsperada = localizacaoEsperada;
        ValorContabilSnapshot = valorContabilSnapshot;
        Contado = false;
    }

    /// <summary>Bem do acervo a que esta linha se refere.</summary>
    public BemPatrimonialId BemPatrimonialId { get; private set; }

    /// <summary>Número de tombo do bem no momento do snapshot (nulo se não tombado).</summary>
    public string? NumeroTombamento { get; private set; }

    /// <summary>Descrição do bem congelada na abertura.</summary>
    public string DescricaoSnapshot { get; private set; } = default!;

    /// <summary>Localização esperada (do snapshot contábil); nula se não informada.</summary>
    public string? LocalizacaoEsperada { get; private set; }

    /// <summary>Valor contábil congelado na abertura (imutável).</summary>
    public ValorMonetario ValorContabilSnapshot { get; private set; } = default!;

    /// <summary>Situação física apurada na contagem; nula enquanto não contado.</summary>
    public SituacaoEncontrada? SituacaoEncontrada { get; private set; }

    /// <summary>Localização efetivamente encontrada na contagem (opcional).</summary>
    public string? LocalizacaoEncontrada { get; private set; }

    /// <summary>Observação livre da comissão sobre a linha.</summary>
    public string? Observacao { get; private set; }

    /// <summary>Indica se o item já foi conferido fisicamente.</summary>
    public bool Contado { get; private set; }

    /// <summary>Materializa uma linha do snapshot contábil congelado.</summary>
    /// <param name="bemPatrimonialId">Bem do acervo.</param>
    /// <param name="numeroTombamento">Número de tombo (nulo se não tombado).</param>
    /// <param name="descricaoSnapshot">Descrição congelada.</param>
    /// <param name="localizacaoEsperada">Localização esperada (opcional).</param>
    /// <param name="valorContabilSnapshot">Valor contábil congelado.</param>
    /// <returns>Nova linha de inventário.</returns>
    /// <exception cref="ArgumentException">Se a descrição for vazia.</exception>
    /// <exception cref="ArgumentNullException">Se o valor for nulo.</exception>
    public static ItemInventario DoSnapshot(
        BemPatrimonialId bemPatrimonialId,
        string? numeroTombamento,
        string descricaoSnapshot,
        string? localizacaoEsperada,
        ValorMonetario valorContabilSnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricaoSnapshot);
        ArgumentNullException.ThrowIfNull(valorContabilSnapshot);
        return new ItemInventario(
            ItemInventarioId.New(),
            bemPatrimonialId,
            numeroTombamento,
            descricaoSnapshot,
            localizacaoEsperada,
            valorContabilSnapshot);
    }

    /// <summary>Registra a contagem física do item (idempotente: reconta sobrescreve a apuração anterior).</summary>
    /// <param name="situacaoEncontrada">Situação física apurada.</param>
    /// <param name="localizacaoEncontrada">Localização encontrada (opcional).</param>
    /// <param name="observacao">Observação livre (opcional).</param>
    internal void RegistrarContagem(SituacaoEncontrada situacaoEncontrada, string? localizacaoEncontrada, string? observacao)
    {
        SituacaoEncontrada = situacaoEncontrada;
        LocalizacaoEncontrada = localizacaoEncontrada;
        Observacao = observacao;
        Contado = true;
    }
}
