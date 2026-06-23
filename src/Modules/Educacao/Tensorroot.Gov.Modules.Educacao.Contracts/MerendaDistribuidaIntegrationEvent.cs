using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Contracts;

/// <summary>
/// Evento de integracao publico: a merenda foi distribuida em uma escola, consumindo generos do
/// almoxarifado (PNAE). Carrega o consumo por genero (ItemEstoque por Id + quantidade) para que o
/// modulo Patrimonio realize a baixa efetiva do saldo/lote (<c>AtenderRequisicao</c>) de forma
/// idempotente por <c>EventId</c> no consumidor (reentrega via Outbox). A operacao local nasce aqui;
/// a prestacao de contas PNAE ao FNDE = M10 (atras de ACL).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente municipal/rede) dono do registro.</param>
/// <param name="DistribuicaoId">Identificador da distribuicao de merenda.</param>
/// <param name="EscolaId">Escola da distribuicao.</param>
/// <param name="Data">Data efetiva da distribuicao.</param>
/// <param name="Consumos">Consumo por genero (ItemEstoque por Id + quantidade a baixar).</param>
public sealed record MerendaDistribuidaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid DistribuicaoId,
    Guid EscolaId,
    DateOnly Data,
    IReadOnlyList<ConsumoGeneroMerenda> Consumos) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Item de consumo de genero da merenda no evento de integracao: o genero do almoxarifado (ItemEstoque
/// por Id) e a quantidade a baixar. Consumido por Patrimonio.
/// </summary>
/// <param name="GeneroEstoqueId">Genero (ItemEstoque de Patrimonio) por Id.</param>
/// <param name="Quantidade">Quantidade a baixar do estoque.</param>
/// <param name="UnidadeMedida">Unidade de medida do genero (kg, L, un).</param>
public sealed record ConsumoGeneroMerenda(Guid GeneroEstoqueId, decimal Quantidade, string UnidadeMedida);
