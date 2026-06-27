using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>ASO registrado (gancho p/ geracao do S-2220 e atualizacao do PCMSO).</summary>
/// <param name="ExameOcupacionalId">Identificador do ASO.</param>
/// <param name="ServidorId">Servidor examinado.</param>
/// <param name="Tipo">Tipo do exame ocupacional.</param>
/// <param name="DataExame">Data de realizacao.</param>
public sealed record ExameOcupacionalRegistrado(
    ExameOcupacionalId ExameOcupacionalId,
    ServidorId ServidorId,
    TipoExameOcupacional Tipo,
    DateOnly DataExame) : IDomainEvent;

/// <summary>Periodo de exposicao a agentes nocivos iniciado (gancho p/ S-2240/PPP).</summary>
/// <param name="ExposicaoAgenteNocivoId">Identificador da exposicao.</param>
/// <param name="ServidorId">Servidor exposto.</param>
/// <param name="InicioExposicao">Inicio do periodo.</param>
public sealed record ExposicaoAgenteNocivoIniciada(
    ExposicaoAgenteNocivoId ExposicaoAgenteNocivoId,
    ServidorId ServidorId,
    DateOnly InicioExposicao) : IDomainEvent;

/// <summary>Periodo de exposicao a agentes nocivos encerrado (gancho p/ S-2240/PPP).</summary>
/// <param name="ExposicaoAgenteNocivoId">Identificador da exposicao.</param>
/// <param name="ServidorId">Servidor exposto.</param>
/// <param name="FimExposicao">Fim do periodo.</param>
public sealed record ExposicaoAgenteNocivoEncerrada(
    ExposicaoAgenteNocivoId ExposicaoAgenteNocivoId,
    ServidorId ServidorId,
    DateOnly FimExposicao) : IDomainEvent;

/// <summary>CAT comunicada (gancho p/ geracao do S-2210).</summary>
/// <param name="ComunicacaoAcidenteId">Identificador da CAT.</param>
/// <param name="ServidorId">Servidor acidentado.</param>
/// <param name="TipoCat">Tipo da CAT (inicial/reabertura/obito).</param>
/// <param name="DataHoraAcidente">Data/hora do acidente.</param>
/// <param name="HouveObito">Indica obito.</param>
public sealed record ComunicacaoAcidenteRegistrada(
    ComunicacaoAcidenteId ComunicacaoAcidenteId,
    ServidorId ServidorId,
    TipoCat TipoCat,
    DateTimeOffset DataHoraAcidente,
    bool HouveObito) : IDomainEvent;

/// <summary>Registro de SST (ASO/exposicao/CAT) cancelado (tornado sem efeito).</summary>
/// <param name="RegistroId">Identificador do registro cancelado.</param>
/// <param name="TipoRegistro">Nome do tipo de registro (ExameOcupacional/ExposicaoAgenteNocivo/ComunicacaoAcidente).</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record RegistroSstCancelado(Guid RegistroId, string TipoRegistro, string Motivo) : IDomainEvent;
