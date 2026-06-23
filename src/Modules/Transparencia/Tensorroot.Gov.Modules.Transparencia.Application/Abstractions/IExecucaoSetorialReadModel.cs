using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Execução fiscal de um exercício/período já classificada por setor, pronta para o
/// <c>ApuradorMinimo</c>: a receita-base e as despesas computáveis por setor.
/// </summary>
/// <param name="Exercicio">Ano de exercício.</param>
/// <param name="ReceitaBaseImpostosTransferencias">
/// Receita-base do mínimo: impostos + transferências constitucionais (base dos 15%/25%).
/// </param>
/// <param name="DespesasPorSetor">Despesa computável agregada por setor (Saúde/Educação).</param>
public sealed record ExecucaoSetorial(
    int Exercicio,
    decimal ReceitaBaseImpostosTransferencias,
    IReadOnlyList<DespesaSetorialApurada> DespesasPorSetor);

/// <summary>
/// <b>M7.0.0 — abstração de fonte de dados de execução.</b> Desacopla o <c>ApuradorMinimo</c> da
/// <i>via</i> usada para obter os dados de execução classificados (Via A2 hoje: deriva da
/// <c>MSCGeradaIntegrationEvent</c> cruzando com <see cref="FonteRecursoVinculado"/>; Via A1 amanhã:
/// consome os campos decompostos já carimbados em <c>DespesaEmpenhadaIntegrationEvent</c>). A troca
/// A2→A1 NÃO toca o domínio nem os apuradores — só a implementação desta porta.
/// </summary>
public interface IExecucaoSetorialReadModel
{
    /// <summary>Obtém a execução setorial classificada do exercício (tenant-scoped).</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Execução setorial, ou base zerada quando não há dados.</returns>
    Task<ExecucaoSetorial> ObterExecucaoAsync(int exercicio, CancellationToken cancellationToken);
}
