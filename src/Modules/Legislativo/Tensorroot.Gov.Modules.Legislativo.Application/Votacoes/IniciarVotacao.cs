using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Abre uma votacao para registro de votos (I-1).</summary>
/// <param name="SessaoId">Sessao em que ocorre a votacao.</param>
/// <param name="ProposicaoId">Materia (proposicao) votada.</param>
/// <param name="Tipo">Modalidade de apuracao (1 = Simbolica, 2 = Nominal, 3 = Secreta).</param>
/// <param name="MaioriaExigida">Criterio de aprovacao (1 = Simples, 2 = Absoluta, 3 = Qualificada).</param>
/// <param name="TotalMembros">Numero de vereadores da Camara (base da maioria absoluta).</param>
/// <param name="Presentes">Numero de vereadores presentes (base da maioria simples).</param>
/// <param name="Turno">Turno da votacao (1 ou 2; qualificada exige 2 turnos).</param>
public sealed record IniciarVotacaoCommand(
    Guid SessaoId,
    Guid ProposicaoId,
    int Tipo,
    int MaioriaExigida,
    int TotalMembros,
    int Presentes,
    int Turno) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de votacao.</summary>
public sealed class IniciarVotacaoValidator : AbstractValidator<IniciarVotacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public IniciarVotacaoValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
        // Tipo/MaioriaExigida trafegam como int; validar contra os valores do enum de dominio
        // (IsInEnum() so vale para propriedade ja tipada como enum — aqui sao Int32).
        RuleFor(comando => comando.Tipo)
            .Must(tipo => Enum.IsDefined(typeof(TipoVotacao), tipo))
            .WithMessage("Tipo de votacao invalido (1 = Simbolica, 2 = Nominal, 3 = Secreta).");
        RuleFor(comando => comando.MaioriaExigida)
            .Must(maioria => Enum.IsDefined(typeof(MaioriaExigida), maioria))
            .WithMessage("Maioria exigida invalida (1 = Simples, 2 = Absoluta, 3 = Qualificada).");
        RuleFor(comando => comando.TotalMembros).GreaterThan(0);
        RuleFor(comando => comando.Presentes).GreaterThan(0);
        RuleFor(comando => comando.Turno).InclusiveBetween(1, 2);
    }
}

/// <summary>Handler da abertura de votacao.</summary>
public sealed class IniciarVotacaoHandler(
    IVotacaoRepository votacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<IniciarVotacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(IniciarVotacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = Votacao.Iniciar(
            tenant.TenantId,
            new SessaoId(request.SessaoId),
            new ProposicaoId(request.ProposicaoId),
            (TipoVotacao)request.Tipo,
            (MaioriaExigida)request.MaioriaExigida,
            request.TotalMembros,
            request.Presentes,
            request.Turno);

        votacoes.Adicionar(votacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return votacao.Id.Value;
    }
}
