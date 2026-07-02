using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Events;

/// <summary>
/// Documento juntado ao processo — torna-se imutavel e integra a trilha documental
/// (Lei 11.419/2006; e-ARQ Brasil). Emitido por <see cref="Documento.Juntar"/> (I-2).
/// </summary>
/// <param name="DocumentoId">Identificador do documento juntado.</param>
/// <param name="ProcessoId">Processo ao qual o documento foi juntado.</param>
/// <param name="Hash">Resumo SHA-256 do conteudo (integridade).</param>
public sealed record DocumentoJuntado(DocumentoId DocumentoId, Guid ProcessoId, Hash Hash) : IDomainEvent;

/// <summary>
/// Documento assinado conforme a criticidade do ato (Decreto 10.543/2020).
/// Emitido por <see cref="Documento.Assinar"/> (I-7); admite coassinatura/re-assinatura.
/// </summary>
/// <param name="DocumentoId">Identificador do documento assinado.</param>
/// <param name="SignatarioId">Sujeito que assinou.</param>
/// <param name="Tipo">Nivel da assinatura aplicada.</param>
public sealed record DocumentoAssinado(DocumentoId DocumentoId, Guid SignatarioId, TipoAssinatura Tipo) : IDomainEvent;

/// <summary>Processo administrativo autuado — NUP gerado e situacao inicial <c>Autuado</c> (Lei 9.784/1999). Emitido por <see cref="Processo.Autuar"/> (I-2).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Nup">Numero Unico de Protocolo gerado.</param>
/// <param name="OrigemModulo">Modulo originador (quando autuado por outro modulo).</param>
/// <param name="OrigemId">Identificador da origem no modulo originador.</param>
public sealed record ProcessoAutuado(
    ProcessoId ProcessoId,
    Nup Nup,
    string? OrigemModulo,
    Guid? OrigemId) : IDomainEvent;

/// <summary>Processo tramitado para um setor de destino (movimentacao registrada). Emitido por <see cref="Processo.Tramitar"/> e <see cref="Processo.Reativar"/> (I-4).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="SetorDestinoId">Setor de destino da tramitacao.</param>
public sealed record ProcessoTramitado(ProcessoId ProcessoId, Guid SetorDestinoId) : IDomainEvent;

/// <summary>Processo sobrestado (andamento temporariamente suspenso). Emitido por <see cref="Processo.Sobrestar"/> (I-5).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Motivo">Motivo do sobrestamento.</param>
public sealed record ProcessoSobrestado(ProcessoId ProcessoId, string Motivo) : IDomainEvent;

/// <summary>Processo arquivado (terminal); guarda regida pela Tabela de Temporalidade (TTD/CONARQ). Emitido por <see cref="Processo.Arquivar"/> (I-7).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Motivo">Motivo do arquivamento (Lei 9.784/1999).</param>
/// <param name="Data">Data do arquivamento.</param>
public sealed record ProcessoArquivado(ProcessoId ProcessoId, string? Motivo, DateOnly Data) : IDomainEvent;
