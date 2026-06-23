using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio do catalogo de medicamentos (sempre tenant-scoped via Global Query Filter).</summary>
public interface IMedicamentoRepository
{
    /// <summary>Marca um novo medicamento para insercao.</summary>
    /// <param name="medicamento">Medicamento a adicionar.</param>
    void Adicionar(Medicamento medicamento);

    /// <summary>Obtem um medicamento por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O medicamento, ou <c>null</c>.</returns>
    Task<Medicamento?> ObterPorIdAsync(MedicamentoId id, CancellationToken cancellationToken);

    /// <summary>Busca paginada de medicamentos por principio ativo/apresentacao.</summary>
    /// <param name="termo">Termo livre (principio ativo/apresentacao); nulo lista tudo.</param>
    /// <param name="apenasAtivos">Quando verdadeiro, filtra apenas itens ativos.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de medicamentos e o total.</returns>
    Task<(IReadOnlyList<Medicamento> Itens, int Total)> BuscarAsync(
        string? termo, bool apenasAtivos, int pagina, int tamanho, CancellationToken cancellationToken);
}

/// <summary>Repositorio do estoque de medicamentos por estabelecimento (sempre tenant-scoped).</summary>
public interface IEstoqueMedicamentoRepository
{
    /// <summary>Marca uma nova posicao de estoque para insercao.</summary>
    /// <param name="estoque">Estoque a adicionar.</param>
    void Adicionar(EstoqueMedicamento estoque);

    /// <summary>Obtem a posicao de estoque por identificador (com lotes).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O estoque, ou <c>null</c>.</returns>
    Task<EstoqueMedicamento?> ObterPorIdAsync(EstoqueMedicamentoId id, CancellationToken cancellationToken);

    /// <summary>Obtem a posicao de estoque de um medicamento num estabelecimento (com lotes).</summary>
    /// <param name="estabelecimentoId">Estabelecimento.</param>
    /// <param name="medicamentoId">Medicamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O estoque, ou <c>null</c> se ainda nao aberto.</returns>
    Task<EstoqueMedicamento?> ObterPorEstabelecimentoEMedicamentoAsync(
        EstabelecimentoId estabelecimentoId, MedicamentoId medicamentoId, CancellationToken cancellationToken);

    /// <summary>Lista as posicoes de estoque de um estabelecimento (posicao de estoque/relatorio).</summary>
    /// <param name="estabelecimentoId">Estabelecimento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Estoques do estabelecimento.</returns>
    Task<IReadOnlyList<EstoqueMedicamento>> ListarPorEstabelecimentoAsync(
        EstabelecimentoId estabelecimentoId, CancellationToken cancellationToken);

    /// <summary>Lista os estoques com lotes que vencem ate a data informada (alerta de validade).</summary>
    /// <param name="ate">Data limite de validade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Estoques com lotes a vencer.</returns>
    Task<IReadOnlyList<EstoqueMedicamento>> ListarComLotesAVencerAsync(DateOnly ate, CancellationToken cancellationToken);
}

/// <summary>Repositorio das dispensacoes ao paciente (sempre tenant-scoped).</summary>
public interface IDispensacaoRepository
{
    /// <summary>Marca uma nova dispensacao para insercao.</summary>
    /// <param name="dispensacao">Dispensacao a adicionar.</param>
    void Adicionar(Dispensacao dispensacao);

    /// <summary>Obtem uma dispensacao por identificador (com itens).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A dispensacao, ou <c>null</c>.</returns>
    Task<Dispensacao?> ObterPorIdAsync(DispensacaoId id, CancellationToken cancellationToken);

    /// <summary>Lista o historico de dispensacoes de um paciente (trilha LGPD).</summary>
    /// <param name="pacienteId">Paciente.</param>
    /// <param name="de">Data inicial (opcional).</param>
    /// <param name="ate">Data final (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dispensacoes do paciente, mais recentes primeiro.</returns>
    Task<IReadOnlyList<Dispensacao>> ListarPorPacienteAsync(
        Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId pacienteId,
        DateOnly? de, DateOnly? ate, CancellationToken cancellationToken);
}
