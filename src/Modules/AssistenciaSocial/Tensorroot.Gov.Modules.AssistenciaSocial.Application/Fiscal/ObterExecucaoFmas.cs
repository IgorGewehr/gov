using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;

/// <summary>Saldo segregado de um bloco de cofinanciamento do FMAS.</summary>
/// <param name="Bloco">Bloco (PSB/PSE-MC/PSE-AC/Gestao-IGD).</param>
/// <param name="Recebido">Total recebido do FNAS no bloco.</param>
/// <param name="Executado">Total executado no bloco.</param>
/// <param name="Saldo">Saldo disponivel no bloco.</param>
public sealed record SaldoBlocoFmas(BlocoFinanciamentoAssistencia Bloco, decimal Recebido, decimal Executado, decimal Saldo);

/// <summary>Saldo segregado de um piso de cofinanciamento do FMAS.</summary>
/// <param name="Piso">Piso (servico tipificado).</param>
/// <param name="Recebido">Total recebido no piso.</param>
/// <param name="Executado">Total executado no piso.</param>
/// <param name="Saldo">Saldo disponivel no piso.</param>
public sealed record SaldoPisoFmas(PisoAssistencia Piso, decimal Recebido, decimal Executado, decimal Saldo);

/// <summary>Execucao do FMAS por bloco e por piso (painel de execucao por piso — A-1).</summary>
/// <param name="FundoId">Identificador do fundo.</param>
/// <param name="Nome">Nome da unidade gestora.</param>
/// <param name="Blocos">Saldos por bloco de cofinanciamento.</param>
/// <param name="Pisos">Saldos por piso de cofinanciamento.</param>
public sealed record ExecucaoFmasResultado(
    Guid FundoId,
    string Nome,
    IReadOnlyList<SaldoBlocoFmas> Blocos,
    IReadOnlyList<SaldoPisoFmas> Pisos);

/// <summary>A-1: consulta a execucao segregada por bloco/piso de um FMAS, tenant-scoped.</summary>
/// <param name="FundoId">Identificador do fundo.</param>
public sealed record ObterExecucaoFmasQuery(Guid FundoId) : IQuery<ExecucaoFmasResultado>;

/// <summary>Handler da consulta de execucao do FMAS por bloco/piso.</summary>
public sealed class ObterExecucaoFmasHandler(IFundoMunicipalAssistenciaRepository fundos)
    : IQueryHandler<ObterExecucaoFmasQuery, ExecucaoFmasResultado>
{
    /// <inheritdoc />
    public async Task<ExecucaoFmasResultado> Handle(ObterExecucaoFmasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fundo = await fundos.ObterPorIdAsync(new FundoMunicipalAssistenciaId(request.FundoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fundo Municipal de Assistencia Social inexistente no tenant.");

        var blocos = Enum.GetValues<BlocoFinanciamentoAssistencia>()
            .Select(bloco => new SaldoBlocoFmas(
                bloco,
                fundo.RecebidoDoBloco(bloco),
                fundo.ExecutadoDoBloco(bloco),
                fundo.SaldoDoBloco(bloco)))
            .ToList();

        var pisos = Enum.GetValues<PisoAssistencia>()
            .Select(piso => new SaldoPisoFmas(
                piso,
                fundo.RecebidoDoPiso(piso),
                fundo.ExecutadoDoPiso(piso),
                fundo.SaldoDoPiso(piso)))
            .ToList();

        return new ExecucaoFmasResultado(fundo.Id.Value, fundo.Nome, blocos, pisos);
    }
}
