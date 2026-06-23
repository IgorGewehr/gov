using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>Cadastra um item no catalogo de medicamentos da rede (REMUME).</summary>
/// <param name="PrincipioAtivo">Principio ativo (DCB/DCI).</param>
/// <param name="Apresentacao">Apresentacao descritiva.</param>
/// <param name="Concentracao">Concentracao/dosagem.</param>
/// <param name="Forma">Forma farmaceutica.</param>
/// <param name="Unidade">Unidade de medida do estoque.</param>
/// <param name="Controle">Classe de controle especial (Portaria 344/1998).</param>
/// <param name="CodigoCatmat">Codigo CATMAT (opcional).</param>
public sealed record CadastrarMedicamentoCommand(
    string PrincipioAtivo,
    string Apresentacao,
    string Concentracao,
    FormaFarmaceutica Forma,
    UnidadeMedidaMedicamento Unidade,
    TipoControleSngpc Controle,
    string? CodigoCatmat) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de medicamento.</summary>
public sealed class CadastrarMedicamentoValidator : AbstractValidator<CadastrarMedicamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarMedicamentoValidator()
    {
        RuleFor(c => c.PrincipioAtivo).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Apresentacao).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Concentracao).NotEmpty().MaximumLength(60);
        RuleFor(c => c.Forma).IsInEnum();
        RuleFor(c => c.Unidade).IsInEnum();
        RuleFor(c => c.Controle).IsInEnum();
        RuleFor(c => c.CodigoCatmat).MaximumLength(20);
    }
}

/// <summary>Handler do cadastro de medicamento.</summary>
public sealed class CadastrarMedicamentoHandler(
    IMedicamentoRepository medicamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarMedicamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarMedicamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medicamento = Medicamento.Cadastrar(
            tenant.TenantId,
            request.PrincipioAtivo,
            request.Apresentacao,
            request.Concentracao,
            request.Forma,
            request.Unidade,
            request.Controle,
            request.CodigoCatmat);

        medicamentos.Adicionar(medicamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return medicamento.Id.Value;
    }
}
