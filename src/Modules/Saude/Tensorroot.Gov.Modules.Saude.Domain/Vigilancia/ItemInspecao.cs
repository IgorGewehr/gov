using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Item do roteiro/checklist de uma <see cref="Inspecao"/>: um requisito sanitario verificado e sua
/// conformidade. Entidade filha (owned) da inspecao — nasce e morre com a raiz. Itens nao conformes
/// viram pendencias e podem fundamentar o auto. Imutavel apos a conclusao da inspecao (controlado na raiz).
/// </summary>
public sealed class ItemInspecao : Entity<ItemInspecaoId>
{
    private ItemInspecao()
    {
    }

    private ItemInspecao(ItemInspecaoId id, string requisito, ConformidadeItem conformidade, string? observacao)
        : base(id)
    {
        Requisito = requisito;
        Conformidade = conformidade;
        Observacao = observacao;
    }

    /// <summary>Requisito sanitario verificado (ex.: "Higienizacao de superficies").</summary>
    public string Requisito { get; private set; } = string.Empty;

    /// <summary>Conformidade aferida do requisito.</summary>
    public ConformidadeItem Conformidade { get; private set; }

    /// <summary>Observacao do fiscal (obrigatoria quando nao conforme — descreve a pendencia).</summary>
    public string? Observacao { get; private set; }

    /// <summary>Cria um item do roteiro. Exige observacao quando nao conforme (descreve a pendencia).</summary>
    /// <param name="requisito">Requisito sanitario.</param>
    /// <param name="conformidade">Conformidade aferida.</param>
    /// <param name="observacao">Observacao (obrigatoria se nao conforme).</param>
    /// <returns>Novo item de inspecao.</returns>
    /// <exception cref="ArgumentException">Se o requisito for vazio ou faltar observacao em item nao conforme.</exception>
    public static ItemInspecao Criar(string requisito, ConformidadeItem conformidade, string? observacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requisito);
        if (conformidade == ConformidadeItem.NaoConforme && string.IsNullOrWhiteSpace(observacao))
        {
            throw new ArgumentException("Item nao conforme exige observacao descrevendo a pendencia.", nameof(observacao));
        }

        return new ItemInspecao(ItemInspecaoId.New(), requisito.Trim(), conformidade, observacao?.Trim());
    }

    /// <summary>Indica se o item representa uma pendencia (nao conforme).</summary>
    /// <returns><c>true</c> se nao conforme.</returns>
    public bool EhPendencia() => Conformidade == ConformidadeItem.NaoConforme;
}
