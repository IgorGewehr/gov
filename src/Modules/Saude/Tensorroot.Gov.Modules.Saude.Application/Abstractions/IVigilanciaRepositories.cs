using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio dos estabelecimentos sujeitos a VISA (sempre tenant-scoped via Global Query Filter).</summary>
public interface IEstabelecimentoFiscalizavelRepository
{
    /// <summary>Marca um novo estabelecimento fiscalizavel para insercao.</summary>
    /// <param name="estabelecimento">Estabelecimento a adicionar.</param>
    void Adicionar(EstabelecimentoFiscalizavel estabelecimento);

    /// <summary>Obtem um estabelecimento fiscalizavel por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O estabelecimento, ou <c>null</c>.</returns>
    Task<EstabelecimentoFiscalizavel?> ObterPorIdAsync(EstabelecimentoFiscalizavelId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe estabelecimento com o mesmo documento+razao social (unicidade).</summary>
    /// <param name="documentoPersistido">Documento na forma persistida.</param>
    /// <param name="razaoSocial">Razao social.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe.</returns>
    Task<bool> ExisteAsync(string documentoPersistido, string razaoSocial, CancellationToken cancellationToken);

    /// <summary>Busca paginada por razao social/documento, filtrando por ramo/risco/situacao.</summary>
    /// <param name="termo">Termo livre (razao social/documento); nulo lista tudo.</param>
    /// <param name="ramo">Filtro de ramo (opcional).</param>
    /// <param name="risco">Filtro de risco (opcional).</param>
    /// <param name="situacao">Filtro de situacao (opcional).</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de estabelecimentos e o total.</returns>
    Task<(IReadOnlyList<EstabelecimentoFiscalizavel> Itens, int Total)> BuscarAsync(
        string? termo,
        RamoVisa? ramo,
        GrauRiscoSanitario? risco,
        SituacaoEstabelecimentoVisa? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}

/// <summary>Repositorio das inspecoes/vistorias sanitarias (sempre tenant-scoped).</summary>
public interface IInspecaoRepository
{
    /// <summary>Marca uma nova inspecao para insercao.</summary>
    /// <param name="inspecao">Inspecao a adicionar.</param>
    void Adicionar(Inspecao inspecao);

    /// <summary>Obtem uma inspecao por identificador (com itens).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A inspecao, ou <c>null</c>.</returns>
    Task<Inspecao?> ObterPorIdAsync(InspecaoId id, CancellationToken cancellationToken);

    /// <summary>Lista as inspecoes de um estabelecimento (agenda/historico, mais recentes primeiro).</summary>
    /// <param name="estabelecimentoId">Estabelecimento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Inspecoes do estabelecimento.</returns>
    Task<IReadOnlyList<Inspecao>> ListarPorEstabelecimentoAsync(
        EstabelecimentoFiscalizavelId estabelecimentoId, CancellationToken cancellationToken);

    /// <summary>Lista a agenda de inspecoes num intervalo (filtro opcional de situacao).</summary>
    /// <param name="de">Data inicial.</param>
    /// <param name="ate">Data final.</param>
    /// <param name="situacao">Filtro de situacao (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Inspecoes do intervalo.</returns>
    Task<IReadOnlyList<Inspecao>> ListarAgendaAsync(
        DateOnly de, DateOnly ate, SituacaoInspecao? situacao, CancellationToken cancellationToken);
}

/// <summary>Repositorio dos autos (infracao/intimacao) da VISA (sempre tenant-scoped).</summary>
public interface IAutoVisaRepository
{
    /// <summary>Marca um novo auto para insercao.</summary>
    /// <param name="autoVisa">Auto a adicionar.</param>
    void Adicionar(AutoVisa autoVisa);

    /// <summary>Obtem um auto por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O auto, ou <c>null</c>.</returns>
    Task<AutoVisa?> ObterPorIdAsync(AutoVisaId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe auto com o mesmo numero (unicidade do livro de autos).</summary>
    /// <param name="numero">Numero do auto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe.</returns>
    Task<bool> ExisteNumeroAsync(string numero, CancellationToken cancellationToken);

    /// <summary>Lista os autos por situacao (fila de processos administrativos por status).</summary>
    /// <param name="situacao">Situacao filtrada (opcional — nulo lista todos).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Autos filtrados, mais recentes primeiro.</returns>
    Task<IReadOnlyList<AutoVisa>> ListarPorSituacaoAsync(SituacaoAutoVisa? situacao, CancellationToken cancellationToken);

    /// <summary>Lista os autos Lavrados com prazo vencido ate a data (busca ativa por prazo).</summary>
    /// <param name="ate">Data limite (em regra, hoje).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Autos com prazo vencido.</returns>
    Task<IReadOnlyList<AutoVisa>> ListarPrazosVencidosAsync(DateOnly ate, CancellationToken cancellationToken);
}

/// <summary>Repositorio das licencas/alvaras sanitarios (sempre tenant-scoped).</summary>
public interface ILicencaSanitariaRepository
{
    /// <summary>Marca uma nova licenca para insercao.</summary>
    /// <param name="licenca">Licenca a adicionar.</param>
    void Adicionar(LicencaSanitaria licenca);

    /// <summary>Obtem uma licenca por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A licenca, ou <c>null</c>.</returns>
    Task<LicencaSanitaria?> ObterPorIdAsync(LicencaSanitariaId id, CancellationToken cancellationToken);

    /// <summary>Lista as licencas de um estabelecimento (historico, mais recentes primeiro).</summary>
    /// <param name="estabelecimentoId">Estabelecimento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Licencas do estabelecimento.</returns>
    Task<IReadOnlyList<LicencaSanitaria>> ListarPorEstabelecimentoAsync(
        EstabelecimentoFiscalizavelId estabelecimentoId, CancellationToken cancellationToken);

    /// <summary>Lista as licencas vigentes que vencem ate a data informada (alerta de renovacao).</summary>
    /// <param name="ate">Data limite de validade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Licencas a vencer.</returns>
    Task<IReadOnlyList<LicencaSanitaria>> ListarAVencerAsync(DateOnly ate, CancellationToken cancellationToken);
}
