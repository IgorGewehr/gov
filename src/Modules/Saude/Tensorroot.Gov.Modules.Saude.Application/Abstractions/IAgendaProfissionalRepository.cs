using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Item de vaga LIVRE projetado para a consulta de disponibilidade (grade → slots marcaveis).
/// </summary>
/// <param name="AgendaId">Agenda dona da vaga.</param>
/// <param name="VagaId">Identificador da vaga.</param>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="Tipo">Natureza (consulta/exame).</param>
/// <param name="DataHora">Data/hora do slot.</param>
public sealed record VagaLivre(
    Guid AgendaId,
    Guid VagaId,
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    TipoAtendimentoAgenda Tipo,
    DateTimeOffset DataHora);

/// <summary>
/// Repositorio do agregado <see cref="AgendaProfissional"/> (grade + vagas owned; sempre tenant-scoped
/// via Global Query Filter). Fronteira de consistencia da concorrencia de marcacao (anti-overbooking).
/// </summary>
public interface IAgendaProfissionalRepository
{
    /// <summary>Marca uma nova agenda para insercao.</summary>
    /// <param name="agenda">Agenda a adicionar.</param>
    void Adicionar(AgendaProfissional agenda);

    /// <summary>Obtem uma agenda por identificador (com suas vagas; respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A agenda, ou <c>null</c> se inexistente no tenant.</returns>
    Task<AgendaProfissional?> ObterPorIdAsync(AgendaProfissionalId id, CancellationToken cancellationToken);

    /// <summary>Obtem a agenda que contem a vaga informada (com suas vagas; respeitando o filtro de tenant).</summary>
    /// <param name="vagaId">Identificador da vaga.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A agenda dona da vaga, ou <c>null</c> se inexistente no tenant.</returns>
    Task<AgendaProfissional?> ObterPorVagaAsync(VagaId vagaId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as vagas LIVRES por profissional/estabelecimento/tipo dentro de uma janela de datas
    /// (consulta de disponibilidade). Tenant-scoped via Global Query Filter; ordena por data/hora.
    /// </summary>
    /// <param name="profissionalId">Filtro opcional por profissional.</param>
    /// <param name="estabelecimentoId">Filtro opcional por estabelecimento.</param>
    /// <param name="de">Inicio da janela (inclusivo).</param>
    /// <param name="ate">Fim da janela (inclusivo).</param>
    /// <param name="tipo">Filtro opcional por natureza.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Vagas livres no periodo.</returns>
    Task<IReadOnlyList<VagaLivre>> BuscarVagasLivresAsync(
        ProfissionalId? profissionalId,
        EstabelecimentoId? estabelecimentoId,
        DateOnly? de,
        DateOnly? ate,
        TipoAtendimentoAgenda? tipo,
        CancellationToken cancellationToken);
}
