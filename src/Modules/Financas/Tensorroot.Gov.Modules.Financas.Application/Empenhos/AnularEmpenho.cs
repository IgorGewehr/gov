using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Empenhos;

/// <summary>Anula um empenho (parcial se informado valor; total caso contrário).</summary>
/// <param name="EmpenhoId">Identificador do empenho.</param>
/// <param name="Valor">Valor a anular; <c>null</c> para anulação total.</param>
public sealed record AnularEmpenhoCommand(Guid EmpenhoId, decimal? Valor) : ICommand;

/// <summary>
/// Handler da anulação de empenho. Reduz o saldo do empenho e libera o saldo na
/// dotação (<see cref="Domain.Dotacoes.DotacaoOrcamentaria.LiberarEmpenho"/>) na mesma transação.
/// </summary>
public sealed class AnularEmpenhoHandler(
    IEmpenhoRepository empenhos,
    IDotacaoOrcamentariaRepository dotacoes,
    IUnitOfWork unitOfWork) : ICommandHandler<AnularEmpenhoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AnularEmpenhoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var empenho = await empenhos.ObterPorIdAsync(new EmpenhoId(request.EmpenhoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empenho nao encontrado.");

        var valorAnular = request.Valor is { } v ? ValorMonetario.De(v) : empenho.SaldoALiquidar;

        if (request.Valor is null)
        {
            empenho.AnularTotal();
        }
        else
        {
            empenho.AnularParcial(valorAnular);
        }

        var dotacao = await dotacoes.ObterPorIdAsync(empenho.DotacaoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dotacao do empenho nao encontrada.");

        dotacao.LiberarEmpenho(valorAnular);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
