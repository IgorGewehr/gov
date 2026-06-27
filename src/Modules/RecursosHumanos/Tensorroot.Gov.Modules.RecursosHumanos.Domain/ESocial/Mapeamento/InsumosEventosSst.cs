namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>Identificacao do trabalhador+vinculo (<c>ideVinculo</c>) presente nos eventos nao-periodicos de SST (S-2210/S-2220/S-2240).</summary>
/// <param name="CpfTrab">CPF do trabalhador (sem mascara).</param>
/// <param name="Matricula">Matricula do vinculo.</param>
public sealed record IdeVinculoSst(string CpfTrab, string Matricula);

/// <summary>Um agente nocivo (item do grupo <c>agNoc</c> do S-2240).</summary>
/// <param name="CodAgNoc">Codigo do agente nocivo (Tabela 23). // TODO(validar-oficial).</param>
/// <param name="Descricao">Descricao/atividade.</param>
/// <param name="Intensidade">Intensidade/concentracao medida (opcional).</param>
/// <param name="UnidadeMedida">Unidade da intensidade (opcional).</param>
/// <param name="UtilizaEpc">Uso de EPC eficaz.</param>
/// <param name="UtilizaEpi">Uso de EPI eficaz.</param>
public sealed record ItemAgenteNocivo(
    string CodAgNoc,
    string Descricao,
    decimal? Intensidade,
    string? UnidadeMedida,
    bool UtilizaEpc,
    bool UtilizaEpi);

/// <summary>
/// Snapshot de uma CAT para o S-2210. Origem: agregado <see cref="Sst.ComunicacaoAcidente"/>.
/// // TODO(validar-oficial): grupo <c>cat</c> (Tabelas 25/26/13) e <c>nrRecCatOrig</c> no XSD travado.
/// </summary>
/// <param name="Empregador">Inscricao do empregador (<c>ideEmpregador</c>).</param>
/// <param name="Vinculo">Identificacao do trabalhador+vinculo.</param>
/// <param name="DataAcidente">Data do acidente.</param>
/// <param name="HoraAcidente">Hora do acidente (<c>HHMM</c>).</param>
/// <param name="TpAcid">Tipo do acidente (1=tipico,2=doenca,3=trajeto). // TODO(validar-oficial).</param>
/// <param name="TpCat">Tipo da CAT (1=inicial,2=reabertura,3=obito).</param>
/// <param name="IndCatObito">Indica obito (<c>S</c>/<c>N</c>).</param>
/// <param name="DataObito">Data do obito (<c>AAAA-MM-DD</c>) quando houve; nula caso contrario.</param>
/// <param name="DescricaoSituacao">Descricao da situacao geradora.</param>
/// <param name="Cid">CID-10 (opcional).</param>
/// <param name="ParteCorpoAtingida">Parte do corpo atingida (opcional).</param>
/// <param name="AgenteCausador">Agente causador (opcional).</param>
/// <param name="NrRecCatOrig">Recibo/numero da CAT de origem (reabertura/obito); nulo na inicial.</param>
public sealed record InsumoS2210(
    InscricaoEmpregador Empregador,
    IdeVinculoSst Vinculo,
    DateOnly DataAcidente,
    string HoraAcidente,
    int TpAcid,
    int TpCat,
    string IndCatObito,
    DateOnly? DataObito,
    string DescricaoSituacao,
    string? Cid,
    string? ParteCorpoAtingida,
    string? AgenteCausador,
    string? NrRecCatOrig);

/// <summary>
/// Snapshot de um ASO para o S-2220 (Monitoramento da Saude). Origem: agregado <see cref="Sst.ExameOcupacional"/>.
/// // TODO(validar-oficial): grupos <c>exMedOcup</c>/<c>aso</c>/<c>exameComp</c> (Tabela 27) no XSD travado.
/// </summary>
/// <param name="Empregador">Inscricao do empregador (<c>ideEmpregador</c>).</param>
/// <param name="Vinculo">Identificacao do trabalhador+vinculo.</param>
/// <param name="TpExameOcup">Tipo do exame (0=adm,1=per,2=ret,3=mud,4=monit,9=dem). // TODO(validar-oficial).</param>
/// <param name="DataAso">Data do ASO (<c>AAAA-MM-DD</c>).</param>
/// <param name="ResAso">Resultado (1=apto,2=inapto). // TODO(validar-oficial).</param>
/// <param name="ExamesComplementares">Codigos dos exames complementares (Tabela 27).</param>
/// <param name="MedicoNome">Nome do medico responsavel pelo ASO.</param>
/// <param name="MedicoNrCrm">Numero do CRM.</param>
/// <param name="MedicoUfCrm">UF do CRM.</param>
public sealed record InsumoS2220(
    InscricaoEmpregador Empregador,
    IdeVinculoSst Vinculo,
    int TpExameOcup,
    DateOnly DataAso,
    int ResAso,
    IReadOnlyList<string> ExamesComplementares,
    string MedicoNome,
    string MedicoNrCrm,
    string MedicoUfCrm);

/// <summary>
/// Snapshot das condicoes ambientais (S-2240) — agentes nocivos. Origem: agregado
/// <see cref="Sst.ExposicaoAgenteNocivo"/>. // TODO(validar-oficial): grupos <c>infoExpRisco</c>/<c>agNoc</c>
/// e responsavel pelos registros ambientais no XSD travado.
/// </summary>
/// <param name="Empregador">Inscricao do empregador (<c>ideEmpregador</c>).</param>
/// <param name="Vinculo">Identificacao do trabalhador+vinculo.</param>
/// <param name="InicioCondicao">Inicio do periodo de exposicao (<c>AAAA-MM-DD</c>).</param>
/// <param name="FimCondicao">Fim do periodo (<c>AAAA-MM-DD</c>); nulo quando vigente.</param>
/// <param name="SetorAtividade">Setor/descricao das atividades.</param>
/// <param name="Agentes">Agentes nocivos do periodo.</param>
public sealed record InsumoS2240(
    InscricaoEmpregador Empregador,
    IdeVinculoSst Vinculo,
    DateOnly InicioCondicao,
    DateOnly? FimCondicao,
    string SetorAtividade,
    IReadOnlyList<ItemAgenteNocivo> Agentes);
