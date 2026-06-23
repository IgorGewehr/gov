using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using AgendamentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Agendamento.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="AgendamentoRaiz"/> (sempre tenant-scoped via Global Query Filter).
/// </summary>
public interface IAgendamentoRepository
{
    /// <summary>Marca um novo agendamento para insercao.</summary>
    /// <param name="agendamento">Agendamento a adicionar.</param>
    void Adicionar(AgendamentoRaiz agendamento);

    /// <summary>Obtem um agendamento por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O agendamento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<AgendamentoRaiz?> ObterPorIdAsync(AgendamentoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se o paciente ja possui um agendamento ATIVO (Marcado/Confirmado) no mesmo instante de
    /// atendimento (anti duplo-agendamento do paciente no mesmo slot). Tenant-scoped.
    /// </summary>
    /// <param name="pacienteId">Paciente.</param>
    /// <param name="dataHora">Data/hora do atendimento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver conflito; caso contrario, <c>false</c>.</returns>
    Task<bool> PacienteTemConflitoAsync(PacienteId pacienteId, DateTimeOffset dataHora, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de agendamentos por paciente/profissional/data/situacao. Tenant-scoped via Global
    /// Query Filter. Ordena por data/hora do atendimento.
    /// </summary>
    /// <param name="pacienteId">Filtro opcional por paciente.</param>
    /// <param name="profissionalId">Filtro opcional por profissional.</param>
    /// <param name="data">Filtro opcional por data do atendimento.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de agendamentos e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<AgendamentoRaiz> Itens, int Total)> BuscarAsync(
        PacienteId? pacienteId,
        ProfissionalId? profissionalId,
        DateOnly? data,
        SituacaoAgendamento? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
