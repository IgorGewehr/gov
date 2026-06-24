using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using AtendimentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="AtendimentoRaiz"/>.</summary>
public interface IAtendimentoRepository
{
    /// <summary>Marca um novo atendimento para insercao.</summary>
    /// <param name="atendimento">Atendimento a adicionar.</param>
    void Adicionar(AtendimentoRaiz atendimento);

    /// <summary>Obtem um atendimento por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O atendimento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<AtendimentoRaiz?> ObterPorIdAsync(AtendimentoId id, CancellationToken cancellationToken);

    /// <summary>Lista os atendimentos de um paciente no tenant, opcionalmente filtrados por intervalo.</summary>
    /// <param name="pacienteId">Paciente.</param>
    /// <param name="de">Data inicial (opcional).</param>
    /// <param name="ate">Data final (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Atendimentos do paciente no tenant.</returns>
    Task<IReadOnlyList<AtendimentoRaiz>> ListarPorPacienteAsync(
        PacienteId pacienteId,
        DateOnly? de,
        DateOnly? ate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se a prescricao informada EXISTE no tenant e PERTENCE ao paciente indicado (a prescricao
    /// e entidade do agregado <see cref="AtendimentoRaiz"/>). Usado para validar — fail-closed — a
    /// retencao de receita na dispensacao de medicamento controlado (Portaria 344/1998 / SNGPC).
    /// </summary>
    /// <param name="prescricaoId">Prescricao de origem a validar.</param>
    /// <param name="pacienteId">Paciente que retira o medicamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a prescricao existe e e do paciente; caso contrario <c>false</c>.</returns>
    Task<bool> PrescricaoPertenceAoPacienteAsync(
        PrescricaoId prescricaoId,
        PacienteId pacienteId,
        CancellationToken cancellationToken);
}
