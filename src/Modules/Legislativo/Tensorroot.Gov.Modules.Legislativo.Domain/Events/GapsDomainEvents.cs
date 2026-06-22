using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Events;

// --- G1 Normas Juridicas ---

/// <summary>Norma promulgada (nasce em vigor) — N-3.</summary>
/// <param name="NormaId">Identificador da norma.</param>
/// <param name="Tipo">Especie da norma.</param>
/// <param name="Numero">Numero.</param>
/// <param name="Ano">Ano.</param>
public sealed record NormaPromulgada(NormaId NormaId, TipoNorma Tipo, int Numero, int Ano) : IDomainEvent;

/// <summary>Norma revogada (terminal) — N-4.</summary>
/// <param name="NormaId">Identificador da norma.</param>
/// <param name="DataRevogacao">Data da revogacao.</param>
public sealed record NormaRevogada(NormaId NormaId, DateOnly DataRevogacao) : IDomainEvent;

// --- G2 Diario Oficial ---

/// <summary>Edicao do Diario Oficial publicada (marco de eficacia) — mapeia para o Integration Event via Outbox.</summary>
/// <param name="EdicaoDiarioId">Identificador da edicao.</param>
/// <param name="Numero">Numero da edicao.</param>
/// <param name="Ano">Ano da edicao.</param>
/// <param name="DataPublicacao">Momento oficial da publicacao.</param>
public sealed record DiarioPublicado(EdicaoDiarioId EdicaoDiarioId, int Numero, int Ano, DateTimeOffset DataPublicacao) : IDomainEvent;

// --- G3 Tribuna ---

/// <summary>Orador iniciou a fala (cronometro iniciado) — T-4.</summary>
/// <param name="TribunaSessaoId">Identificador da tribuna.</param>
/// <param name="InscricaoId">Identificador da inscricao.</param>
/// <param name="VereadorId">Vereador na tribuna.</param>
public sealed record OradorIniciouFala(TribunaSessaoId TribunaSessaoId, InscricaoOradorId InscricaoId, VereadorId VereadorId) : IDomainEvent;

/// <summary>Orador encerrou a fala (tempo apurado) — T-6.</summary>
/// <param name="TribunaSessaoId">Identificador da tribuna.</param>
/// <param name="InscricaoId">Identificador da inscricao.</param>
/// <param name="VereadorId">Vereador na tribuna.</param>
/// <param name="TempoUtilizado">Tempo efetivamente utilizado.</param>
/// <param name="Excedente">Tempo excedente (>= zero).</param>
public sealed record OradorEncerrouFala(
    TribunaSessaoId TribunaSessaoId,
    InscricaoOradorId InscricaoId,
    VereadorId VereadorId,
    TimeSpan TempoUtilizado,
    TimeSpan Excedente) : IDomainEvent;
