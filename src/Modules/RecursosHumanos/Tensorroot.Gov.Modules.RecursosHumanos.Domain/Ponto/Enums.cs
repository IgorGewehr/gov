namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>
/// Tipo de Registrador Eletronico de Ponto (Portaria MTP 671/2021, art. 75 e ss.). Determina o regime
/// de comprovacao/assinatura aplicavel a marcacao.
/// </summary>
public enum TipoRep
{
    /// <summary>REP-C (Convencional): hardware que imprime comprovante; exige homologacao/INMETRO.</summary>
    RepC = 1,

    /// <summary>REP-A (Alternativo): software/hardware/hibrido mediante autorizacao em ACT/CCT.</summary>
    RepA = 2,

    /// <summary>REP-P (por Programa): software (inclusive mobile); registro INPI, sem homologacao do Ministerio.</summary>
    RepP = 3,
}

/// <summary>
/// Sentido (direcao) de uma marcacao de ponto. Mantido simples (entrada/saida); a apuracao pareia
/// entradas e saidas para compor periodos trabalhados.
/// </summary>
public enum SentidoMarcacao
{
    /// <summary>Marcacao de entrada (inicio de periodo trabalhado).</summary>
    Entrada = 1,

    /// <summary>Marcacao de saida (fim de periodo trabalhado).</summary>
    Saida = 2,
}

/// <summary>
/// Tipo de registro do AFD (Arquivo Fonte de Dados — Portaria MTP 671/2021).
/// // TODO(validar-oficial): confirmar a tabela exata de tipos e seus codigos no Anexo do AFD da
/// Portaria 671 (texto integral DOU). As fontes secundarias citam "tipos 1-5/7/9" porem o leiaute
/// oficial NAO foi obtido; os codigos abaixo seguem o CONCEITO (cabecalho/marcacao/ajuste/trailer).
/// </summary>
public enum TipoRegistroAfd
{
    /// <summary>Tipo 1 — Cabecalho/identificacao do empregador. // TODO(validar-oficial: codigo/posicoes).</summary>
    Cabecalho = 1,

    /// <summary>Tipo 2 — Inclusao/alteracao de empresa no REP. // TODO(validar-oficial).</summary>
    IdentificacaoEmpregador = 2,

    /// <summary>
    /// Tipo 3 — Marcacao de ponto de REP-C/REP-A. // TODO(validar-oficial: codigo real do registro de
    /// marcacao). Fontes secundarias (TOTVS/Alterdata) indicam o leiaute vigente desde 10/02/2022 com
    /// CPF (12 chars, posicoes 035-046) — confirmar no Anexo oficial (DOU).
    /// </summary>
    Marcacao = 3,

    /// <summary>Tipo 4 — Ajuste do relogio de tempo real (RTC). // TODO(validar-oficial).</summary>
    AjusteRelogio = 4,

    /// <summary>Tipo 5 — Inclusao/alteracao/exclusao de empregado. // TODO(validar-oficial).</summary>
    Empregado = 5,

    /// <summary>
    /// Tipo 7 — Marcacao de ponto de REP-P (por programa). O leiaute 671 distingue a marcacao de REP-C/A
    /// (tipo 3) da marcacao de REP-P (tipo 7). // TODO(validar-oficial): posicoes/larguras integrais no
    /// Anexo oficial (CPF 035-046, 12 chars). O parser e o GeradorAfd selecionam 3 vs 7 conforme a origem.
    /// </summary>
    MarcacaoRepP = 7,

    /// <summary>Tipo 9 — Trailer/totalizador (encerramento do arquivo). // TODO(validar-oficial).</summary>
    Trailer = 9,
}

/// <summary>
/// Modo de coleta/transporte pelo qual o AFD chegou do equipamento REP ate o pipeline de ingestao.
/// Como [Flags] porque um driver de fabricante pode suportar mais de um transporte. // TODO(prod):
/// confirmar os protocolos proprietarios de cada fabricante (Control iD/Henry/Madis/Topdata/Dimep).
/// </summary>
[Flags]
public enum ModoColeta
{
    /// <summary>Nenhum modo (valor neutro).</summary>
    Nenhum = 0,

    /// <summary>Importacao de arquivo AFD ja exportado (upload pela UI ou pendrive da porta fiscal).</summary>
    Arquivo = 1,

    /// <summary>Coleta TCP/SDK proprietario na rede do cliente (on-premise; exige agente local).</summary>
    TcpSdk = 2,

    /// <summary>Coleta REST/cloud sem agente local (Control iD iDCloud, Dimep REST).</summary>
    RestCloud = 4,
}

/// <summary>
/// Fabricante/driver do REP (equipamento de ponto). Determina qual <c>IColetorRep</c> coleta o AFD.
/// O leiaute do AFD (Port. 671) e universal; o protocolo de coleta e proprietario por fabricante.
/// </summary>
public enum MarcaRep
{
    /// <summary>Driver universal de importacao de arquivo AFD (qualquer REP-C/A/P; upload/pendrive).</summary>
    ArquivoAfd = 1,

    /// <summary>Control iD (REST get_afd.fcgi / iDCloud) — // TODO(prod: SDK proprietario).</summary>
    ControlId = 2,

    /// <summary>Henry (TCP/Serial/USB, filtro por NSR) — // TODO(prod: SDK proprietario).</summary>
    Henry = 3,

    /// <summary>Madis (pendrive AFD + SDK) — // TODO(prod: SDK proprietario).</summary>
    Madis = 4,

    /// <summary>Topdata (SDK Inner REP, DLL TCP) — // TODO(prod: SDK proprietario).</summary>
    Topdata = 5,

    /// <summary>Dimep (REST / Protocolo VIII) — // TODO(prod: SDK proprietario).</summary>
    Dimep = 6,

    /// <summary>Coletor online SIMULADO (gera um AFD de exemplo) — para testes/demonstracao do pipeline.</summary>
    Simulado = 99,
}

/// <summary>Situacao (estado) de um lote de marcacoes/competencia de ponto apurada.</summary>
public enum SituacaoApuracaoPonto
{
    /// <summary>Apuracao em rascunho, ainda recalculavel.</summary>
    Aberta = 1,

    /// <summary>Apuracao fechada (espelho/AEJ congelados, prontos para folha).</summary>
    Fechada = 2,
}

/// <summary>
/// Aplicabilidade da Portaria 671 a categoria do vinculo. A Portaria regulamenta a CLT (celetistas);
/// para o estatutario o controle de jornada e regido por lei municipal/RJU e normas do TCE-RS, sendo
/// // TODO(validar-oficial) parametrizavel por tenant (vide pesquisa-ponto-671 §6).
/// </summary>
public enum RegimeJornada
{
    /// <summary>Estatutario (RJU/lei municipal) — Portaria 671 nao e, por si, a norma aplicavel.</summary>
    Estatutario = 1,

    /// <summary>Celetista (CLT) — Portaria 671 aplicavel.</summary>
    Celetista = 2,
}
