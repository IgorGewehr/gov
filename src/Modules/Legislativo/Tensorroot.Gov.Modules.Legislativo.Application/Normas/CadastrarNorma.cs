using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Normas;

/// <summary>Cadastra (promulga) uma norma juridica na base consultavel do tenant.</summary>
/// <param name="Tipo">Especie da norma (<see cref="TipoNorma"/>).</param>
/// <param name="Numero">Numero (positivo).</param>
/// <param name="Ano">Ano de promulgacao.</param>
/// <param name="Ementa">Resumo do objeto.</param>
/// <param name="DataPromulgacao">Data de promulgacao.</param>
/// <param name="TextoArticulado">Texto articulado (opcional).</param>
/// <param name="ProposicaoOrigemId">Proposicao de origem (opcional).</param>
public sealed record CadastrarNormaCommand(
    int Tipo,
    int Numero,
    int Ano,
    string Ementa,
    DateOnly DataPromulgacao,
    string? TextoArticulado,
    Guid? ProposicaoOrigemId) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de norma.</summary>
public sealed class CadastrarNormaValidator : AbstractValidator<CadastrarNormaCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarNormaValidator()
    {
        RuleFor(comando => comando.Tipo).Must(valor => Enum.IsDefined(typeof(TipoNorma), valor))
            .WithMessage("Tipo de norma invalido.");
        RuleFor(comando => comando.Numero).GreaterThan(0);
        RuleFor(comando => comando.Ano).InclusiveBetween(Norma.AnoMinimo, Norma.AnoMaximo);
        RuleFor(comando => comando.Ementa).NotEmpty().MaximumLength(Ementa.ComprimentoMaximo);
    }
}

/// <summary>Handler do cadastro de norma (verifica unicidade logica antes de persistir — N-2).</summary>
public sealed class CadastrarNormaHandler(
    INormaRepository normas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<CadastrarNormaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarNormaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipo = (TipoNorma)request.Tipo;
        if (await normas.ExisteAsync(tipo, request.Numero, request.Ano, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Ja existe {tipo} n. {request.Numero}/{request.Ano} neste tenant.");
        }

        var norma = Norma.Promulgar(
            tenant.TenantId,
            tipo,
            request.Numero,
            request.Ano,
            Ementa.De(request.Ementa),
            request.DataPromulgacao,
            request.TextoArticulado,
            request.ProposicaoOrigemId is { } pid ? new ProposicaoId(pid) : null);

        normas.Adicionar(norma);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return norma.Id.Value;
    }
}
