using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;

/// <summary>Cadastra um contribuinte pessoa física.</summary>
/// <param name="Cpf">CPF (com ou sem máscara).</param>
/// <param name="Nome">Nome completo.</param>
/// <param name="InscricaoMunicipal">Inscrição municipal opcional.</param>
public sealed record CadastrarContribuintePessoaFisicaCommand(string Cpf, string Nome, string? InscricaoMunicipal)
    : ICommand<Guid>;

/// <summary>Regras de validação do cadastro de contribuinte pessoa física.</summary>
public sealed class CadastrarContribuintePessoaFisicaValidator : AbstractValidator<CadastrarContribuintePessoaFisicaCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarContribuintePessoaFisicaValidator()
    {
        RuleFor(comando => comando.Cpf)
            .NotEmpty()
            .Must(cpf => Cpf.TryCreate(cpf, out _))
            .WithMessage("CPF inválido.");

        RuleFor(comando => comando.Nome)
            .NotEmpty()
            .MaximumLength(200);
    }
}

/// <summary>Handler do cadastro de contribuinte pessoa física.</summary>
public sealed class CadastrarContribuintePessoaFisicaHandler(
    IContribuinteRepository contribuintes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarContribuintePessoaFisicaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarContribuintePessoaFisicaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cpf = Cpf.Create(request.Cpf);
        var contribuinte = Contribuinte.PessoaFisica(tenant.TenantId, cpf, request.Nome, request.InscricaoMunicipal);

        contribuintes.Adicionar(contribuinte);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return contribuinte.Id.Value;
    }
}
