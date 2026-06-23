using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

/// <summary>
/// Distribuicao de merenda de um dia: registra a refeicao efetivamente servida em uma escola (data,
/// tipo, numero de comensais) e a baixa de generos por consumo (per capita x comensais), calculada a
/// partir do <see cref="Cardapio"/> publicado. Raiz de agregado que nasce ja com o consumo registrado
/// via <see cref="Registrar"/> (a baixa real do estoque ocorre em Patrimonio, acionada por evento de
/// integracao). Operacao local; PNAE/FNDE = M10.
/// </summary>
public sealed class DistribuicaoMerenda : AggregateRoot<DistribuicaoMerendaId>, IMustHaveTenant
{
    private readonly List<ConsumoGenero> _consumos = [];

    private DistribuicaoMerenda()
    {
    }

    private DistribuicaoMerenda(
        DistribuicaoMerendaId id,
        Guid tenantId,
        EscolaId escolaId,
        CardapioId cardapioId,
        DateOnly data,
        TipoRefeicao refeicao,
        int comensais)
        : base(id)
    {
        TenantId = tenantId;
        EscolaId = escolaId;
        CardapioId = cardapioId;
        Data = data;
        Refeicao = refeicao;
        Comensais = comensais;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Escola da distribuicao (FK logica ao agregado Escola — referencia por Id).</summary>
    public EscolaId EscolaId { get; private set; }

    /// <summary>Cardapio publicado que originou o consumo (FK logica ao agregado Cardapio).</summary>
    public CardapioId CardapioId { get; private set; }

    /// <summary>Data efetiva da distribuicao.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Tipo de refeicao servida.</summary>
    public TipoRefeicao Refeicao { get; private set; }

    /// <summary>Numero de comensais (matriculados que receberam a refeicao).</summary>
    public int Comensais { get; private set; }

    /// <summary>Consumo efetivo por genero (entidades-filhas: baixa reconhecida).</summary>
    public IReadOnlyCollection<ConsumoGenero> Consumos => _consumos.AsReadOnly();

    /// <summary>
    /// Registra a distribuicao do dia a partir do consumo previsto (calculado pelo Cardapio publicado),
    /// criando as baixas por genero. Emite <see cref="MerendaDistribuida"/> (aciona a baixa em Patrimonio
    /// via Integration Event). Exige ao menos um genero a consumir (I-M5).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="escolaId">Escola da distribuicao.</param>
    /// <param name="cardapioId">Cardapio publicado de origem.</param>
    /// <param name="data">Data da distribuicao.</param>
    /// <param name="refeicao">Tipo de refeicao servida.</param>
    /// <param name="comensais">Numero de comensais (&gt; 0).</param>
    /// <param name="consumoPrevisto">Consumo por genero (per capita x comensais).</param>
    /// <returns>Nova <see cref="DistribuicaoMerenda"/> com os consumos registrados.</returns>
    /// <exception cref="ArgumentException">Se escola/cardapio forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se os comensais nao forem positivos ou refeicao invalida.</exception>
    /// <exception cref="InvalidOperationException">Se nao houver genero a consumir (I-M5).</exception>
    public static DistribuicaoMerenda Registrar(
        Guid tenantId,
        EscolaId escolaId,
        CardapioId cardapioId,
        DateOnly data,
        TipoRefeicao refeicao,
        int comensais,
        IReadOnlyCollection<ConsumoPrevisto> consumoPrevisto)
    {
        ArgumentNullException.ThrowIfNull(consumoPrevisto);
        if (escolaId.Value == Guid.Empty)
        {
            throw new ArgumentException("Escola obrigatoria.", nameof(escolaId));
        }

        if (cardapioId.Value == Guid.Empty)
        {
            throw new ArgumentException("Cardapio obrigatorio.", nameof(cardapioId));
        }

        if (!Enum.IsDefined(refeicao))
        {
            throw new ArgumentOutOfRangeException(nameof(refeicao), "Tipo de refeicao invalido.");
        }

        if (comensais <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(comensais), "Comensais deve ser positivo.");
        }

        if (consumoPrevisto.Count == 0)
        {
            throw new InvalidOperationException("Nao ha genero planejado para a refeicao/dia (I-M5).");
        }

        var distribuicao = new DistribuicaoMerenda(
            DistribuicaoMerendaId.New(), tenantId, escolaId, cardapioId, data, refeicao, comensais);

        foreach (var previsto in consumoPrevisto)
        {
            distribuicao._consumos.Add(
                ConsumoGenero.Registrar(previsto.GeneroEstoqueId, previsto.Quantidade, previsto.UnidadeMedida));
        }

        distribuicao.RaiseDomainEvent(new MerendaDistribuida(distribuicao.Id, escolaId, data, refeicao));
        return distribuicao;
    }
}
