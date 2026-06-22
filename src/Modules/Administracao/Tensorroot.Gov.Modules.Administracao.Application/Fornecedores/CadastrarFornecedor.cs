using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Cadastra um novo fornecedor (CNPJ validado na Receita; nasce <c>Ativo</c>).</summary>
/// <param name="Cnpj">CNPJ do fornecedor (com ou sem mascara).</param>
/// <param name="RazaoSocial">Razao social do fornecedor.</param>
public sealed record CadastrarFornecedorCommand(string Cnpj, string RazaoSocial) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de fornecedor.</summary>
public sealed class CadastrarFornecedorValidator : AbstractValidator<CadastrarFornecedorCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarFornecedorValidator()
    {
        RuleFor(comando => comando.Cnpj)
            .NotEmpty()
            .Must(cnpj => Cnpj.TryCreate(cnpj, out _))
            .WithMessage("CNPJ invalido.");
        RuleFor(comando => comando.RazaoSocial)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Razao social e obrigatoria (max. 200).");
    }
}

/// <summary>Handler do cadastro de fornecedor.</summary>
public sealed class CadastrarFornecedorHandler(
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IReceitaCnpjGateway receita)
    : ICommandHandler<CadastrarFornecedorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarFornecedorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // I-1: CNPJ formalmente valido (DV conferidos via Cnpj.Create).
        var cnpj = Cnpj.Create(request.Cnpj);

        // I-4: unicidade (TenantId, Cnpj).
        if (await fornecedores.ExistePorCnpjAsync(cnpj, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe um fornecedor com este CNPJ no tenant.");
        }

        // I-2: CNPJ confirmado/ativo na Receita (Anti-Corruption Layer).
        if (!await receita.EstaAtivoAsync(cnpj, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("CNPJ inexistente ou baixado na Receita.");
        }

        var fornecedor = Fornecedor.Cadastrar(tenant.TenantId, cnpj, request.RazaoSocial);
        fornecedores.Adicionar(fornecedor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return fornecedor.Id.Value;
    }
}
