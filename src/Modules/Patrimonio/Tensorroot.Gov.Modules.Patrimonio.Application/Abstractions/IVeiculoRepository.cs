using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Veiculo"/>.</summary>
public interface IVeiculoRepository
{
    /// <summary>Marca um novo veículo para inserção.</summary>
    /// <param name="veiculo">Veículo a adicionar.</param>
    void Adicionar(Veiculo veiculo);

    /// <summary>Obtém um veículo por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O veículo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Veiculo?> ObterPorIdAsync(VeiculoId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe um veículo com o RENAVAM informado no tenant (I-2).</summary>
    /// <param name="renavam">RENAVAM a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o RENAVAM já estiver em uso.</returns>
    Task<bool> ExisteRenavamAsync(Domain.ValueObjects.Renavam renavam, CancellationToken cancellationToken);

    /// <summary>Lista os abastecimentos de um veículo em um período.</summary>
    /// <param name="veiculoId">Veículo.</param>
    /// <param name="de">Data inicial (inclusiva).</param>
    /// <param name="ate">Data final (inclusiva).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Abastecimentos do veículo no período.</returns>
    Task<IReadOnlyList<Abastecimento>> ListarAbastecimentosAsync(
        VeiculoId veiculoId,
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken);

    /// <summary>Lista as multas pendentes/em recurso de todos os veículos do tenant.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Multas não pagas, com a placa do veículo.</returns>
    Task<IReadOnlyList<MultaComVeiculo>> ListarMultasPendentesAsync(CancellationToken cancellationToken);

    /// <summary>Lista os veículos sem licenciamento regular no exercício informado (I-11).</summary>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Veículos com pendência de licenciamento no exercício.</returns>
    Task<IReadOnlyList<LicenciamentoPendente>> ListarLicenciamentosPendentesAsync(
        int exercicio,
        CancellationToken cancellationToken);

    /// <summary>Lista os veículos depreciáveis (ativos no acervo e em condições de uso) — BUG-P4.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Veículos aptos a depreciação.</returns>
    Task<IReadOnlyList<Veiculo>> ListarDepreciaveisAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de veículos por descrição/placa/RENAVAM (navegabilidade — Onda 0), com filtro
    /// opcional por situação. Tenant-scoped via Global Query Filter. Ordena por placa.
    /// </summary>
    /// <param name="termo">Termo livre (descrição, placa ou RENAVAM; case/acento-insensível); nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situação no ciclo patrimonial.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de veículos e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Veiculo> Itens, int Total)> BuscarAsync(
        string? termo,
        Domain.Bens.SituacaoBemPatrimonial? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    /// <summary>
    /// Projeta o custo agregado por veículo em um período (Painel de Frota — Onda 3a): combustível
    /// (abastecimentos no período), manutenção (OS concluídas no período, custo realizado) e multas
    /// (infrações no período). Inclui consumo médio (km/L) e a placa. Tenant-scoped via Global Query Filter.
    /// </summary>
    /// <param name="de">Data inicial (inclusiva).</param>
    /// <param name="ate">Data final (inclusiva).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linha de custo/consumo por veículo no período.</returns>
    Task<IReadOnlyList<CustoVeiculoLinha>> ProjetarCustosPorVeiculoAsync(
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista os condutores cuja CNH vence dentro da janela informada a partir da data de referência
    /// (Painel de Frota — Onda 3a). Inclui CNH já vencida (dias negativos). Tenant-scoped.
    /// </summary>
    /// <param name="referencia">Data de referência (hoje).</param>
    /// <param name="ate">Limite superior da janela de vencimento (inclusivo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Condutores com CNH a vencer/vencida na janela, com o veículo vinculado.</returns>
    Task<IReadOnlyList<CnhVencendoLinha>> ListarCnhVencendoAsync(
        DateOnly referencia,
        DateOnly ate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista as ordens de serviço de manutenção em aberto de toda a frota do tenant (Painel de Frota — Onda 3a).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Ordens de serviço abertas, com a placa do veículo.</returns>
    Task<IReadOnlyList<ManutencaoAbertaLinha>> ListarManutencoesAbertasAsync(CancellationToken cancellationToken);
}

/// <summary>Linha de custo/consumo de um veículo em um período (Painel de Frota).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Descricao">Descrição do veículo.</param>
/// <param name="GastoCombustivel">Gasto com combustível no período.</param>
/// <param name="LitrosAbastecidos">Litros abastecidos no período.</param>
/// <param name="GastoManutencao">Gasto com manutenção (OS concluídas) no período.</param>
/// <param name="GastoMultas">Valor de multas (infrações) no período.</param>
/// <param name="KmRodados">Quilômetros rodados no período (odômetro final menos inicial dos abastecimentos).</param>
/// <param name="ConsumoMedioKmL">Consumo médio km/L no período (nulo se indeterminável).</param>
public sealed record CustoVeiculoLinha(
    Guid VeiculoId,
    string Placa,
    string Descricao,
    decimal GastoCombustivel,
    decimal LitrosAbastecidos,
    decimal GastoManutencao,
    decimal GastoMultas,
    int KmRodados,
    decimal? ConsumoMedioKmL)
{
    /// <summary>Custo total do veículo no período (combustível + manutenção + multas).</summary>
    public decimal CustoTotal => GastoCombustivel + GastoManutencao + GastoMultas;
}

/// <summary>Linha de CNH a vencer/vencida de um condutor (Painel de Frota).</summary>
/// <param name="VeiculoId">Identificador do veículo vinculado.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="MotoristaId">Identificador do motorista.</param>
/// <param name="Nome">Nome do condutor.</param>
/// <param name="Cnh">Número da CNH.</param>
/// <param name="CategoriaCnh">Categoria da CNH.</param>
/// <param name="ValidadeCnh">Validade da CNH.</param>
/// <param name="DiasParaVencer">Dias até o vencimento (negativo se já vencida).</param>
public sealed record CnhVencendoLinha(
    Guid VeiculoId,
    string Placa,
    Guid MotoristaId,
    string Nome,
    string Cnh,
    string CategoriaCnh,
    DateOnly ValidadeCnh,
    int DiasParaVencer);

/// <summary>Linha de ordem de serviço de manutenção em aberto (Painel de Frota).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="OrdemServicoId">Identificador da ordem de serviço.</param>
/// <param name="Descricao">Descrição do serviço.</param>
/// <param name="CustoEstimado">Custo estimado na abertura.</param>
/// <param name="Odometro">Leitura do odômetro na abertura.</param>
public sealed record ManutencaoAbertaLinha(
    Guid VeiculoId,
    string Placa,
    Guid OrdemServicoId,
    string Descricao,
    decimal CustoEstimado,
    int Odometro);

/// <summary>Projeção de leitura de uma multa acompanhada da placa do veículo.</summary>
/// <param name="MultaId">Identificador da multa.</param>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="CodigoInfracaoCtb">Código de infração do CTB.</param>
/// <param name="Valor">Valor da multa.</param>
/// <param name="DataInfracao">Data da infração.</param>
public sealed record MultaComVeiculo(
    Guid MultaId,
    Guid VeiculoId,
    string Placa,
    string CodigoInfracaoCtb,
    decimal Valor,
    DateOnly DataInfracao);

/// <summary>Projeção de leitura de uma pendência de licenciamento por veículo/exercício.</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Exercicio">Exercício (ano).</param>
/// <param name="ValorIpva">Valor de IPVA conhecido para o exercício (zero se desconhecido).</param>
/// <param name="Situacao">Situação do licenciamento.</param>
public sealed record LicenciamentoPendente(
    Guid VeiculoId,
    string Placa,
    int Exercicio,
    decimal ValorIpva,
    string Situacao);
