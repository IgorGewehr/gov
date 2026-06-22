using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Application.Escolas;

/// <summary>Atualiza os dados cadastrais exigidos pelo EducaCenso de uma escola (endereco e infraestrutura).</summary>
/// <param name="EscolaId">Identificador da escola.</param>
/// <param name="Endereco">Novo endereco/georreferenciamento.</param>
/// <param name="Infraestrutura">Nova infraestrutura.</param>
public sealed record AtualizarDadosCensoCommand(
    Guid EscolaId,
    Endereco Endereco,
    Infraestrutura Infraestrutura) : ICommand;

/// <summary>Regras de validacao da atualizacao de dados do Censo.</summary>
public sealed class AtualizarDadosCensoValidator : AbstractValidator<AtualizarDadosCensoCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarDadosCensoValidator()
    {
        RuleFor(comando => comando.EscolaId)
            .NotEmpty()
            .WithMessage("Identificador da escola obrigatorio.");
    }
}

/// <summary>Handler da atualizacao de dados do Censo.</summary>
public sealed class AtualizarDadosCensoHandler(
    IEscolaRepository escolas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AtualizarDadosCensoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarDadosCensoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escola = await escolas.ObterPorIdAsync(new EscolaId(request.EscolaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Escola nao encontrada.");

        escola.AtualizarDadosCenso(request.Endereco, request.Infraestrutura);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
