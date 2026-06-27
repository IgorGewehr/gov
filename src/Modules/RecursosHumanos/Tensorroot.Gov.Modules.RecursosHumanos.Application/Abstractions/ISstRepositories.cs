using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ExameOcupacional"/> (ASO — S-2220/PCMSO).</summary>
public interface IExameOcupacionalRepository
{
    /// <summary>Marca um novo ASO para insercao.</summary>
    /// <param name="exame">ASO a adicionar.</param>
    void Adicionar(ExameOcupacional exame);

    /// <summary>Obtem o ASO por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O ASO, ou <c>null</c>.</returns>
    Task<ExameOcupacional?> ObterPorIdAsync(ExameOcupacionalId id, CancellationToken cancellationToken);

    /// <summary>Lista os ASO de um servidor, do mais recente para o mais antigo (ficha de saude/PCMSO).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>ASO do servidor.</returns>
    Task<IReadOnlyList<ExameOcupacional>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os ASO cujo PROXIMO exame esta previsto ate a data de corte (agenda do PCMSO — NR-07):
    /// suporta o controle de vencimentos do programa de controle medico.
    /// </summary>
    /// <param name="ateData">Data de corte (proximo exame ate ela).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>ASO com proximo exame vencido/a vencer ate a data.</returns>
    Task<IReadOnlyList<ExameOcupacional>> ListarComProximoExameAteAsync(DateOnly ateData, CancellationToken cancellationToken);
}

/// <summary>Repositorio do agregado <see cref="ExposicaoAgenteNocivo"/> (S-2240/PPP).</summary>
public interface IExposicaoAgenteNocivoRepository
{
    /// <summary>Marca uma nova exposicao para insercao.</summary>
    /// <param name="exposicao">Exposicao a adicionar.</param>
    void Adicionar(ExposicaoAgenteNocivo exposicao);

    /// <summary>Obtem a exposicao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A exposicao, ou <c>null</c>.</returns>
    Task<ExposicaoAgenteNocivo?> ObterPorIdAsync(ExposicaoAgenteNocivoId id, CancellationToken cancellationToken);

    /// <summary>Lista as exposicoes de um servidor, da mais recente para a mais antiga (PPP).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Exposicoes do servidor.</returns>
    Task<IReadOnlyList<ExposicaoAgenteNocivo>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);
}

/// <summary>Repositorio do agregado <see cref="ComunicacaoAcidente"/> (CAT — S-2210).</summary>
public interface IComunicacaoAcidenteRepository
{
    /// <summary>Marca uma nova CAT para insercao.</summary>
    /// <param name="cat">CAT a adicionar.</param>
    void Adicionar(ComunicacaoAcidente cat);

    /// <summary>Obtem a CAT por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A CAT, ou <c>null</c>.</returns>
    Task<ComunicacaoAcidente?> ObterPorIdAsync(ComunicacaoAcidenteId id, CancellationToken cancellationToken);

    /// <summary>Lista as CAT de um servidor, da mais recente para a mais antiga.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>CAT do servidor.</returns>
    Task<IReadOnlyList<ComunicacaoAcidente>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);
}
