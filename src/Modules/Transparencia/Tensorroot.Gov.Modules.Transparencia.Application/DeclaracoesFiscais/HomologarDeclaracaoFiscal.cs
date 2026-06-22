using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Registra a homologacao da declaracao pelo SICONFI/STN (secao 5.3 das regras).</summary>
/// <param name="DeclaracaoFiscalId">Declaracao a homologar.</param>
public sealed record HomologarDeclaracaoFiscalCommand(Guid DeclaracaoFiscalId) : ICommand;

/// <summary>Regras de validacao da homologacao da declaracao fiscal.</summary>
public sealed class HomologarDeclaracaoFiscalValidator : AbstractValidator<HomologarDeclaracaoFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public HomologarDeclaracaoFiscalValidator()
    {
        RuleFor(comando => comando.DeclaracaoFiscalId).NotEmpty();
    }
}

/// <summary>Handler da homologacao da declaracao fiscal.</summary>
public sealed class HomologarDeclaracaoFiscalHandler(
    IDeclaracaoFiscalRepository declaracoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<HomologarDeclaracaoFiscalCommand>
{
    /// <inheritdoc />
    public async Task Handle(HomologarDeclaracaoFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracao = await declaracoes
            .ObterPorIdAsync(new DeclaracaoFiscalId(request.DeclaracaoFiscalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Declaracao fiscal nao encontrada.");

        declaracao.Homologar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
