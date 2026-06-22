using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Marcacao de ponto registrada (entrada do AFD imutavel; NSR atribuido).</summary>
/// <param name="MarcacaoPontoId">Identificador da marcacao.</param>
/// <param name="ServidorId">Servidor da marcacao.</param>
/// <param name="Nsr">Numero sequencial de registro atribuido.</param>
/// <param name="DataHora">Data/hora exata da batida.</param>
public sealed record MarcacaoPontoRegistrada(MarcacaoPontoId MarcacaoPontoId, Guid ServidorId, long Nsr, DateTimeOffset DataHora) : IDomainEvent;

/// <summary>Apuracao de jornada de uma competencia fechada (espelho/AEJ congelados; gancho p/ folha).</summary>
/// <param name="ApuracaoPontoId">Identificador da apuracao.</param>
/// <param name="ServidorId">Servidor apurado.</param>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="MinutosExtras">Total de minutos de hora extra apurados.</param>
/// <param name="MinutosFalta">Total de minutos de falta/atraso apurados.</param>
public sealed record ApuracaoPontoFechada(
    ApuracaoPontoId ApuracaoPontoId,
    Guid ServidorId,
    int Ano,
    int Mes,
    int MinutosExtras,
    int MinutosFalta) : IDomainEvent;
