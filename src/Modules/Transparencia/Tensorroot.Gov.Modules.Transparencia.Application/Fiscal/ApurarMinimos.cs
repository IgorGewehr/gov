using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Fiscal;

/// <summary>Linha do resultado da apuração de um mínimo constitucional.</summary>
/// <param name="Setor">Setor apurado (Saúde/Educação).</param>
/// <param name="ReceitaBase">Receita-base (impostos + transferências constitucionais).</param>
/// <param name="Aplicado">Valor aplicado computável no setor.</param>
/// <param name="PercentualAplicado">Percentual aplicado (0..1).</param>
/// <param name="PercentualMinimo">Percentual mínimo (limite) vigente.</param>
/// <param name="Situacao">Situação (Atingido/NaoAtingido).</param>
public sealed record ApuracaoMinimoResultado(
    SetorMinimo Setor,
    decimal ReceitaBase,
    decimal Aplicado,
    decimal PercentualAplicado,
    decimal PercentualMinimo,
    SituacaoMinimo Situacao);

/// <summary>
/// <b>M7.0.3.</b> Apura os mínimos constitucionais (Saúde 15% ASPS · Educação 25% MDE) de um exercício,
/// tenant-scoped. Read-only e reprodutível (sem relógio): usa o exercício como âncora temporal.
/// </summary>
/// <param name="Exercicio">Ano de exercício a apurar.</param>
public sealed record ApurarMinimosQuery(int Exercicio) : IQuery<IReadOnlyList<ApuracaoMinimoResultado>>;

/// <summary>
/// Handler da apuração: obtém a execução setorial classificada (<see cref="IExecucaoSetorialReadModel"/>)
/// e os percentuais vigentes (<see cref="IParametroMinimoProvider"/>), delega ao
/// <see cref="ApuradorMinimo"/> e projeta o resultado. Toda a regra fiscal é do domínio; o handler só
/// orquestra as portas.
/// </summary>
public sealed class ApurarMinimosHandler(
    IExecucaoSetorialReadModel execucao,
    IParametroMinimoProvider parametros)
    : IQueryHandler<ApurarMinimosQuery, IReadOnlyList<ApuracaoMinimoResultado>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ApuracaoMinimoResultado>> Handle(
        ApurarMinimosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var execucaoSetorial = await execucao.ObterExecucaoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        var parametrosVigentes = await parametros.ObterParametrosAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        var indicadores = ApuradorMinimo.ApurarTodos(
            execucaoSetorial.ReceitaBaseImpostosTransferencias,
            execucaoSetorial.DespesasPorSetor,
            parametrosVigentes);

        return indicadores
            .Select(indicador => new ApuracaoMinimoResultado(
                indicador.Setor,
                indicador.ReceitaBase,
                indicador.Aplicado,
                indicador.PercentualAplicado,
                indicador.PercentualMinimo,
                indicador.Situacao))
            .ToList();
    }
}
