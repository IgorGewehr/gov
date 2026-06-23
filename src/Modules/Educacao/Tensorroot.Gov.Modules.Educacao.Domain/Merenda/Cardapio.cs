using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

/// <summary>
/// Cardapio semanal do PNAE: planejamento nutricional de uma escola, faixa etaria e semana (segunda da
/// semana ISO), composto por itens (genero x refeicao x dia, com per capita). Raiz de agregado que
/// nasce <see cref="SituacaoCardapio.Planejado"/> via <see cref="Planejar"/> e admite edicao de itens
/// somente enquanto Planejado (I-M1). Apos <see cref="Publicar"/>, vira a base das distribuicoes do dia.
/// Operacao local; prestacao de contas PNAE ao FNDE = M10 (atras de ACL).
/// </summary>
public sealed class Cardapio : AggregateRoot<CardapioId>, IMustHaveTenant
{
    private readonly List<ItemCardapio> _itens = [];

    private Cardapio()
    {
    }

    private Cardapio(
        CardapioId id,
        Guid tenantId,
        EscolaId escolaId,
        FaixaEtariaPnae faixaEtaria,
        DateOnly semana)
        : base(id)
    {
        TenantId = tenantId;
        EscolaId = escolaId;
        FaixaEtaria = faixaEtaria;
        Semana = semana;
        Situacao = SituacaoCardapio.Planejado;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Escola do cardapio (FK logica ao agregado Escola — referencia por Id).</summary>
    public EscolaId EscolaId { get; private set; }

    /// <summary>Faixa etaria PNAE de referencia nutricional.</summary>
    public FaixaEtariaPnae FaixaEtaria { get; private set; }

    /// <summary>Primeiro dia da semana planejada (segunda-feira).</summary>
    public DateOnly Semana { get; private set; }

    /// <summary>Situacao atual do cardapio.</summary>
    public SituacaoCardapio Situacao { get; private set; }

    /// <summary>Itens planejados (entidades-filhas: genero x refeicao x dia).</summary>
    public IReadOnlyCollection<ItemCardapio> Itens => _itens.AsReadOnly();

    /// <summary>
    /// Planeja um novo cardapio semanal (situacao inicial <see cref="SituacaoCardapio.Planejado"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="escolaId">Escola do cardapio.</param>
    /// <param name="faixaEtaria">Faixa etaria PNAE.</param>
    /// <param name="semana">Primeiro dia da semana (segunda-feira).</param>
    /// <returns>Novo <see cref="Cardapio"/> em situacao Planejado.</returns>
    /// <exception cref="ArgumentException">Se a escola for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a faixa etaria for invalida.</exception>
    public static Cardapio Planejar(Guid tenantId, EscolaId escolaId, FaixaEtariaPnae faixaEtaria, DateOnly semana)
    {
        if (escolaId.Value == Guid.Empty)
        {
            throw new ArgumentException("Escola obrigatoria para o cardapio.", nameof(escolaId));
        }

        if (!Enum.IsDefined(faixaEtaria))
        {
            throw new ArgumentOutOfRangeException(nameof(faixaEtaria), "Faixa etaria PNAE invalida.");
        }

        var cardapio = new Cardapio(CardapioId.New(), tenantId, escolaId, faixaEtaria, semana);
        cardapio.RaiseDomainEvent(new CardapioPlanejado(cardapio.Id));
        return cardapio;
    }

    /// <summary>
    /// Adiciona (planeja) um genero a uma refeicao/dia. Admitido somente enquanto Planejado (I-M1).
    /// </summary>
    /// <param name="dia">Dia da semana.</param>
    /// <param name="refeicao">Tipo de refeicao.</param>
    /// <param name="generoEstoqueId">Genero (ItemEstoque de Patrimonio) por Id.</param>
    /// <param name="quantidadePerCapita">Per capita (&gt; 0).</param>
    /// <param name="unidadeMedida">Unidade de medida do genero.</param>
    /// <returns>O identificador do item adicionado.</returns>
    /// <exception cref="InvalidOperationException">Se o cardapio nao estiver Planejado (I-M1).</exception>
    public ItemCardapioId AdicionarItem(
        DiaSemanaCardapio dia,
        TipoRefeicao refeicao,
        Guid generoEstoqueId,
        decimal quantidadePerCapita,
        string unidadeMedida)
    {
        if (Situacao != SituacaoCardapio.Planejado)
        {
            throw new InvalidOperationException($"Edicao de itens exige cardapio Planejado. Situacao atual: {Situacao}.");
        }

        var item = ItemCardapio.Planejar(dia, refeicao, generoEstoqueId, quantidadePerCapita, unidadeMedida);
        _itens.Add(item);
        return item.Id;
    }

    /// <summary>Publica o cardapio (Planejado -&gt; Publicado). Exige ao menos um item (I-M3).</summary>
    /// <exception cref="InvalidOperationException">Se nao estiver Planejado ou nao houver itens (I-M3).</exception>
    public void Publicar()
    {
        if (Situacao != SituacaoCardapio.Planejado)
        {
            throw new InvalidOperationException($"A publicacao exige cardapio Planejado. Situacao atual: {Situacao}.");
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("Cardapio sem itens nao pode ser publicado (I-M3).");
        }

        Situacao = SituacaoCardapio.Publicado;
        RaiseDomainEvent(new CardapioPublicado(Id));
    }

    /// <summary>Encerra o cardapio (estado terminal — semana concluida).</summary>
    /// <exception cref="InvalidOperationException">Se ja estiver Encerrado.</exception>
    public void Encerrar()
    {
        if (Situacao == SituacaoCardapio.Encerrado)
        {
            throw new InvalidOperationException("Cardapio ja encerrado.");
        }

        Situacao = SituacaoCardapio.Encerrado;
    }

    /// <summary>
    /// Calcula o consumo planejado por genero para uma refeicao/dia e um numero de comensais
    /// (per capita x comensais), agregando os itens daquele dia/refeicao. Base da distribuicao.
    /// </summary>
    /// <param name="dia">Dia da semana.</param>
    /// <param name="refeicao">Tipo de refeicao.</param>
    /// <param name="comensais">Numero de comensais (&gt; 0).</param>
    /// <returns>Consumo previsto por genero (genero -&gt; quantidade total e unidade).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os comensais nao forem positivos.</exception>
    public IReadOnlyList<ConsumoPrevisto> CalcularConsumo(DiaSemanaCardapio dia, TipoRefeicao refeicao, int comensais)
    {
        if (comensais <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(comensais), "Comensais deve ser positivo.");
        }

        return _itens
            .Where(item => item.Dia == dia && item.Refeicao == refeicao)
            .GroupBy(item => new { item.GeneroEstoqueId, item.UnidadeMedida })
            .Select(grupo => new ConsumoPrevisto(
                grupo.Key.GeneroEstoqueId,
                grupo.Sum(item => item.QuantidadePerCapita) * comensais,
                grupo.Key.UnidadeMedida))
            .ToList();
    }
}

/// <summary>
/// Consumo previsto de um genero (resultado do calculo per capita x comensais). VO de transporte
/// entre o planejamento (Cardapio) e a baixa efetiva (DistribuicaoMerenda).
/// </summary>
/// <param name="GeneroEstoqueId">Genero (ItemEstoque) por Id.</param>
/// <param name="Quantidade">Quantidade total prevista a consumir.</param>
/// <param name="UnidadeMedida">Unidade de medida do genero.</param>
public readonly record struct ConsumoPrevisto(Guid GeneroEstoqueId, decimal Quantidade, string UnidadeMedida);
