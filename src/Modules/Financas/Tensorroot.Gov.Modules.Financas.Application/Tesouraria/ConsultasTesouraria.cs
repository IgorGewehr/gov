using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

namespace Tensorroot.Gov.Modules.Financas.Application.Tesouraria;

/// <summary>Resumo de uma conta da tesouraria.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nome">Nome.</param>
/// <param name="Tipo">Espécie (Bancaria/Caixa).</param>
/// <param name="Banco">Banco (se bancária).</param>
/// <param name="Agencia">Agência (se bancária).</param>
/// <param name="Conta">Conta (se bancária).</param>
/// <param name="SaldoInicial">Saldo de abertura.</param>
/// <param name="Saldo">Saldo corrente.</param>
/// <param name="Situacao">Situação.</param>
public sealed record ContaFinanceiraResumo(
    Guid Id,
    string Nome,
    string Tipo,
    string? Banco,
    string? Agencia,
    string? Conta,
    decimal SaldoInicial,
    decimal Saldo,
    string Situacao);

/// <summary>Linha do extrato de uma conta.</summary>
/// <param name="MovimentoId">Identificador do movimento.</param>
/// <param name="Data">Data.</param>
/// <param name="Tipo">Natureza.</param>
/// <param name="Valor">Valor.</param>
/// <param name="SaldoApos">Saldo após o movimento.</param>
/// <param name="Historico">Histórico.</param>
/// <param name="Documento">Documento.</param>
/// <param name="Conciliado">Se já conciliado.</param>
public sealed record MovimentoFinanceiroLinha(
    Guid MovimentoId,
    DateOnly Data,
    string Tipo,
    decimal Valor,
    decimal SaldoApos,
    string Historico,
    string? Documento,
    bool Conciliado);

/// <summary>Lista as contas da tesouraria do tenant.</summary>
public sealed record ListarContasFinanceirasQuery : IQuery<IReadOnlyList<ContaFinanceiraResumo>>;

/// <summary>Obtém o extrato de uma conta num intervalo de datas.</summary>
/// <param name="ContaId">Conta.</param>
/// <param name="De">Data inicial (inclusiva, opcional).</param>
/// <param name="Ate">Data final (inclusiva, opcional).</param>
public sealed record ConsultarExtratoContaQuery(Guid ContaId, DateOnly? De, DateOnly? Ate)
    : IQuery<IReadOnlyList<MovimentoFinanceiroLinha>>;

internal static class TesourariaMapper
{
    public static ContaFinanceiraResumo Mapear(ContaFinanceira c) => new(
        c.Id.Value,
        c.Nome,
        c.Tipo.ToString(),
        c.DadosBancarios?.Banco,
        c.DadosBancarios?.Agencia,
        c.DadosBancarios?.Conta,
        c.SaldoInicial.Valor,
        c.Saldo.Valor,
        c.Situacao.ToString());

    public static MovimentoFinanceiroLinha Mapear(MovimentoFinanceiro m) => new(
        m.Id.Value,
        m.Data,
        m.Tipo.ToString(),
        m.Valor.Valor,
        m.SaldoApos.Valor,
        m.Historico,
        m.Documento,
        m.Conciliado);
}

/// <summary>Handler da listagem de contas.</summary>
public sealed class ListarContasFinanceirasHandler(IContaFinanceiraRepository contas)
    : IQueryHandler<ListarContasFinanceirasQuery, IReadOnlyList<ContaFinanceiraResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContaFinanceiraResumo>> Handle(ListarContasFinanceirasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await contas.ListarAsync(cancellationToken).ConfigureAwait(false);
        return lista.Select(TesourariaMapper.Mapear).ToList();
    }
}

/// <summary>Handler do extrato.</summary>
public sealed class ConsultarExtratoContaHandler(IContaFinanceiraRepository contas)
    : IQueryHandler<ConsultarExtratoContaQuery, IReadOnlyList<MovimentoFinanceiroLinha>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MovimentoFinanceiroLinha>> Handle(ConsultarExtratoContaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var conta = await contas.ObterPorIdAsync(new ContaFinanceiraId(request.ContaId), cancellationToken).ConfigureAwait(false);
        if (conta is null)
        {
            return [];
        }

        return conta.Movimentos
            .Where(m => (request.De is null || m.Data >= request.De) && (request.Ate is null || m.Data <= request.Ate))
            .OrderBy(m => m.Data)
            .Select(TesourariaMapper.Mapear)
            .ToList();
    }
}
