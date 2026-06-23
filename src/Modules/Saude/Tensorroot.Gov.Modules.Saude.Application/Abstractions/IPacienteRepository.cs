using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Paciente"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IPacienteRepository
{
    /// <summary>Marca um novo paciente para insercao.</summary>
    /// <param name="paciente">Paciente a adicionar.</param>
    void Adicionar(Paciente paciente);

    /// <summary>Obtem um paciente por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O paciente, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Paciente?> ObterPorIdAsync(PacienteId id, CancellationToken cancellationToken);

    /// <summary>Obtem um paciente pelo CNS (respeitando o filtro de tenant).</summary>
    /// <param name="cns">Cartao Nacional de Saude.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O paciente, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Paciente?> ObterPorCnsAsync(Cns cns, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe um paciente com o CNS informado no tenant atual (I-9).</summary>
    /// <param name="cns">Cartao Nacional de Saude.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir; caso contrario, <c>false</c>.</returns>
    Task<bool> ExistePorCnsAsync(Cns cns, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de pacientes por nome/CNS/CPF (navegabilidade — Onda 0), com filtro opcional por
    /// situacao. Tenant-scoped via Global Query Filter. O nome casa por trecho (case/acento-insensivel);
    /// CNS/CPF casam por digitos. Ordena por nome. Os parametros sao normalizados na implementacao.
    /// </summary>
    /// <param name="termo">Termo livre (nome, CNS ou CPF); nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situacao do cadastro.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de pacientes e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Paciente> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoPaciente? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
