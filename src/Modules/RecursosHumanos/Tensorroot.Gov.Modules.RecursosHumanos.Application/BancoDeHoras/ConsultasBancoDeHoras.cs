using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.BancoDeHoras;

/// <summary>Linha do extrato do banco de horas.</summary>
/// <param name="Tipo">Natureza do lancamento.</param>
/// <param name="Minutos">Minutos do lancamento.</param>
/// <param name="Data">Data-base do lancamento.</param>
/// <param name="Descricao">Descricao do lancamento.</param>
public sealed record LancamentoBancoHorasView(
    TipoLancamentoBancoHoras Tipo,
    int Minutos,
    DateOnly Data,
    string Descricao);

/// <summary>Extrato do banco de horas de um servidor: saldo corrente + linhas do livro-razao.</summary>
/// <param name="ServidorId">Servidor titular.</param>
/// <param name="SaldoMinutos">Saldo corrente em minutos (positivo a favor; negativo devido).</param>
/// <param name="Lancamentos">Linhas do extrato (mais recentes primeiro).</param>
public sealed record ExtratoBancoDeHorasView(
    Guid ServidorId,
    int SaldoMinutos,
    IReadOnlyList<LancamentoBancoHorasView> Lancamentos);

/// <summary>Obtem o extrato do banco de horas de um servidor.</summary>
/// <param name="ServidorId">Servidor titular.</param>
public sealed record ObterExtratoBancoDeHorasQuery(Guid ServidorId) : IQuery<ExtratoBancoDeHorasView>;

/// <summary>Handler do extrato do banco de horas.</summary>
public sealed class ObterExtratoBancoDeHorasHandler(IBancoDeHorasRepository bancos)
    : IQueryHandler<ObterExtratoBancoDeHorasQuery, ExtratoBancoDeHorasView>
{
    /// <inheritdoc />
    public async Task<ExtratoBancoDeHorasView> Handle(ObterExtratoBancoDeHorasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var banco = await bancos.ObterPorServidorAsync(request.ServidorId, cancellationToken).ConfigureAwait(false);
        if (banco is null)
        {
            // Sem banco aberto ainda: extrato vazio com saldo zero (nao e erro).
            return new ExtratoBancoDeHorasView(request.ServidorId, 0, []);
        }

        var linhas = banco.Lancamentos
            .OrderByDescending(l => l.Data)
            .Select(l => new LancamentoBancoHorasView(l.Tipo, l.Minutos, l.Data, l.Descricao))
            .ToList();

        return new ExtratoBancoDeHorasView(request.ServidorId, banco.SaldoMinutos, linhas);
    }
}
