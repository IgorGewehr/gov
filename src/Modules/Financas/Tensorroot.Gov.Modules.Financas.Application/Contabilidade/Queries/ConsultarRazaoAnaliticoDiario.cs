using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;

/// <summary>Linha analítica do razão (um lançamento-a-lançamento de uma conta), com saldo corrente.</summary>
/// <param name="LancamentoId">Lançamento de origem.</param>
/// <param name="Data">Data.</param>
/// <param name="Historico">Histórico.</param>
/// <param name="Origem">Origem (automático/manual/estorno/encerramento).</param>
/// <param name="Lado">Lado (Debito/Credito).</param>
/// <param name="Debito">Valor a débito (0 se for crédito).</param>
/// <param name="Credito">Valor a crédito (0 se for débito).</param>
/// <param name="SaldoAcumulado">Saldo corrente da conta após a partida (na natureza do saldo).</param>
public sealed record LinhaRazaoAnaliticoDto(
    Guid LancamentoId,
    DateOnly Data,
    string Historico,
    string Origem,
    string Lado,
    decimal Debito,
    decimal Credito,
    decimal SaldoAcumulado);

/// <summary>Linha cronológica do Livro Diário (um lançamento, com suas partidas).</summary>
/// <param name="LancamentoId">Lançamento.</param>
/// <param name="Data">Data.</param>
/// <param name="Historico">Histórico.</param>
/// <param name="Origem">Origem.</param>
/// <param name="Partidas">Partidas do lançamento.</param>
public sealed record LinhaDiarioDto(
    Guid LancamentoId,
    DateOnly Data,
    string Historico,
    string Origem,
    IReadOnlyList<PartidaDiarioDto> Partidas);

/// <summary>Partida de um lançamento no Diário.</summary>
/// <param name="CodigoConta">Código da conta.</param>
/// <param name="Lado">Lado.</param>
/// <param name="Valor">Valor.</param>
public sealed record PartidaDiarioDto(string CodigoConta, string Lado, decimal Valor);

/// <summary>
/// Razão ANALÍTICO de uma conta: extrato lançamento-a-lançamento (não saldos mensais agregados) com
/// saldo acumulado. Exigência do TCE e da rotina contábil.
/// </summary>
/// <param name="ContaId">Conta.</param>
/// <param name="Exercicio">Exercício.</param>
public sealed record ConsultarRazaoAnaliticoQuery(Guid ContaId, int Exercicio)
    : IQuery<IReadOnlyList<LinhaRazaoAnaliticoDto>>;

/// <summary>Livro Diário: lançamentos em ordem cronológica num intervalo (exigência TCE).</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="De">Data inicial (inclusiva, opcional).</param>
/// <param name="Ate">Data final (inclusiva, opcional).</param>
public sealed record ConsultarDiarioQuery(int Exercicio, DateOnly? De, DateOnly? Ate)
    : IQuery<IReadOnlyList<LinhaDiarioDto>>;

/// <summary>Handler do razão analítico.</summary>
public sealed class ConsultarRazaoAnaliticoHandler(ILancamentoContabilConsulta consulta)
    : IQueryHandler<ConsultarRazaoAnaliticoQuery, IReadOnlyList<LinhaRazaoAnaliticoDto>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LinhaRazaoAnaliticoDto>> Handle(ConsultarRazaoAnaliticoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return consulta.RazaoAnaliticoAsync(request.ContaId, request.Exercicio, cancellationToken);
    }
}

/// <summary>Handler do diário cronológico.</summary>
public sealed class ConsultarDiarioHandler(ILancamentoContabilConsulta consulta)
    : IQueryHandler<ConsultarDiarioQuery, IReadOnlyList<LinhaDiarioDto>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LinhaDiarioDto>> Handle(ConsultarDiarioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return consulta.DiarioAsync(request.Exercicio, request.De, request.Ate, cancellationToken);
    }
}
