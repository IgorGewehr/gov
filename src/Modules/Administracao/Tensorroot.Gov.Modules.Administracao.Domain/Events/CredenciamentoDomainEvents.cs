using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Events;

/// <summary>Edital de credenciamento aberto (rascunho): itens/condicoes em cadastro (Lei 14.133/2021, art. 79).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="Hipotese">Hipotese autorizadora (art. 79, I a III).</param>
public sealed record CredenciamentoAberto(CredenciamentoId CredenciamentoId, HipoteseCredenciamento Hipotese) : IDomainEvent;

/// <summary>Chamamento publico do credenciamento publicado e aberto a inscricoes permanentes (art. 79, par. unico).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="NumeroEdital">Numero/identificador do edital de chamamento.</param>
public sealed record ChamamentoCredenciamentoPublicado(CredenciamentoId CredenciamentoId, string NumeroEdital) : IDomainEvent;

/// <summary>Interessado inscrito (protocolo de adesao ao chamamento permanente).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="FornecedorId">Fornecedor interessado.</param>
public sealed record InteressadoInscrito(CredenciamentoId CredenciamentoId, CredenciadoId CredenciadoId, Guid FornecedorId) : IDomainEvent;

/// <summary>Interessado credenciado (inscricao deferida apos analise): apto a ser contratado por inexigibilidade (art. 74, IV).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="FornecedorId">Fornecedor credenciado.</param>
public sealed record InteressadoCredenciado(CredenciamentoId CredenciamentoId, CredenciadoId CredenciadoId, Guid FornecedorId) : IDomainEvent;

/// <summary>Interessado descredenciado (terminal): a pedido, descumprimento ou sancao impeditiva.</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="FornecedorId">Fornecedor descredenciado.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record InteressadoDescredenciado(CredenciamentoId CredenciamentoId, CredenciadoId CredenciadoId, Guid FornecedorId, string Motivo) : IDomainEvent;

/// <summary>Credenciamento encerrado/anulado/revogado (terminal do edital).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="Situacao">Situacao terminal alcancada.</param>
/// <param name="Motivo">Motivacao do ato administrativo.</param>
public sealed record CredenciamentoEncerrado(CredenciamentoId CredenciamentoId, SituacaoCredenciamento Situacao, string Motivo) : IDomainEvent;
