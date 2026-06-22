using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Abre uma solicitacao de regulacao de procedimento (SIGTAP) para um paciente.</summary>
/// <param name="PacienteId">Paciente da solicitacao.</param>
/// <param name="EstabelecimentoSolicitanteId">Unidade solicitante (CNES).</param>
/// <param name="ProfissionalSolicitanteId">Profissional solicitante.</param>
/// <param name="CodigoSigtap">Codigo SIGTAP do procedimento.</param>
/// <param name="DescricaoProcedimento">Descricao do procedimento.</param>
/// <param name="Prioridade">Classificacao de risco/urgencia.</param>
/// <param name="Justificativa">Justificativa clinica do pedido.</param>
public sealed record SolicitarRegulacaoCommand(
    Guid PacienteId,
    Guid EstabelecimentoSolicitanteId,
    Guid ProfissionalSolicitanteId,
    string CodigoSigtap,
    string DescricaoProcedimento,
    Prioridade Prioridade,
    string Justificativa) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de solicitacao de regulacao.</summary>
public sealed class SolicitarRegulacaoValidator : AbstractValidator<SolicitarRegulacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public SolicitarRegulacaoValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.EstabelecimentoSolicitanteId).NotEmpty().WithMessage("Estabelecimento solicitante e obrigatorio.");
        RuleFor(comando => comando.ProfissionalSolicitanteId).NotEmpty().WithMessage("Profissional solicitante e obrigatorio.");
        RuleFor(comando => comando.CodigoSigtap).NotEmpty().MaximumLength(10).WithMessage("Codigo SIGTAP e obrigatorio (max. 10).");
        RuleFor(comando => comando.Prioridade).IsInEnum().WithMessage("Prioridade invalida.");
        RuleFor(comando => comando.Justificativa).NotEmpty().MaximumLength(2000).WithMessage("Justificativa e obrigatoria (max. 2000).");
    }
}

/// <summary>Handler da abertura de solicitacao de regulacao.</summary>
public sealed class SolicitarRegulacaoHandler(
    IPacienteRepository pacientes,
    ISolicitacaoRegulacaoRepository solicitacoes,
    ICotaRepository cotas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<SolicitarRegulacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(SolicitarRegulacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // I-2: paciente deve existir com CNS valido/confirmado (CADSUS).
        var paciente = await pacientes
            .ObterPorIdAsync(new DomainPacienteId(request.PacienteId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        if (!paciente.CnsConfirmado)
        {
            throw new InvalidOperationException("Paciente sem CNS valido/confirmado.");
        }

        // Cota aplicavel ao procedimento na unidade solicitante (snapshot).
        var estabelecimentoId = new EstabelecimentoId(request.EstabelecimentoSolicitanteId);
        var cota = await cotas
            .ObterCotaAsync(request.CodigoSigtap, estabelecimentoId, cancellationToken)
            .ConfigureAwait(false);

        var procedimento = Procedimento.Criar(request.CodigoSigtap, request.DescricaoProcedimento);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var solicitacao = SolicitacaoRegulacao.Solicitar(
            tenant.TenantId,
            new PacienteId(request.PacienteId),
            estabelecimentoId,
            new ProfissionalId(request.ProfissionalSolicitanteId),
            procedimento,
            request.Prioridade,
            cota,
            request.Justificativa,
            hoje);

        solicitacoes.Adicionar(solicitacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return solicitacao.Id.Value;
    }
}
