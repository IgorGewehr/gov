using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>
/// Lei Orçamentária Anual (LOA — Lei 4.320/64 art. 2º): estima a receita e FIXA a despesa.
/// O QDD é a coleção de <see cref="ItemDespesaFixada"/> (nível em que a dotação nasce). Ao entrar
/// em execução, cada item gera UMA <see cref="DotacaoOrcamentaria"/> (dotação inicial = despesa fixada).
/// </summary>
public sealed class LeiOrcamentariaAnual : AggregateRoot<LoaId>, IMustHaveTenant
{
    private readonly List<ReceitaPrevista> _receitas = [];
    private readonly List<ItemDespesaFixada> _itens = [];

    private LeiOrcamentariaAnual()
    {
    }

    private LeiOrcamentariaAnual(
        LoaId id,
        Guid tenantId,
        int exercicio,
        LdoId ldoId,
        PpaId ppaId,
        decimal limiteSuplementacaoPercentual,
        string numeroLei,
        int anoLei)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        LdoId = ldoId;
        PpaId = ppaId;
        LimiteSuplementacaoPercentual = limiteSuplementacaoPercentual;
        NumeroLei = numeroLei;
        AnoLei = anoLei;
        Situacao = SituacaoLoa.ProjetoLei;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício da LOA.</summary>
    public int Exercicio { get; private set; }

    /// <summary>LDO vigente do exercício.</summary>
    public LdoId LdoId { get; private set; }

    /// <summary>PPA vigente do quadriênio.</summary>
    public PpaId PpaId { get; private set; }

    /// <summary>
    /// Percentual do total fixado autorizado para suplementação por decreto (art. 7º / CF 167, V).
    /// Limita a soma de créditos suplementares por decreto no <see cref="Creditos.CreditoAdicional"/>.
    /// </summary>
    public decimal LimiteSuplementacaoPercentual { get; private set; }

    /// <summary>Número da lei da LOA.</summary>
    public string NumeroLei { get; private set; } = default!;

    /// <summary>Ano da lei da LOA.</summary>
    public int AnoLei { get; private set; }

    /// <summary>Situação (máquina de estados).</summary>
    public SituacaoLoa Situacao { get; private set; }

    /// <summary>Receitas previstas.</summary>
    public IReadOnlyCollection<ReceitaPrevista> Receitas => _receitas.AsReadOnly();

    /// <summary>Itens de despesa fixada (QDD).</summary>
    public IReadOnlyCollection<ItemDespesaFixada> Itens => _itens.AsReadOnly();

    /// <summary>Total de receita prevista.</summary>
    public ValorMonetario TotalReceitaPrevista
        => _receitas.Aggregate(ValorMonetario.Zero, (acc, r) => acc.Somar(r.ValorPrevisto));

    /// <summary>Total de despesa fixada.</summary>
    public ValorMonetario TotalDespesaFixada
        => _itens.Aggregate(ValorMonetario.Zero, (acc, i) => acc.Somar(i.ValorFixado));

    /// <summary>Cria uma LOA (projeto de lei) vinculada a uma LDO e a um PPA vigentes.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício da LOA (== exercício da LDO).</param>
    /// <param name="ldo">LDO vigente do exercício.</param>
    /// <param name="limiteSuplementacaoPercentual">% do total fixado autorizado p/ suplementação por decreto.</param>
    /// <param name="numeroLei">Número/identificação da lei.</param>
    /// <param name="anoLei">Ano da lei.</param>
    /// <returns>Nova <see cref="LeiOrcamentariaAnual"/>.</returns>
    /// <exception cref="InvalidOperationException">Se a LDO não estiver vigente ou exercício divergir.</exception>
    public static LeiOrcamentariaAnual Criar(
        Guid tenantId,
        int exercicio,
        LeiDiretrizes ldo,
        decimal limiteSuplementacaoPercentual,
        string numeroLei,
        int anoLei)
    {
        ArgumentNullException.ThrowIfNull(ldo);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLei);
        ArgumentOutOfRangeException.ThrowIfNegative(limiteSuplementacaoPercentual);
        if (ldo.Situacao != SituacaoLdo.Vigente)
        {
            throw new InvalidOperationException("LOA exige uma LDO vigente.");
        }

        if (ldo.Exercicio != exercicio)
        {
            throw new InvalidOperationException($"Exercicio da LOA ({exercicio}) difere do da LDO ({ldo.Exercicio}).");
        }

        return new LeiOrcamentariaAnual(LoaId.New(), tenantId, exercicio, ldo.Id, ldo.PpaId, limiteSuplementacaoPercentual, numeroLei.Trim(), anoLei);
    }

    /// <summary>Prevê uma receita por natureza/fonte (somente em projeto de lei).</summary>
    /// <returns>Identificador da receita prevista.</returns>
    public ReceitaPrevistaId PreverReceita(NaturezaReceita natureza, string fonteDeRecurso, ValorMonetario valorPrevisto)
    {
        GarantirEditavel(nameof(PreverReceita));
        var receita = ReceitaPrevista.Criar(Id, natureza, fonteDeRecurso, valorPrevisto);
        _receitas.Add(receita);
        return receita.Id;
    }

    /// <summary>Fixa uma despesa (linha do QDD) vinculada a uma ação do PPA (somente em projeto de lei).</summary>
    /// <returns>Identificador do item de despesa fixada.</returns>
    public ItemDespesaFixadaId FixarDespesa(
        ClassificacaoOrcamentaria classificacao,
        AcaoPpaId acaoPpaId,
        string naturezaDespesa,
        ValorMonetario valorFixado)
    {
        GarantirEditavel(nameof(FixarDespesa));
        var item = ItemDespesaFixada.Criar(Id, classificacao, acaoPpaId, naturezaDespesa, valorFixado);
        _itens.Add(item);
        return item.Id;
    }

    /// <summary>Coloca a LOA em tramitação no Legislativo.</summary>
    public void ColocarEmTramitacao()
    {
        if (Situacao != SituacaoLoa.ProjetoLei)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiOrcamentariaAnual), Situacao.ToString(), nameof(ColocarEmTramitacao));
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("LOA exige ao menos um item de despesa fixada para tramitar.");
        }

        Situacao = SituacaoLoa.EmTramitacao;
    }

    /// <summary>
    /// Aprova a LOA após validar a compatibilidade LOA ⊆ LDO ⊆ PPA + equilíbrio
    /// (Lei 4.320/64 art. 2º; CF 167, I e §1º; CF 165 §2º). Falha → <see cref="IncompatibilidadeOrcamentariaException"/>.
    /// </summary>
    /// <param name="servico">Serviço de domínio que roda a checagem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <exception cref="IncompatibilidadeOrcamentariaException">Se a LOA for incompatível.</exception>
    public async Task AprovarAsync(ICompatibilidadeOrcamentariaService servico, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servico);
        if (Situacao is not (SituacaoLoa.ProjetoLei or SituacaoLoa.EmTramitacao))
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiOrcamentariaAnual), Situacao.ToString(), nameof(AprovarAsync));
        }

        var resultado = await servico.VerificarAsync(this, cancellationToken).ConfigureAwait(false);
        if (!resultado.Compativel)
        {
            throw new IncompatibilidadeOrcamentariaException(resultado.Motivos);
        }

        Situacao = SituacaoLoa.Aprovada;
        RaiseDomainEvent(new LoaAprovada(Id, Exercicio));
    }

    /// <summary>
    /// Coloca a LOA em execução (virar do exercício/publicação): para CADA item de despesa
    /// fixada ainda sem dotação, levanta <see cref="ItemLoaEntrouEmExecucao"/> (o handler de
    /// integração converte em dotação). Idempotente: item já vinculado não reemite.
    /// </summary>
    /// <exception cref="TransicaoPlanejamentoInvalidaException">Se não estiver aprovada/em execução.</exception>
    public void EntrarEmExecucao()
    {
        if (Situacao is not (SituacaoLoa.Aprovada or SituacaoLoa.EmExecucao))
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiOrcamentariaAnual), Situacao.ToString(), nameof(EntrarEmExecucao));
        }

        Situacao = SituacaoLoa.EmExecucao;
        foreach (var item in _itens.Where(i => !i.DotacaoGerada))
        {
            RaiseDomainEvent(new ItemLoaEntrouEmExecucao(Id, item.Id, Exercicio, item.Classificacao, item.ValorFixado, item.AcaoPpaId));
        }
    }

    /// <summary>Registra a dotação gerada para um item (fecha o elo 1:1 LOA→Dotação). Idempotente.</summary>
    /// <param name="itemId">Item de despesa fixada.</param>
    /// <param name="dotacaoId">Dotação gerada.</param>
    /// <exception cref="InvalidOperationException">Se o item não existir na LOA.</exception>
    public void RegistrarDotacaoGerada(ItemDespesaFixadaId itemId, DotacaoOrcamentariaId dotacaoId)
    {
        var item = _itens.Find(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Item de despesa fixada nao encontrado na LOA.");
        item.VincularDotacao(dotacaoId);
    }

    /// <summary>
    /// Cria um item de despesa fixada extraordinário decorrente de crédito ESPECIAL (cria dotação nova)
    /// e o coloca em execução (levanta <see cref="ItemLoaEntrouEmExecucao"/>). Só com a LOA em execução.
    /// </summary>
    /// <returns>Identificador do novo item.</returns>
    /// <exception cref="TransicaoPlanejamentoInvalidaException">Se a LOA não estiver em execução.</exception>
    public ItemDespesaFixadaId FixarDespesaPorCreditoEspecial(
        ClassificacaoOrcamentaria classificacao,
        AcaoPpaId acaoPpaId,
        string naturezaDespesa,
        ValorMonetario valorFixado)
    {
        if (Situacao != SituacaoLoa.EmExecucao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiOrcamentariaAnual), Situacao.ToString(), nameof(FixarDespesaPorCreditoEspecial));
        }

        var item = ItemDespesaFixada.Criar(Id, classificacao, acaoPpaId, naturezaDespesa, valorFixado, origemCreditoEspecial: true);
        _itens.Add(item);
        RaiseDomainEvent(new ItemLoaEntrouEmExecucao(Id, item.Id, Exercicio, item.Classificacao, item.ValorFixado, item.AcaoPpaId));
        return item.Id;
    }

    /// <summary>Encerra a LOA no fim do exercício.</summary>
    public void Encerrar()
    {
        if (Situacao != SituacaoLoa.EmExecucao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiOrcamentariaAnual), Situacao.ToString(), nameof(Encerrar));
        }

        Situacao = SituacaoLoa.Encerrada;
    }

    private void GarantirEditavel(string operacao)
    {
        if (Situacao != SituacaoLoa.ProjetoLei)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiOrcamentariaAnual), Situacao.ToString(), operacao);
        }
    }
}
