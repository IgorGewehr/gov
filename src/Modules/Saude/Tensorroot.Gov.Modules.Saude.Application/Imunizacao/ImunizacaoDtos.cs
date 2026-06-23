namespace Tensorroot.Gov.Modules.Saude.Application.Imunizacao;

/// <summary>Item da lista do catalogo de imunobiologicos.</summary>
/// <param name="Id">Identificador do imunobiologico.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Sigla">Sigla.</param>
/// <param name="TotalDoses">Total de doses do esquema.</param>
/// <param name="IntervaloDiasProximaDose">Intervalo padrao (dias) entre doses.</param>
/// <param name="DoseUnica">Indica dose unica.</param>
public sealed record ImunobiologicoItemLista(
    Guid Id,
    string Nome,
    string Sigla,
    int TotalDoses,
    int IntervaloDiasProximaDose,
    bool DoseUnica);

/// <summary>Dose aplicada na carteira de vacinacao.</summary>
/// <param name="ImunobiologicoId">Imunobiologico aplicado.</param>
/// <param name="Sigla">Sigla do imunobiologico.</param>
/// <param name="TipoDose">Tipo da dose (descricao).</param>
/// <param name="NumeroDose">Numero da dose no esquema.</param>
/// <param name="Lote">Lote aplicado.</param>
/// <param name="DataAplicacao">Data de aplicacao.</param>
/// <param name="ProximaDoseAprazada">Aprazamento da proxima dose (opcional).</param>
public sealed record DoseAplicadaDto(
    Guid ImunobiologicoId,
    string Sigla,
    string TipoDose,
    int NumeroDose,
    string Lote,
    DateOnly DataAplicacao,
    DateOnly? ProximaDoseAprazada);

/// <summary>Carteira de vacinacao do paciente (historico de doses).</summary>
/// <param name="PacienteId">Paciente titular.</param>
/// <param name="Doses">Doses aplicadas.</param>
public sealed record CarteiraVacinacaoDto(Guid PacienteId, IReadOnlyList<DoseAplicadaDto> Doses);

/// <summary>Aprazamento vencido (alvo de busca ativa).</summary>
/// <param name="PacienteId">Paciente.</param>
/// <param name="ImunobiologicoId">Imunobiologico.</param>
/// <param name="Sigla">Sigla do imunobiologico.</param>
/// <param name="UltimaDose">Numero da ultima dose aplicada.</param>
/// <param name="DataAprazada">Data aprazada (vencida).</param>
public sealed record AprazamentoVencidoDto(
    Guid PacienteId,
    Guid ImunobiologicoId,
    string Sigla,
    int UltimaDose,
    DateOnly DataAprazada);
