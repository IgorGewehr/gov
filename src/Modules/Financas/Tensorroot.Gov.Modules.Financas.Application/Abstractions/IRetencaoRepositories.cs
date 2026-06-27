using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório da tabela de IRRF sobre serviços/PJ (IN RFB 1.234/2012).</summary>
public interface ITabelaIrrfServicosRepository
{
    /// <summary>Marca uma nova tabela para inserção.</summary>
    /// <param name="tabela">Tabela a adicionar.</param>
    void Adicionar(TabelaIrrfServicos tabela);

    /// <summary>Obtém a tabela vigente na data informada.</summary>
    /// <param name="data">Data de referência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela vigente, ou <c>null</c>.</returns>
    Task<TabelaIrrfServicos?> ObterVigenteAsync(DateOnly data, CancellationToken cancellationToken);

    /// <summary>Lista todas as tabelas do tenant.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tabelas cadastradas.</returns>
    Task<IReadOnlyList<TabelaIrrfServicos>> ListarTodasAsync(CancellationToken cancellationToken);

    /// <summary>Indica se já existe tabela com a vigência inicial informada (idempotência do seed).</summary>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existir.</returns>
    Task<bool> ExisteVigenciaAsync(DateOnly vigenciaInicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="GuiaRecolhimento"/> (recolhimento de consignações).</summary>
public interface IGuiaRecolhimentoRepository
{
    /// <summary>Marca uma nova guia para inserção.</summary>
    /// <param name="guia">Guia a adicionar.</param>
    void Adicionar(GuiaRecolhimento guia);

    /// <summary>Obtém uma guia por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A guia, ou <c>null</c>.</returns>
    Task<GuiaRecolhimento?> ObterPorIdAsync(GuiaRecolhimentoId id, CancellationToken cancellationToken);

    /// <summary>Lista guias por situação (paginação simples).</summary>
    /// <param name="situacao">Situação (nula = todas).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Guias.</returns>
    Task<IReadOnlyList<GuiaRecolhimento>> ListarAsync(SituacaoGuiaRecolhimento? situacao, CancellationToken cancellationToken);
}
