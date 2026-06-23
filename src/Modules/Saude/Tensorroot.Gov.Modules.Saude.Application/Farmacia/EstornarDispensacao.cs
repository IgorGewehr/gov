using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Estorna uma dispensacao (entrega indevida/devolucao): marca como estornada e recompoe o saldo de
/// estoque devolvendo cada baixa ao lote de origem — na mesma transacao (consistencia estoque +
/// dispensacao). Reagrupa as baixas por medicamento para devolver no estoque correto.
/// </summary>
/// <param name="DispensacaoId">Dispensacao a estornar.</param>
/// <param name="Motivo">Motivo do estorno.</param>
public sealed record EstornarDispensacaoCommand(Guid DispensacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do estorno.</summary>
public sealed class EstornarDispensacaoValidator : AbstractValidator<EstornarDispensacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public EstornarDispensacaoValidator()
    {
        RuleFor(c => c.DispensacaoId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(300);
    }
}

/// <summary>Handler do estorno de dispensacao.</summary>
public sealed class EstornarDispensacaoHandler(
    IDispensacaoRepository dispensacoes,
    IEstoqueMedicamentoRepository estoques,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EstornarDispensacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EstornarDispensacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensacao = await dispensacoes
            .ObterPorIdAsync(new DispensacaoId(request.DispensacaoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensacao nao encontrada.");

        // Recompoe o saldo por medicamento ANTES de selar o estorno (devolve cada baixa ao seu lote).
        foreach (var grupo in dispensacao.BaixasPorMedicamento().GroupBy(x => x.MedicamentoId))
        {
            var estoque = await estoques
                .ObterPorEstabelecimentoEMedicamentoAsync(dispensacao.EstabelecimentoId, grupo.Key, cancellationToken)
                .ConfigureAwait(false);
            estoque?.Estornar(grupo.Select(x => x.Baixa));
        }

        dispensacao.Estornar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
