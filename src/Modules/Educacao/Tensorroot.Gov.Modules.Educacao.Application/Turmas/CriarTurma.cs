using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Turmas;

/// <summary>Cria uma turma (situacao inicial Planejada), retornando seu identificador.</summary>
/// <param name="EscolaId">Escola da turma.</param>
/// <param name="AnoLetivo">Ano letivo.</param>
/// <param name="Etapa">Etapa/modalidade de ensino.</param>
/// <param name="Serie">Serie/ano.</param>
/// <param name="Turno">Turno de funcionamento.</param>
/// <param name="Vagas">Capacidade de vagas (&gt; 0).</param>
public sealed record CriarTurmaCommand(
    Guid EscolaId,
    int AnoLetivo,
    Etapa Etapa,
    string Serie,
    Turno Turno,
    int Vagas) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao de turma.</summary>
public sealed class CriarTurmaValidator : AbstractValidator<CriarTurmaCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarTurmaValidator()
    {
        RuleFor(comando => comando.EscolaId).NotEmpty().WithMessage("Escola obrigatoria.");
        RuleFor(comando => comando.AnoLetivo).InclusiveBetween(1900, 9999).WithMessage("Ano letivo invalido.");
        RuleFor(comando => comando.Etapa).IsInEnum().WithMessage("Etapa invalida.");
        RuleFor(comando => comando.Serie).NotEmpty().MaximumLength(Turma.ComprimentoSerie).WithMessage("Serie obrigatoria.");
        RuleFor(comando => comando.Turno).IsInEnum().WithMessage("Turno invalido.");
        RuleFor(comando => comando.Vagas).GreaterThan(0).WithMessage("Vagas deve ser maior que zero.");
    }
}

/// <summary>Handler da criacao de turma.</summary>
public sealed class CriarTurmaHandler(
    ITurmaRepository turmas,
    IEscolaRepository escolas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CriarTurmaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarTurmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escolaId = new EscolaId(request.EscolaId);

        // Coerencia: a escola da turma deve existir no tenant.
        var escola = await escolas.ObterPorIdAsync(escolaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Escola nao encontrada.");
        if (escola.Encerrada)
        {
            throw new InvalidOperationException("Escola desativada nao admite novas turmas.");
        }

        // I-T4: nao duplicar turma identica (escola, ano, serie, turno).
        if (await turmas.ExisteDuplicadaAsync(escolaId, request.AnoLetivo, request.Serie.Trim(), request.Turno, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe turma identica (escola, ano, serie, turno).");
        }

        var turma = Turma.Criar(
            tenant.TenantId,
            escolaId,
            request.AnoLetivo,
            request.Etapa,
            request.Serie,
            request.Turno,
            request.Vagas);

        turmas.Adicionar(turma);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return turma.Id.Value;
    }
}
