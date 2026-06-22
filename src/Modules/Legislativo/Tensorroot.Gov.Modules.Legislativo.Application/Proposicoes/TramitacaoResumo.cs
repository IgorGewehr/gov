namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Resumo de uma fase da trilha de tramitacao (transparencia LAI/APIs abertas).</summary>
/// <param name="Id">Identificador da fase.</param>
/// <param name="Fase">Fase de tramitacao.</param>
/// <param name="Comissao">Comissao emitente (quando a fase for parecer); nulo nas demais.</param>
/// <param name="ParecerFavoravel">Sentido do parecer (quando houver); nulo nas demais fases.</param>
/// <param name="Data">Data da fase.</param>
public sealed record TramitacaoResumo(
    Guid Id,
    string Fase,
    string? Comissao,
    bool? ParecerFavoravel,
    DateOnly Data);
