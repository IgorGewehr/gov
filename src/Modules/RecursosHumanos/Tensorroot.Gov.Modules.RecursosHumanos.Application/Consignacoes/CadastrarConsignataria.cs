using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>Cadastra uma consignataria habilitada (banco/entidade) no cadastro mestre do tenant.</summary>
/// <param name="Cnpj">CNPJ (com ou sem mascara).</param>
/// <param name="RazaoSocial">Razao social/nome empresarial.</param>
/// <param name="Tipo">Natureza da consignataria.</param>
public sealed record CadastrarConsignatariaCommand(
    string Cnpj,
    string RazaoSocial,
    TipoConsignataria Tipo) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de consignataria.</summary>
public sealed class CadastrarConsignatariaValidator : AbstractValidator<CadastrarConsignatariaCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarConsignatariaValidator()
    {
        RuleFor(c => c.Cnpj).NotEmpty().WithMessage("CNPJ e obrigatorio.");
        RuleFor(c => c.RazaoSocial).NotEmpty().MaximumLength(200).WithMessage("Razao social e obrigatoria (max. 200).");
        RuleFor(c => c.Tipo).IsInEnum().WithMessage("Tipo de consignataria invalido.");
    }
}

/// <summary>Handler do cadastro de consignataria.</summary>
public sealed class CadastrarConsignatariaHandler(
    IConsignatariaRepository consignatarias,
    ITenantContext tenant,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CadastrarConsignatariaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarConsignatariaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cnpj = Cnpj.Create(request.Cnpj);
        if (await consignatarias.ExistePorCnpjAsync(cnpj, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe consignataria com este CNPJ no tenant.");
        }

        var consignataria = Consignataria.Cadastrar(tenant.TenantId, cnpj, request.RazaoSocial, request.Tipo);
        consignatarias.Adicionar(consignataria);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return consignataria.Id.Value;
    }
}
