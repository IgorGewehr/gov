// Read models (projecoes de leitura) dos relatorios gerenciais da folha (ONDA3-DESIGN §4.1, sub-onda
// 3a). DTOs imutaveis projetados sobre os agregados existentes — sem dominio novo. Valores em BRL.
// FONTE no contexto RH = regime previdenciario (RPPS = efetivo/proprio; RGPS = celetista/INSS), o eixo
// que separa a despesa previdenciaria patronal do ente (EC 103/2019). UO/secretaria = unidade de lotacao
// do cargo (Lotacao.DenominacaoUnidade, mapeada a S-1020).
namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

/// <summary>Linha da folha agregada por secretaria/unidade de lotacao (UO).</summary>
/// <param name="Unidade">Denominacao da unidade/secretaria de lotacao (UO).</param>
/// <param name="QuantidadeServidores">Servidores distintos com lancamento na UO na competencia.</param>
/// <param name="TotalProventos">Soma dos proventos da UO.</param>
/// <param name="TotalDescontos">Soma dos descontos da UO.</param>
/// <param name="TotalLiquido">Liquido (proventos - descontos) da UO, nunca negativo.</param>
public sealed record LinhaFolhaSecretaria(
    string Unidade,
    int QuantidadeServidores,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido);

/// <summary>Linha da folha agregada por fonte (regime previdenciario RPPS/RGPS).</summary>
/// <param name="Fonte">Regime previdenciario (RPPS/RGPS) como rotulo da fonte.</param>
/// <param name="QuantidadeServidores">Servidores distintos com lancamento na fonte na competencia.</param>
/// <param name="TotalProventos">Soma dos proventos da fonte.</param>
/// <param name="TotalDescontos">Soma dos descontos da fonte.</param>
/// <param name="TotalLiquido">Liquido (proventos - descontos) da fonte.</param>
public sealed record LinhaFolhaFonte(
    string Fonte,
    int QuantidadeServidores,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido);

/// <summary>
/// Folha por secretaria/UO e por fonte numa competencia (read model — ONDA3-DESIGN §4.1).
/// </summary>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="SituacaoFolha">Situacao da folha mensal projetada.</param>
/// <param name="QuantidadeServidores">Servidores distintos com lancamento na competencia.</param>
/// <param name="TotalProventos">Total geral de proventos.</param>
/// <param name="TotalDescontos">Total geral de descontos.</param>
/// <param name="TotalLiquido">Total geral liquido.</param>
/// <param name="PorSecretaria">Quebra por secretaria/UO (decrescente por proventos).</param>
/// <param name="PorFonte">Quebra por fonte/regime (RPPS/RGPS).</param>
public sealed record FolhaPorSecretariaView(
    string Competencia,
    string SituacaoFolha,
    int QuantidadeServidores,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido,
    IReadOnlyList<LinhaFolhaSecretaria> PorSecretaria,
    IReadOnlyList<LinhaFolhaFonte> PorFonte);

/// <summary>Ponto da serie mensal da despesa de pessoal.</summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="Competencia">Competencia (<c>AAAA-MM</c>).</param>
/// <param name="SituacaoFolha">Situacao da folha mensal da competencia.</param>
/// <param name="QuantidadeServidores">Servidores distintos com lancamento na competencia.</param>
/// <param name="DespesaBruta">Despesa bruta de pessoal (soma dos proventos da folha mensal).</param>
/// <param name="TotalDescontos">Total de descontos da competencia.</param>
/// <param name="TotalLiquido">Liquido pago na competencia.</param>
public sealed record PontoEvolucaoDespesa(
    int Ano,
    int Mes,
    string Competencia,
    string SituacaoFolha,
    int QuantidadeServidores,
    decimal DespesaBruta,
    decimal TotalDescontos,
    decimal TotalLiquido);

/// <summary>
/// Evolucao mensal da despesa de pessoal num intervalo de competencias (read model — ONDA3-DESIGN §4.1).
/// Base gerencial para o acompanhamento do limite da LRF (art. 19/20); a RCL e fornecida fora deste modulo.
/// </summary>
/// <param name="CompetenciaInicial">Competencia inicial (<c>AAAA-MM</c>).</param>
/// <param name="CompetenciaFinal">Competencia final (<c>AAAA-MM</c>).</param>
/// <param name="DespesaBrutaAcumulada">Soma das despesas brutas no intervalo.</param>
/// <param name="DespesaBrutaMediaMensal">Media mensal da despesa bruta sobre os meses com folha.</param>
/// <param name="MesesComFolha">Quantidade de competencias com folha mensal no intervalo.</param>
/// <param name="Serie">Serie ordenada por competencia (crescente).</param>
public sealed record EvolucaoDespesaPessoalView(
    string CompetenciaInicial,
    string CompetenciaFinal,
    decimal DespesaBrutaAcumulada,
    decimal DespesaBrutaMediaMensal,
    int MesesComFolha,
    IReadOnlyList<PontoEvolucaoDespesa> Serie);

/// <summary>Linha do mapa de cargos (um cargo do quadro).</summary>
/// <param name="CargoId">Identificador do cargo.</param>
/// <param name="Denominacao">Denominacao legal do cargo.</param>
/// <param name="Tipo">Tipo (efetivo/comissionado/temporario).</param>
/// <param name="Situacao">Situacao do cargo (ativo/vago/extinto).</param>
/// <param name="Unidade">Unidade/secretaria de lotacao do cargo.</param>
/// <param name="Vencimento">Vencimento-base do cargo.</param>
/// <param name="VagasAutorizadas">Vagas autorizadas em lei.</param>
/// <param name="VagasOcupadas">Vagas providas (ocupadas).</param>
/// <param name="VagasDisponiveis">Vagas livres (autorizadas - ocupadas).</param>
public sealed record LinhaMapaCargo(
    Guid CargoId,
    string Denominacao,
    string Tipo,
    string Situacao,
    string Unidade,
    decimal Vencimento,
    int VagasAutorizadas,
    int VagasOcupadas,
    int VagasDisponiveis);

/// <summary>Totalizador do mapa de cargos por tipo de cargo.</summary>
/// <param name="Tipo">Tipo do cargo (efetivo/comissionado/temporario).</param>
/// <param name="VagasAutorizadas">Soma das vagas autorizadas do tipo.</param>
/// <param name="VagasOcupadas">Soma das vagas ocupadas do tipo.</param>
/// <param name="VagasDisponiveis">Soma das vagas livres do tipo.</param>
public sealed record TotalMapaCargoPorTipo(
    string Tipo,
    int VagasAutorizadas,
    int VagasOcupadas,
    int VagasDisponiveis);

/// <summary>
/// Mapa de cargos do quadro de pessoal (read model — ONDA3-DESIGN §4.1): ocupados x vagos por cargo,
/// com totalizadores por tipo e geral. Cargos extintos sao incluidos com marcacao de situacao.
/// </summary>
/// <param name="TotalVagasAutorizadas">Total geral de vagas autorizadas.</param>
/// <param name="TotalVagasOcupadas">Total geral de vagas ocupadas.</param>
/// <param name="TotalVagasDisponiveis">Total geral de vagas livres.</param>
/// <param name="PorTipo">Totalizadores por tipo de cargo.</param>
/// <param name="Cargos">Linhas detalhadas por cargo (decrescente por vagas ocupadas).</param>
public sealed record MapaCargosView(
    int TotalVagasAutorizadas,
    int TotalVagasOcupadas,
    int TotalVagasDisponiveis,
    IReadOnlyList<TotalMapaCargoPorTipo> PorTipo,
    IReadOnlyList<LinhaMapaCargo> Cargos);

/// <summary>Linha do demonstrativo TCE por fonte (regime previdenciario).</summary>
/// <param name="Fonte">Regime previdenciario (RPPS/RGPS).</param>
/// <param name="QuantidadeServidores">Servidores distintos da fonte na competencia.</param>
/// <param name="TotalProventos">Proventos da fonte.</param>
/// <param name="TotalDescontos">Descontos da fonte.</param>
/// <param name="TotalLiquido">Liquido da fonte.</param>
public sealed record DemonstrativoTceFonte(
    string Fonte,
    int QuantidadeServidores,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido);

/// <summary>Linha do demonstrativo TCE por secretaria/UO.</summary>
/// <param name="Unidade">Unidade/secretaria de lotacao.</param>
/// <param name="QuantidadeServidores">Servidores distintos da UO na competencia.</param>
/// <param name="TotalProventos">Proventos da UO.</param>
/// <param name="TotalDescontos">Descontos da UO.</param>
/// <param name="TotalLiquido">Liquido da UO.</param>
public sealed record DemonstrativoTceUnidade(
    string Unidade,
    int QuantidadeServidores,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido);

/// <summary>
/// Demonstrativo de despesa de pessoal para o Tribunal de Contas (read model — ONDA3-DESIGN §4.1):
/// totais gerais, por fonte (RPPS/RGPS) e por secretaria/UO de uma competencia, com a contribuicao
/// previdenciaria identificada (rubricas de desconto previdenciario). Visao gerencial; a remessa
/// formatada SIAPC/PAD e do modulo Transparencia/M4.
/// </summary>
/// <param name="Competencia">Competencia (<c>AAAA-MM</c>).</param>
/// <param name="SituacaoFolha">Situacao da folha mensal projetada.</param>
/// <param name="QuantidadeServidores">Servidores distintos com lancamento na competencia.</param>
/// <param name="TotalProventos">Total bruto de proventos (remuneracao bruta).</param>
/// <param name="TotalDescontos">Total de descontos.</param>
/// <param name="TotalLiquido">Total liquido.</param>
/// <param name="ContribuicaoPrevidenciariaSegurado">Soma das rubricas de desconto previdenciario (INSS/RPPS segurado).</param>
/// <param name="PorFonte">Quebra por fonte (regime).</param>
/// <param name="PorSecretaria">Quebra por secretaria/UO.</param>
public sealed record DemonstrativoTceView(
    string Competencia,
    string SituacaoFolha,
    int QuantidadeServidores,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido,
    decimal ContribuicaoPrevidenciariaSegurado,
    IReadOnlyList<DemonstrativoTceFonte> PorFonte,
    IReadOnlyList<DemonstrativoTceUnidade> PorSecretaria);
