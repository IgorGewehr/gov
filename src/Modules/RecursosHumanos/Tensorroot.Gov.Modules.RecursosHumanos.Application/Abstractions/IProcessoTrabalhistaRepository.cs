using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ProcessoTrabalhista"/> (processos trabalhistas do ente).</summary>
public interface IProcessoTrabalhistaRepository
{
    /// <summary>Marca um novo processo para insercao.</summary>
    /// <param name="processo">Processo a adicionar.</param>
    void Adicionar(ProcessoTrabalhista processo);

    /// <summary>Obtem um processo por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O processo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ProcessoTrabalhista?> ObterPorIdAsync(ProcessoTrabalhistaId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe processo com o numero informado no tenant (unicidade).</summary>
    /// <param name="numeroProcesso">Numero do processo (CNJ).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe.</returns>
    Task<bool> ExisteNumeroAsync(string numeroProcesso, CancellationToken cancellationToken);

    /// <summary>Busca paginada de processos por situacao/prognostico (navegabilidade).</summary>
    /// <param name="situacao">Filtro opcional por situacao processual.</param>
    /// <param name="prognostico">Filtro opcional por prognostico de perda.</param>
    /// <param name="termo">Filtro opcional por numero/reclamante (contem).</param>
    /// <param name="pagina">Pagina (1-based).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens da pagina e total filtrado.</returns>
    Task<(IReadOnlyList<ProcessoTrabalhista> Itens, int Total)> BuscarAsync(
        SituacaoProcessoTrabalhista? situacao,
        PrognosticoPerda? prognostico,
        string? termo,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    /// <summary>
    /// Soma o valor PROVISIONADO dos processos em andamento com prognostico provavel (passivo
    /// reconhecido) — base do demonstrativo de provisoes trabalhistas (NBC TG 25).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Total provisionado vigente no tenant.</returns>
    Task<decimal> SomarProvisaoVigenteAsync(CancellationToken cancellationToken);
}
