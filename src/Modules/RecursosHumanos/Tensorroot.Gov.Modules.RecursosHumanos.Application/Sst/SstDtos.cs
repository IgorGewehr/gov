using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

/// <summary>Item de agente nocivo na entrada de registro de exposicao (S-2240/PPP).</summary>
/// <param name="Codigo">Codigo do agente (Tabela 23).</param>
/// <param name="Descricao">Descricao/atividade.</param>
/// <param name="Intensidade">Intensidade/concentracao medida (opcional).</param>
/// <param name="UnidadeMedida">Unidade da intensidade (opcional).</param>
/// <param name="UtilizaEpc">Uso de EPC eficaz.</param>
/// <param name="UtilizaEpi">Uso de EPI eficaz.</param>
public sealed record AgenteNocivoDto(
    string Codigo,
    string Descricao,
    decimal? Intensidade,
    string? UnidadeMedida,
    bool UtilizaEpc,
    bool UtilizaEpi);

/// <summary>Projecao de leitura de um ASO (ficha de saude ocupacional/PCMSO).</summary>
/// <param name="Id">Identificador do ASO.</param>
/// <param name="ServidorId">Servidor examinado.</param>
/// <param name="Tipo">Tipo do exame.</param>
/// <param name="DataExame">Data de realizacao.</param>
/// <param name="Resultado">Resultado/aptidao.</param>
/// <param name="MedicoNome">Nome do medico responsavel.</param>
/// <param name="MedicoCrm">CRM/UF do medico.</param>
/// <param name="DataProximoExame">Data prevista do proximo exame.</param>
/// <param name="ExamesComplementares">Codigos dos exames complementares.</param>
/// <param name="Situacao">Situacao do registro.</param>
public sealed record ExameOcupacionalView(
    Guid Id,
    Guid ServidorId,
    TipoExameOcupacional Tipo,
    DateOnly DataExame,
    ResultadoAso Resultado,
    string MedicoNome,
    string MedicoCrm,
    DateOnly? DataProximoExame,
    IReadOnlyList<string> ExamesComplementares,
    SituacaoRegistroSst Situacao);

/// <summary>Projecao de leitura de uma exposicao a agentes nocivos (PPP/S-2240).</summary>
/// <param name="Id">Identificador da exposicao.</param>
/// <param name="ServidorId">Servidor exposto.</param>
/// <param name="InicioExposicao">Inicio do periodo.</param>
/// <param name="FimExposicao">Fim do periodo (nulo quando vigente).</param>
/// <param name="SetorAtividade">Setor/atividade.</param>
/// <param name="Agentes">Agentes nocivos do periodo.</param>
/// <param name="Situacao">Situacao do registro.</param>
public sealed record ExposicaoAgenteNocivoView(
    Guid Id,
    Guid ServidorId,
    DateOnly InicioExposicao,
    DateOnly? FimExposicao,
    string SetorAtividade,
    IReadOnlyList<AgenteNocivoDto> Agentes,
    SituacaoRegistroSst Situacao);

/// <summary>Projecao de leitura de uma CAT (S-2210).</summary>
/// <param name="Id">Identificador da CAT.</param>
/// <param name="ServidorId">Servidor acidentado.</param>
/// <param name="TipoCat">Tipo da CAT.</param>
/// <param name="TipoAcidente">Tipo do acidente.</param>
/// <param name="DataHoraAcidente">Data/hora do acidente.</param>
/// <param name="HouveObito">Indica obito.</param>
/// <param name="DescricaoSituacao">Descricao da situacao geradora.</param>
/// <param name="Cid">CID-10.</param>
/// <param name="Situacao">Situacao do registro.</param>
public sealed record ComunicacaoAcidenteView(
    Guid Id,
    Guid ServidorId,
    TipoCat TipoCat,
    TipoAcidente TipoAcidente,
    DateTimeOffset DataHoraAcidente,
    bool HouveObito,
    string DescricaoSituacao,
    string? Cid,
    SituacaoRegistroSst Situacao);

/// <summary>
/// Perfil Profissiografico Previdenciario (PPP — IN INSS 128/2022) consolidado de um servidor: dados do
/// trabalhador + registros ambientais (exposicao a agentes nocivos por periodo) + monitoracao biologica
/// (historico de ASO). Documento que comprova o direito a aposentadoria especial; gerado a partir dos
/// agregados de SST (S-2240 + S-2220). // TODO(validar-oficial): formato exato do PPP eletronico (IN 128).
/// </summary>
/// <param name="ServidorId">Servidor titular do perfil.</param>
/// <param name="Nome">Nome do servidor.</param>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="Exposicoes">Registros ambientais (exposicao a agentes nocivos).</param>
/// <param name="MonitoracaoBiologica">Historico de ASO (monitoracao biologica).</param>
/// <param name="PossuiExposicaoEspecial">Indica se ha periodo com agente nocivo que enseja aposentadoria especial.</param>
public sealed record PerfilProfissiograficoView(
    Guid ServidorId,
    string Nome,
    string Matricula,
    IReadOnlyList<ExposicaoAgenteNocivoView> Exposicoes,
    IReadOnlyList<ExameOcupacionalView> MonitoracaoBiologica,
    bool PossuiExposicaoEspecial);
