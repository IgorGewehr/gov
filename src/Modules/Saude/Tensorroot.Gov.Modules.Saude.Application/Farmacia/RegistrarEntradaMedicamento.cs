using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Registra a entrada de um lote de medicamento no estoque de um estabelecimento (compra/transferencia/
/// doacao). Abre a posicao de estoque na primeira entrada (idempotente por estabelecimento+medicamento).
/// Rejeita lote vencido (I-FARM-2).
/// </summary>
/// <param name="EstabelecimentoId">Estabelecimento detentor do estoque.</param>
/// <param name="MedicamentoId">Medicamento que recebe a entrada.</param>
/// <param name="NumeroLote">Numero do lote (do fabricante).</param>
/// <param name="Validade">Validade do lote.</param>
/// <param name="Quantidade">Quantidade recebida (> 0).</param>
/// <param name="PontoDeRessuprimento">Ponto de ressuprimento (aplicado na abertura).</param>
public sealed record RegistrarEntradaMedicamentoCommand(
    Guid EstabelecimentoId,
    Guid MedicamentoId,
    string NumeroLote,
    DateOnly Validade,
    decimal Quantidade,
    int PontoDeRessuprimento) : ICommand<Guid>;

/// <summary>Regras de validacao da entrada de medicamento.</summary>
public sealed class RegistrarEntradaMedicamentoValidator : AbstractValidator<RegistrarEntradaMedicamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarEntradaMedicamentoValidator()
    {
        RuleFor(c => c.EstabelecimentoId).NotEmpty();
        RuleFor(c => c.MedicamentoId).NotEmpty();
        RuleFor(c => c.NumeroLote).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Quantidade).GreaterThan(0m);
        RuleFor(c => c.PontoDeRessuprimento).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da entrada de medicamento no estoque.</summary>
public sealed class RegistrarEntradaMedicamentoHandler(
    IEstoqueMedicamentoRepository estoques,
    IMedicamentoRepository medicamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarEntradaMedicamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarEntradaMedicamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var estabelecimentoId = new EstabelecimentoId(request.EstabelecimentoId);
        var medicamentoId = new MedicamentoId(request.MedicamentoId);

        // O medicamento deve existir e estar ativo no catalogo do tenant.
        var medicamento = await medicamentos.ObterPorIdAsync(medicamentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Medicamento nao encontrado no catalogo.");
        if (!medicamento.Ativo)
        {
            throw new InvalidOperationException("Medicamento inativo no catalogo.");
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var estoque = await estoques
            .ObterPorEstabelecimentoEMedicamentoAsync(estabelecimentoId, medicamentoId, cancellationToken)
            .ConfigureAwait(false);

        if (estoque is null)
        {
            estoque = EstoqueMedicamento.Abrir(tenant.TenantId, estabelecimentoId, medicamentoId, request.PontoDeRessuprimento);
            estoques.Adicionar(estoque);
        }

        estoque.RegistrarEntrada(request.NumeroLote, request.Validade, request.Quantidade, hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return estoque.Id.Value;
    }
}
