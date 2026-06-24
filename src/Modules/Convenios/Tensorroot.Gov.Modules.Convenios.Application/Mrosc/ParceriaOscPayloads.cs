using Tensorroot.Gov.Modules.Convenios.Domain.Comum;

namespace Tensorroot.Gov.Modules.Convenios.Application.Mrosc;

/// <summary>Certidao de regularidade da OSC na superficie de aplicacao (fluxo B).</summary>
/// <param name="Tipo">Tipo da certidao.</param>
/// <param name="ValidaAte">Validade.</param>
public sealed record CertidaoPayload(string Tipo, DateOnly ValidaAte);

/// <summary>Dados da OSC na superficie de aplicacao (fluxo B).</summary>
/// <param name="Cnpj">CNPJ (com ou sem mascara).</param>
/// <param name="RazaoSocial">Razao social.</param>
/// <param name="NaturezaJuridica">Natureza juridica.</param>
/// <param name="ExperienciaPrevia">Experiencia previa comprovada (art. 33).</param>
/// <param name="CapacidadeTecnica">Capacidade tecnica/operacional (art. 33-34).</param>
/// <param name="Certidoes">Certidoes de regularidade.</param>
public sealed record OscPayload(
    string Cnpj,
    string RazaoSocial,
    string NaturezaJuridica,
    bool ExperienciaPrevia,
    bool CapacidadeTecnica,
    IReadOnlyList<CertidaoPayload> Certidoes);

/// <summary>Forma de selecao na superficie de aplicacao (fluxo B). Campos por tipo (mutuamente exclusivos).</summary>
/// <param name="Tipo">Tipo (chamamento/dispensa/inexigibilidade).</param>
/// <param name="ProcessoId">Processo do chamamento (quando chamamento).</param>
/// <param name="Edital">Edital do chamamento (quando chamamento).</param>
/// <param name="EditalHomologado">Marca de homologacao (quando chamamento).</param>
/// <param name="FundamentoLegal">Fundamento legal (quando direta).</param>
/// <param name="Justificativa">Justificativa textual (quando direta).</param>
public sealed record FormaSelecaoPayload(
    TipoFormaSelecao Tipo,
    Guid? ProcessoId,
    string? Edital,
    bool EditalHomologado,
    string? FundamentoLegal,
    string? Justificativa);

/// <summary>Meta do plano de trabalho da OSC na superficie de aplicacao (fluxo B).</summary>
/// <param name="Descricao">Descricao da meta.</param>
/// <param name="Indicador">Indicador.</param>
/// <param name="ParametroEsperado">Parametro esperado.</param>
public sealed record MetaPayload(string Descricao, string Indicador, string ParametroEsperado);

/// <summary>Parcela de repasse na superficie de aplicacao (fluxo B).</summary>
/// <param name="NumeroOrdem">Numero de ordem.</param>
/// <param name="Valor">Valor.</param>
/// <param name="DataPrevista">Data prevista.</param>
/// <param name="Condicionantes">Condicionantes.</param>
public sealed record ParcelaRepassePayload(int NumeroOrdem, decimal Valor, DateOnly DataPrevista, string Condicionantes);
