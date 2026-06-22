using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;

/// <summary>Linha de balancete para leitura.</summary>
/// <param name="ContaId">Conta.</param>
/// <param name="CodigoConta">Código.</param>
/// <param name="Titulo">Título.</param>
/// <param name="NaturezaSaldo">Natureza do saldo.</param>
/// <param name="NaturezaInformacao">Natureza da informação.</param>
/// <param name="SaldoAnterior">Saldo anterior.</param>
/// <param name="TotalDebitos">Total de débitos.</param>
/// <param name="TotalCreditos">Total de créditos.</param>
/// <param name="SaldoAtual">Saldo atual.</param>
public sealed record LinhaBalanceteDto(
    Guid ContaId,
    string CodigoConta,
    string Titulo,
    string NaturezaSaldo,
    string NaturezaInformacao,
    decimal SaldoAnterior,
    decimal TotalDebitos,
    decimal TotalCreditos,
    decimal SaldoAtual);

/// <summary>Consulta o balancete de um período.</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="PeriodoMes">Mês (1-12).</param>
public sealed record ConsultarBalanceteQuery(int Exercicio, int PeriodoMes) : IQuery<IReadOnlyList<LinhaBalanceteDto>>;

/// <summary>Consulta o razão (linhas mensais) de uma conta num exercício.</summary>
/// <param name="ContaId">Conta.</param>
/// <param name="Exercicio">Exercício.</param>
public sealed record ConsultarRazaoQuery(Guid ContaId, int Exercicio) : IQuery<IReadOnlyList<LinhaBalanceteDto>>;

/// <summary>Mapeamento read model → DTO.</summary>
internal static class BalanceteDtoMapper
{
    public static LinhaBalanceteDto Mapear(LinhaBalancete l) => new(
        l.ContaId,
        l.CodigoConta,
        l.Titulo,
        l.NaturezaSaldo.ToString(),
        l.NaturezaInformacao.ToString(),
        l.SaldoAnterior,
        l.TotalDebitos,
        l.TotalCreditos,
        l.SaldoAtual);
}

/// <summary>Handler da consulta de balancete por período.</summary>
public sealed class ConsultarBalanceteHandler(IBalanceteProjection balancete)
    : IQueryHandler<ConsultarBalanceteQuery, IReadOnlyList<LinhaBalanceteDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaBalanceteDto>> Handle(ConsultarBalanceteQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var linhas = await balancete.ListarPorPeriodoAsync(request.Exercicio, request.PeriodoMes, cancellationToken).ConfigureAwait(false);
        return linhas.Select(BalanceteDtoMapper.Mapear).ToList();
    }
}

/// <summary>Handler da consulta de razão por conta.</summary>
public sealed class ConsultarRazaoHandler(IBalanceteProjection balancete)
    : IQueryHandler<ConsultarRazaoQuery, IReadOnlyList<LinhaBalanceteDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LinhaBalanceteDto>> Handle(ConsultarRazaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var linhas = await balancete.ListarRazaoContaAsync(request.ContaId, request.Exercicio, cancellationToken).ConfigureAwait(false);
        return linhas.Select(BalanceteDtoMapper.Mapear).ToList();
    }
}
