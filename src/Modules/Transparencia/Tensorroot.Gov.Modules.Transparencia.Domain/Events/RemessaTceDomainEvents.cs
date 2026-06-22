using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Events;

/// <summary>Remessa consolidada e montada (nasce em <c>Gerada</c>) — LRF arts. 48/48-A.</summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
/// <param name="Periodo">Período (competência/exercício) da remessa.</param>
/// <param name="LeiauteVersao">Versão do leiaute usado.</param>
public sealed record RemessaGerada(RemessaTceId RemessaTceId, string Periodo, string LeiauteVersao) : IDomainEvent;

/// <summary>Remessa validada pelo e-Validador sem erro no RDI (apta a envio).</summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
public sealed record RemessaValidada(RemessaTceId RemessaTceId) : IDomainEvent;

/// <summary>Remessa rejeitada — RDI com erro; envio bloqueado (terminal de falha).</summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
/// <param name="QuantidadeErros">Quantidade de erros apurados no RDI.</param>
public sealed record RemessaRejeitada(RemessaTceId RemessaTceId, int QuantidadeErros) : IDomainEvent;

/// <summary>
/// Remessa empacotada e disponibilizada para download (ZIP nomeado), aguardando transmissão MANUAL no
/// PAD/e-Protocolo (passa a <c>ProntaParaTransmissao</c>).
/// </summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
/// <param name="NomeZip">Nome estruturado do ZIP gerado.</param>
public sealed record RemessaProntaParaTransmissao(RemessaTceId RemessaTceId, string NomeZip) : IDomainEvent;

/// <summary>
/// Remessa transmitida ao SIAPC/PAD — o operador registrou o protocolo/recibo retornado pelo portal
/// (passa a <c>Enviada</c>; NÃO é um POST a um endpoint de envio).
/// </summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
/// <param name="DataEnvio">Data do recibo/transmissão.</param>
/// <param name="Protocolo">Protocolo/recibo retornado pelo PAD/e-Protocolo.</param>
public sealed record RemessaEnviadaTce(RemessaTceId RemessaTceId, DateOnly DataEnvio, string Protocolo) : IDomainEvent;

/// <summary>Remessa homologada pelo TCE-RS (terminal de sucesso).</summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
public sealed record RemessaHomologada(RemessaTceId RemessaTceId) : IDomainEvent;

/// <summary>Prazo de remessa vencido sem envio — alerta de risco de bloqueio de transferências (LRF art. 23 §3º).</summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
/// <param name="DataLimite">Data-limite legal/parametrizada do período.</param>
public sealed record PrazoRemessaVencido(RemessaTceId RemessaTceId, DateOnly DataLimite) : IDomainEvent;
