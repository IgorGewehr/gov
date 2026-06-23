using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio do catalogo de imunobiologicos (sempre tenant-scoped via Global Query Filter).</summary>
public interface IImunobiologicoRepository
{
    /// <summary>Marca um novo imunobiologico para insercao.</summary>
    /// <param name="imunobiologico">Imunobiologico a adicionar.</param>
    void Adicionar(Imunobiologico imunobiologico);

    /// <summary>Obtem um imunobiologico por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O imunobiologico, ou <c>null</c>.</returns>
    Task<Imunobiologico?> ObterPorIdAsync(ImunobiologicoId id, CancellationToken cancellationToken);

    /// <summary>Lista os imunobiologicos do catalogo (ativos).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Imunobiologicos ativos.</returns>
    Task<IReadOnlyList<Imunobiologico>> ListarAtivosAsync(CancellationToken cancellationToken);
}

/// <summary>Repositorio das carteiras de vacinacao (1-1 por paciente, sempre tenant-scoped).</summary>
public interface ICarteiraVacinacaoRepository
{
    /// <summary>Marca uma nova carteira para insercao.</summary>
    /// <param name="carteira">Carteira a adicionar.</param>
    void Adicionar(CarteiraVacinacao carteira);

    /// <summary>Obtem a carteira de um paciente (com doses), ou <c>null</c> se ainda nao aberta.</summary>
    /// <param name="pacienteId">Paciente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A carteira, ou <c>null</c>.</returns>
    Task<CarteiraVacinacao?> ObterPorPacienteAsync(PacienteId pacienteId, CancellationToken cancellationToken);

    /// <summary>Lista carteiras que possuem aprazamentos vencidos ate a data (busca ativa).</summary>
    /// <param name="ate">Data limite (hoje).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Carteiras com aprazamento vencido.</returns>
    Task<IReadOnlyList<CarteiraVacinacao>> ListarComAprazamentoVencidoAsync(DateOnly ate, CancellationToken cancellationToken);
}
