using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Pca;

/// <summary>Identificador forte do agregado <see cref="PlanoContratacoes"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PlanoContratacoesId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PlanoContratacoesId"/>.</returns>
    public static PlanoContratacoesId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Plano de Contratacoes Anual (PCA) — consolida, por exercicio, todas as contratacoes pretendidas
/// pelo ente, para subsidiar a lei orcamentaria e a governanca das aquisicoes (art. 12, VII,
/// Lei 14.133/2021; Dec. 11.246/2022). Construido em elaboracao, aprovado pela autoridade e publicado
/// no PNCP. Um PCA por exercicio/tenant. Raiz de agregado, isolada por tenant.
/// </summary>
public sealed class PlanoContratacoes : AggregateRoot<PlanoContratacoesId>, IMustHaveTenant
{
    private readonly List<ItemPca> _itens = [];

    private PlanoContratacoes()
    {
    }

    private PlanoContratacoes(PlanoContratacoesId id, Guid tenantId, int exercicio)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Situacao = SituacaoPca.EmElaboracao;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercicio (ano) do plano.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Situacao atual no ciclo de vida.</summary>
    public SituacaoPca Situacao { get; private set; }

    /// <summary>Numero de controle do PCA no PNCP, quando publicado.</summary>
    public string? NumeroPncp { get; private set; }

    /// <summary>Numero de revisoes formais ja concluidas sobre o plano (versionamento das alteracoes).</summary>
    public int NumeroRevisao { get; private set; }

    /// <summary>Motivacao da revisao em curso (ato administrativo), quando o plano esta EmRevisao.</summary>
    public string? MotivoRevisaoAtual { get; private set; }

    /// <summary>Itens de contratacao pretendida no exercicio.</summary>
    public IReadOnlyCollection<ItemPca> Itens => _itens;

    /// <summary>Soma dos valores estimados de todos os itens do plano.</summary>
    public ValorMonetario ValorTotalEstimado
        => _itens.Aggregate(ValorMonetario.Zero, (total, item) => total.Somar(item.ValorEstimado));

    /// <summary>
    /// Abre um Plano de Contratacoes Anual para o exercicio (nasce <see cref="SituacaoPca.EmElaboracao"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Ano do exercicio (ex.: 2027).</param>
    /// <returns>Novo <see cref="PlanoContratacoes"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o exercicio nao for um ano plausivel.</exception>
    public static PlanoContratacoes Abrir(Guid tenantId, int exercicio)
    {
        if (exercicio is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(exercicio), "Exercicio fora do intervalo plausivel (2000-2100).");
        }

        return new PlanoContratacoes(PlanoContratacoesId.New(), tenantId, exercicio);
    }

    /// <summary>
    /// Inclui um item de contratacao pretendida (apenas com o plano em elaboracao).
    /// </summary>
    /// <param name="itemCatalogoId">Item de catalogo a contratar.</param>
    /// <param name="quantidade">Quantidade pretendida.</param>
    /// <param name="valorEstimado">Valor estimado total.</param>
    /// <param name="trimestreDesejado">Trimestre desejado (1 a 4).</param>
    /// <param name="justificativa">Justificativa (opcional).</param>
    /// <returns>Identificador do item incluido.</returns>
    /// <exception cref="InvalidOperationException">Se o plano nao estiver EmElaboracao ou o item ja constar.</exception>
    public ItemPcaId IncluirItem(
        ItemCatalogoId itemCatalogoId,
        decimal quantidade,
        ValorMonetario valorEstimado,
        int trimestreDesejado,
        string? justificativa)
    {
        GarantirEditavel();
        if (_itens.Any(i => i.ItemCatalogoId == itemCatalogoId))
        {
            throw new InvalidOperationException("Item de catalogo ja consta no plano; ajuste o item existente.");
        }

        var item = ItemPca.Criar(itemCatalogoId, quantidade, valorEstimado, trimestreDesejado, justificativa);
        _itens.Add(item);
        return item.Id;
    }

    /// <summary>Remove um item do plano em elaboracao.</summary>
    /// <param name="itemPcaId">Item a remover.</param>
    /// <exception cref="InvalidOperationException">Se o plano nao estiver EmElaboracao ou o item nao existir.</exception>
    public void RemoverItem(ItemPcaId itemPcaId)
    {
        GarantirEditavel();
        var item = _itens.FirstOrDefault(i => i.Id == itemPcaId)
            ?? throw new InvalidOperationException("Item nao pertence a este plano.");
        if (item.FoiContratado)
        {
            throw new InvalidOperationException("Item ja vinculado a uma contratacao nao pode ser removido do plano (rastreabilidade).");
        }

        _itens.Remove(item);
    }

    /// <summary>
    /// Vincula um item do plano a contratacao que o concretizou (licitacao/ata/dispensa/inexigibilidade),
    /// materializando a rastreabilidade planejamento -> execucao (art. 12, VII; Dec. 11.246/2022). So apos
    /// o plano estar vigente (Aprovado/Publicado/EmRevisao): nao se vincula execucao a rascunho.
    /// </summary>
    /// <param name="itemPcaId">Item planejado a concretizar.</param>
    /// <param name="contratacao">Vinculo da contratacao gerada.</param>
    /// <exception cref="InvalidOperationException">Plano em rascunho ou item inexistente.</exception>
    public void VincularContratacaoItem(ItemPcaId itemPcaId, ContratacaoVinculada contratacao)
    {
        if (Situacao == SituacaoPca.EmElaboracao)
        {
            throw new InvalidOperationException("So e possivel vincular contratacao a item de plano ja aprovado/vigente.");
        }

        var item = _itens.FirstOrDefault(i => i.Id == itemPcaId)
            ?? throw new InvalidOperationException("Item nao pertence a este plano.");
        item.VincularContratacao(contratacao);
    }

    /// <summary>Desvincula a contratacao de um item (correcao de erro de vinculacao).</summary>
    /// <param name="itemPcaId">Item a desvincular.</param>
    /// <exception cref="InvalidOperationException">Item inexistente.</exception>
    public void DesvincularContratacaoItem(ItemPcaId itemPcaId)
    {
        var item = _itens.FirstOrDefault(i => i.Id == itemPcaId)
            ?? throw new InvalidOperationException("Item nao pertence a este plano.");
        item.DesvincularContratacao();
    }

    /// <summary>
    /// Aprova o plano pela autoridade competente, congelando os itens. Exige ao menos um item.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o plano nao estiver EmElaboracao ou estiver vazio.</exception>
    public void Aprovar()
    {
        if (Situacao is not (SituacaoPca.EmElaboracao or SituacaoPca.EmRevisao))
        {
            throw new InvalidOperationException($"A aprovacao exige plano EmElaboracao ou EmRevisao. Situacao atual: {Situacao}.");
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("Plano sem itens nao pode ser aprovado.");
        }

        // Concluir uma revisao incrementa a versao e limpa a motivacao corrente (versionamento das alteracoes).
        if (Situacao == SituacaoPca.EmRevisao)
        {
            NumeroRevisao++;
            MotivoRevisaoAtual = null;
        }

        Situacao = SituacaoPca.Aprovado;
    }

    /// <summary>
    /// Reabre o plano vigente (Aprovado/Publicado) para REVISAO FORMAL (inclusao/exclusao/ajuste de itens),
    /// passando a <c>EmRevisao</c> com a motivacao registrada. Concluida a revisao, chama-se <see cref="Aprovar"/>
    /// (incrementando <see cref="NumeroRevisao"/>) e, se necessario, republica-se no PNCP.
    /// </summary>
    /// <param name="motivo">Motivacao do ato administrativo de revisao.</param>
    /// <exception cref="ArgumentException">Motivo vazio.</exception>
    /// <exception cref="InvalidOperationException">Plano nao esta Aprovado/Publicado.</exception>
    public void Revisar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is not (SituacaoPca.Aprovado or SituacaoPca.Publicado))
        {
            throw new InvalidOperationException($"A revisao exige plano Aprovado ou Publicado. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoPca.EmRevisao;
        MotivoRevisaoAtual = motivo.Trim();
    }

    /// <summary>
    /// Marca o plano como publicado no PNCP (art. 12, par. 1º). Exige plano Aprovado.
    /// </summary>
    /// <param name="numeroPncp">Numero de controle no PNCP.</param>
    /// <exception cref="ArgumentException">Se o numero for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o plano nao estiver Aprovado.</exception>
    public void PublicarNoPncp(string numeroPncp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroPncp);
        if (Situacao != SituacaoPca.Aprovado)
        {
            throw new InvalidOperationException($"Publicacao exige plano Aprovado. Situacao atual: {Situacao}.");
        }

        NumeroPncp = numeroPncp.Trim();
        Situacao = SituacaoPca.Publicado;
    }

    private void GarantirEditavel()
    {
        // Itens sao editaveis em rascunho (EmElaboracao) ou durante uma revisao formal (EmRevisao).
        if (Situacao is not (SituacaoPca.EmElaboracao or SituacaoPca.EmRevisao))
        {
            throw new InvalidOperationException(
                $"Operacao exige plano EmElaboracao ou EmRevisao; revise o plano antes de altera-lo. Situacao atual: {Situacao}.");
        }
    }
}
