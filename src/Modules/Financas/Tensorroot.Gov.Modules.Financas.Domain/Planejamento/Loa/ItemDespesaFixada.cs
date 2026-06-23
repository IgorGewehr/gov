using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>
/// Item de despesa fixada (linha do QDD — Quadro de Detalhamento da Despesa). É o nível em que
/// a <see cref="DotacaoOrcamentaria"/> nasce: ao entrar em execução, gera UMA dotação cujo valor
/// dotado inicial = <see cref="ValorFixado"/>. Entidade-filha de <see cref="LeiOrcamentariaAnual"/>.
/// </summary>
public sealed class ItemDespesaFixada : Entity<ItemDespesaFixadaId>
{
    private ItemDespesaFixada()
    {
    }

    private ItemDespesaFixada(
        ItemDespesaFixadaId id,
        LoaId loaId,
        ClassificacaoOrcamentaria classificacao,
        AcaoPpaId acaoPpaId,
        string naturezaDespesa,
        ValorMonetario valorFixado,
        bool origemCreditoEspecial)
        : base(id)
    {
        LoaId = loaId;
        Classificacao = classificacao;
        AcaoPpaId = acaoPpaId;
        NaturezaDespesa = naturezaDespesa;
        ValorFixado = valorFixado;
        OrigemCreditoEspecial = origemCreditoEspecial;
    }

    /// <summary>LOA à qual o item pertence.</summary>
    public LoaId LoaId { get; private set; }

    /// <summary>Classificação orçamentária (órgão/UO/funcional/categoria/fonte).</summary>
    public ClassificacaoOrcamentaria Classificacao { get; private set; } = default!;

    /// <summary>Ação do PPA de origem (vínculo da cadeia LOA ⊆ PPA).</summary>
    public AcaoPpaId AcaoPpaId { get; private set; }

    /// <summary>Natureza da despesa (elemento — ex.: "3.3.90.30").</summary>
    public string NaturezaDespesa { get; private set; } = default!;

    /// <summary>Valor fixado (despesa fixada = dotação inicial).</summary>
    public ValorMonetario ValorFixado { get; private set; } = default!;

    /// <summary>Indica que o item nasceu de um crédito especial (fora da LOA original).</summary>
    public bool OrigemCreditoEspecial { get; private set; }

    /// <summary>Dotação gerada na execução (1:1), preenchida ao entrar em execução.</summary>
    public DotacaoOrcamentariaId? DotacaoId { get; private set; }

    /// <summary>Indica se a dotação de execução já foi gerada para este item (idempotência).</summary>
    public bool DotacaoGerada => DotacaoId is not null;

    /// <summary>Cria um item de despesa fixada (QDD) válido.</summary>
    /// <returns>Novo <see cref="ItemDespesaFixada"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor fixado não for positivo.</exception>
    internal static ItemDespesaFixada Criar(
        LoaId loaId,
        ClassificacaoOrcamentaria classificacao,
        AcaoPpaId acaoPpaId,
        string naturezaDespesa,
        ValorMonetario valorFixado,
        bool origemCreditoEspecial = false)
    {
        ArgumentNullException.ThrowIfNull(classificacao);
        ArgumentNullException.ThrowIfNull(valorFixado);
        ArgumentException.ThrowIfNullOrWhiteSpace(naturezaDespesa);
        if (!valorFixado.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valorFixado), "Valor fixado deve ser positivo.");
        }

        return new ItemDespesaFixada(
            ItemDespesaFixadaId.New(),
            loaId,
            classificacao,
            acaoPpaId,
            naturezaDespesa.Trim(),
            valorFixado,
            origemCreditoEspecial);
    }

    /// <summary>Registra a dotação de execução gerada para este item (fecha o elo 1:1).</summary>
    /// <param name="dotacaoId">Dotação gerada.</param>
    internal void VincularDotacao(DotacaoOrcamentariaId dotacaoId)
    {
        if (DotacaoId is not null)
        {
            return; // Idempotente: já vinculado.
        }

        DotacaoId = dotacaoId;
    }
}
