using Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Consulta read-side dos relatorios gerenciais da folha (sub-onda 3a — ONDA3-DESIGN §4.1): projeta
/// sobre os agregados ja existentes (<c>FolhaDePagamento</c>/<c>EventoFolha</c>/<c>Servidor</c>/<c>Cargo</c>)
/// sem novo dominio. Respeita o Global Query Filter por tenant. Somente leitura (sem mutacao/comandos).
/// </summary>
public interface IRelatoriosFolhaConsulta
{
    /// <summary>
    /// Folha consolidada por secretaria/unidade de lotacao (UO) e por fonte (regime previdenciario
    /// RPPS/RGPS) numa competencia: proventos, descontos, liquido e numero de servidores por UO.
    /// </summary>
    /// <param name="ano">Ano da competencia.</param>
    /// <param name="mes">Mes da competencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Consolidado por secretaria/UO + fonte, ou <c>null</c> se nao houver folha mensal na competencia.</returns>
    Task<FolhaPorSecretariaView?> ObterFolhaPorSecretariaAsync(int ano, int mes, CancellationToken cancellationToken);

    /// <summary>
    /// Serie mensal da despesa de pessoal (folha mensal) entre duas competencias: proventos, descontos,
    /// liquido e despesa bruta (proventos) por mes — base para o acompanhamento do limite de despesa com
    /// pessoal (LRF art. 19/20 — 54% da RCL no Executivo municipal; a RCL e fornecida fora deste modulo).
    /// </summary>
    /// <param name="anoDe">Ano da competencia inicial.</param>
    /// <param name="mesDe">Mes da competencia inicial.</param>
    /// <param name="anoAte">Ano da competencia final.</param>
    /// <param name="mesAte">Mes da competencia final.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Serie mensal da despesa de pessoal no intervalo (ordenada por competencia).</returns>
    Task<EvolucaoDespesaPessoalView> ObterEvolucaoDespesaAsync(
        int anoDe, int mesDe, int anoAte, int mesAte, CancellationToken cancellationToken);

    /// <summary>
    /// Mapa de cargos do quadro de pessoal: vagas autorizadas, ocupadas e vagas (livres) por cargo,
    /// com totalizadores por tipo de cargo (efetivo/comissionado/temporario) e geral.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Mapa de cargos do tenant.</returns>
    Task<MapaCargosView> ObterMapaCargosAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Demonstrativo de despesa de pessoal para o Tribunal de Contas (TCE) numa competencia: totais por
    /// fonte (regime RPPS/RGPS) e por secretaria/UO, quantitativo de servidores e contribuicao
    /// previdenciaria (patronal/segurado quando lancadas). Visao gerencial; a remessa formatada (SIAPC/PAD)
    /// e do modulo Transparencia/M4.
    /// </summary>
    /// <param name="ano">Ano da competencia.</param>
    /// <param name="mes">Mes da competencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Demonstrativo da competencia, ou <c>null</c> se nao houver folha mensal na competencia.</returns>
    Task<DemonstrativoTceView?> ObterDemonstrativoTceAsync(int ano, int mes, CancellationToken cancellationToken);
}
