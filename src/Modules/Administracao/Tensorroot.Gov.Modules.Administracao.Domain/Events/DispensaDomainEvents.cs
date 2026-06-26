using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Events;

/// <summary>Dispensa eletronica aberta (rascunho): itens em cadastro (Lei 14.133/2021, art. 75).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Fundamento">Fundamento legal da dispensa em razao do valor (art. 75, I ou II).</param>
public sealed record DispensaAberta(DispensaEletronicaId DispensaId, FundamentoDispensaValor Fundamento) : IDomainEvent;

/// <summary>Aviso de contratacao direta publicado (IN SEGES/ME 67/2021; art. 75 §3).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="NumeroAviso">Identificador/numero do aviso publicado.</param>
/// <param name="AberturaDisputa">Data/hora de abertura da etapa de lances.</param>
public sealed record AvisoDispensaPublicado(
    DispensaEletronicaId DispensaId,
    string NumeroAviso,
    DateTimeOffset AberturaDisputa) : IDomainEvent;

/// <summary>Etapa de envio de lances aberta (IN SEGES/ME 67/2021, art. 9).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
public sealed record DisputaDispensaAberta(DispensaEletronicaId DispensaId) : IDomainEvent;

/// <summary>Dispensa homologada pela autoridade competente — habilita a contratacao direta.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="CotacaoVencedoraId">Cotacao julgada vencedora.</param>
/// <param name="FornecedorVencedorId">Fornecedor vencedor.</param>
/// <param name="ValorAdjudicado">Valor adjudicado (valor da cotacao vencedora).</param>
public sealed record DispensaHomologada(
    DispensaEletronicaId DispensaId,
    Guid CotacaoVencedoraId,
    Guid FornecedorVencedorId,
    decimal ValorAdjudicado) : IDomainEvent;

/// <summary>Dispensa declarada fracassada — sem cotacao valida/habilitada.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record DispensaFracassada(DispensaEletronicaId DispensaId, string Motivo) : IDomainEvent;

/// <summary>Dispensa declarada deserta — sem cotacoes/interessados.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
public sealed record DispensaDeserta(DispensaEletronicaId DispensaId) : IDomainEvent;

/// <summary>Dispensa revogada por conveniencia/oportunidade.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Motivo">Motivacao do ato administrativo.</param>
public sealed record DispensaRevogada(DispensaEletronicaId DispensaId, string Motivo) : IDomainEvent;

/// <summary>Dispensa anulada por ilegalidade.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Motivo">Motivacao do ato administrativo (vicio de legalidade).</param>
public sealed record DispensaAnulada(DispensaEletronicaId DispensaId, string Motivo) : IDomainEvent;
