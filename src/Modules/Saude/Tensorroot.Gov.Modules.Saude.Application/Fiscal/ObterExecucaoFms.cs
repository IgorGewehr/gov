using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Saude.Application.Fiscal;

/// <summary>Saldo segregado de um bloco de financiamento do FMS.</summary>
/// <param name="Bloco">Bloco (Custeio/Investimento).</param>
/// <param name="Recebido">Total recebido do FNS no bloco.</param>
/// <param name="Executado">Total executado no bloco.</param>
/// <param name="Saldo">Saldo disponível no bloco.</param>
public sealed record SaldoBlocoFms(BlocoFinanciamentoSaude Bloco, decimal Recebido, decimal Executado, decimal Saldo);

/// <summary>Execução do FMS por bloco (painel de segregação).</summary>
/// <param name="FundoId">Identificador do fundo.</param>
/// <param name="Nome">Nome da unidade gestora.</param>
/// <param name="Blocos">Saldos por bloco de financiamento.</param>
public sealed record ExecucaoFmsResultado(Guid FundoId, string Nome, IReadOnlyList<SaldoBlocoFms> Blocos);

/// <summary>S-2: consulta a execução segregada por bloco de um FMS, tenant-scoped.</summary>
/// <param name="FundoId">Identificador do fundo.</param>
public sealed record ObterExecucaoFmsQuery(Guid FundoId) : IQuery<ExecucaoFmsResultado>;

/// <summary>Handler da consulta de execução do FMS por bloco.</summary>
public sealed class ObterExecucaoFmsHandler(IFundoMunicipalSaudeRepository fundos)
    : IQueryHandler<ObterExecucaoFmsQuery, ExecucaoFmsResultado>
{
    /// <inheritdoc />
    public async Task<ExecucaoFmsResultado> Handle(ObterExecucaoFmsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = await fundos.ObterPorIdAsync(new FundoMunicipalSaudeId(request.FundoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fundo Municipal de Saude inexistente no tenant.");

        var blocos = Enum.GetValues<BlocoFinanciamentoSaude>()
            .Select(bloco => new SaldoBlocoFms(
                bloco,
                fundo.RecebidoDoBloco(bloco),
                fundo.ExecutadoDoBloco(bloco),
                fundo.SaldoDoBloco(bloco)))
            .ToList();

        return new ExecucaoFmsResultado(fundo.Id.Value, fundo.Nome, blocos);
    }
}
