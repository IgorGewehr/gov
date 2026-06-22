using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Contribuinte"/>.</summary>
public interface IContribuinteRepository
{
    /// <summary>Marca um novo contribuinte para inserção.</summary>
    /// <param name="contribuinte">Contribuinte a adicionar.</param>
    void Adicionar(Contribuinte contribuinte);

    /// <summary>Obtém um contribuinte por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O contribuinte, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Contribuinte?> ObterPorIdAsync(ContribuinteId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Lancamento"/>.</summary>
public interface ILancamentoRepository
{
    /// <summary>Marca um novo lançamento para inserção.</summary>
    /// <param name="lancamento">Lançamento a adicionar.</param>
    void Adicionar(Lancamento lancamento);

    /// <summary>Obtém um lançamento por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O lançamento, ou <c>null</c>.</returns>
    Task<Lancamento?> ObterPorIdAsync(LancamentoId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="DividaAtiva"/>.</summary>
public interface IDividaAtivaRepository
{
    /// <summary>Marca uma nova dívida ativa para inserção.</summary>
    /// <param name="dividaAtiva">Dívida ativa a adicionar.</param>
    void Adicionar(DividaAtiva dividaAtiva);

    /// <summary>Obtém uma dívida ativa por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A dívida ativa, ou <c>null</c>.</returns>
    Task<DividaAtiva?> ObterPorIdAsync(DividaAtivaId id, CancellationToken cancellationToken);

    /// <summary>Lista as dívidas ativas de um contribuinte.</summary>
    /// <param name="contribuinteId">Contribuinte.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dívidas ativas do contribuinte.</returns>
    Task<IReadOnlyList<DividaAtiva>> ListarPorContribuinteAsync(ContribuinteId contribuinteId, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Imovel"/> (cadastro imobiliário).</summary>
public interface IImovelRepository
{
    /// <summary>Marca um novo imóvel para inserção.</summary>
    /// <param name="imovel">Imóvel a adicionar.</param>
    void Adicionar(Imovel imovel);

    /// <summary>Obtém um imóvel por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O imóvel, ou <c>null</c>.</returns>
    Task<Imovel?> ObterPorIdAsync(ImovelId id, CancellationToken cancellationToken);

    /// <summary>Lista os imóveis de um proprietário.</summary>
    /// <param name="proprietarioId">Contribuinte proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Imóveis do proprietário.</returns>
    Task<IReadOnlyList<Imovel>> ListarPorProprietarioAsync(ContribuinteId proprietarioId, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="PlantaValores"/> (PGV).</summary>
public interface IPlantaValoresRepository
{
    /// <summary>Marca uma nova PGV para inserção.</summary>
    /// <param name="planta">PGV a adicionar.</param>
    void Adicionar(PlantaValores planta);

    /// <summary>Obtém uma PGV por identificador (com zonas e fatores).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A PGV, ou <c>null</c>.</returns>
    Task<PlantaValores?> ObterPorIdAsync(PlantaValoresId id, CancellationToken cancellationToken);

    /// <summary>Obtém a PGV vigente de um exercício (com zonas e fatores).</summary>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A PGV vigente, ou <c>null</c> se não houver.</returns>
    Task<PlantaValores?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="TabelaAliquotaIptu"/>.</summary>
public interface ITabelaAliquotaIptuRepository
{
    /// <summary>Marca uma nova tabela de alíquotas para inserção.</summary>
    /// <param name="tabela">Tabela a adicionar.</param>
    void Adicionar(TabelaAliquotaIptu tabela);

    /// <summary>Obtém uma tabela por identificador (com faixas).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela, ou <c>null</c>.</returns>
    Task<TabelaAliquotaIptu?> ObterPorIdAsync(TabelaAliquotaIptuId id, CancellationToken cancellationToken);

    /// <summary>Obtém a tabela vigente de um exercício (predial ou territorial).</summary>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="edificado">Tabela predial (true) ou territorial (false).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela vigente, ou <c>null</c>.</returns>
    Task<TabelaAliquotaIptu?> ObterVigenteAsync(int exercicio, bool edificado, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Dam"/> (guia/carnê).</summary>
public interface IDamRepository
{
    /// <summary>Marca um novo DAM para inserção.</summary>
    /// <param name="dam">DAM a adicionar.</param>
    void Adicionar(Dam dam);

    /// <summary>Obtém um DAM por identificador (com parcelas).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O DAM, ou <c>null</c>.</returns>
    Task<Dam?> ObterPorIdAsync(DamId id, CancellationToken cancellationToken);
}
