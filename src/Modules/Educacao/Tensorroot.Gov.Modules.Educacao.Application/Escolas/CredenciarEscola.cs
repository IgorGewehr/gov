using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Application.Escolas;

/// <summary>Credencia uma nova escola na rede de ensino, retornando seu identificador.</summary>
/// <param name="CodigoInep">Codigo INEP unico nacional (8 digitos).</param>
/// <param name="Nome">Nome da unidade escolar.</param>
/// <param name="Dependencia">Dependencia administrativa.</param>
/// <param name="Endereco">Endereco e georreferenciamento da escola.</param>
/// <param name="Infraestrutura">Infraestrutura fisica e acessibilidade.</param>
public sealed record CredenciarEscolaCommand(
    string CodigoInep,
    string Nome,
    DependenciaAdministrativa Dependencia,
    Endereco Endereco,
    Infraestrutura Infraestrutura) : ICommand<Guid>;

/// <summary>Regras de validacao do credenciamento de escola.</summary>
public sealed class CredenciarEscolaValidator : AbstractValidator<CredenciarEscolaCommand>
{
    /// <summary>Define as regras.</summary>
    public CredenciarEscolaValidator()
    {
        RuleFor(comando => comando.CodigoInep)
            .NotEmpty()
            .MaximumLength(8)
            .WithMessage("Codigo INEP obrigatorio (8 digitos).");
        RuleFor(comando => comando.Nome)
            .NotEmpty()
            .MaximumLength(150)
            .WithMessage("Nome da escola obrigatorio (max. 150 caracteres).");
        RuleFor(comando => comando.Dependencia)
            .IsInEnum()
            .WithMessage("Dependencia administrativa invalida.");
    }
}

/// <summary>Handler do credenciamento de escola.</summary>
public sealed class CredenciarEscolaHandler(
    IEscolaRepository escolas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CredenciarEscolaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CredenciarEscolaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var codigoInep = CodigoInep.Criar(request.CodigoInep);

        if (await escolas.ExisteCodigoInepAsync(codigoInep, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Codigo INEP ja cadastrado.");
        }

        var escola = Escola.Credenciar(
            tenant.TenantId,
            codigoInep,
            request.Nome,
            request.Dependencia,
            request.Endereco,
            request.Infraestrutura);

        escolas.Adicionar(escola);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return escola.Id.Value;
    }
}
