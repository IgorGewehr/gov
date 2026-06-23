using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>Cardapio semanal planejado (situacao inicial Planejado).</summary>
/// <param name="CardapioId">Identificador do cardapio.</param>
public sealed record CardapioPlanejado(CardapioId CardapioId) : IDomainEvent;

/// <summary>Cardapio publicado (base das distribuicoes da semana).</summary>
/// <param name="CardapioId">Identificador do cardapio.</param>
public sealed record CardapioPublicado(CardapioId CardapioId) : IDomainEvent;

/// <summary>
/// Merenda distribuida em uma escola/data/refeicao — dispara a baixa de generos no almoxarifado
/// (Patrimonio) via Integration Event (cross-module por Contracts).
/// </summary>
/// <param name="DistribuicaoId">Identificador da distribuicao.</param>
/// <param name="EscolaId">Escola da distribuicao.</param>
/// <param name="Data">Data da distribuicao.</param>
/// <param name="Refeicao">Tipo de refeicao servida.</param>
public sealed record MerendaDistribuida(
    DistribuicaoMerendaId DistribuicaoId,
    EscolaId EscolaId,
    DateOnly Data,
    TipoRefeicao Refeicao) : IDomainEvent;
