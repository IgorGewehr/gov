using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;
using DomainProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>
/// Abre uma inspecao/vistoria sanitaria para um estabelecimento fiscalizavel. Exige estabelecimento
/// existente e Ativo (nao se inspeciona o inexistente; estabelecimento ja interditado/inativo nao
/// entra no ciclo). O fiscal responsavel e opcional (reuso de Profissional por Id).
/// </summary>
/// <param name="EstabelecimentoId">Estabelecimento a inspecionar.</param>
/// <param name="DataInspecao">Data da vistoria.</param>
/// <param name="FiscalId">Fiscal responsavel (opcional).</param>
/// <param name="Roteiro">Roteiro/checklist aplicado (opcional).</param>
public sealed record AbrirInspecaoCommand(
    Guid EstabelecimentoId,
    DateOnly DataInspecao,
    Guid? FiscalId,
    string? Roteiro) : ICommand<Guid>;

/// <summary>Regras de validacao da abertura de inspecao.</summary>
public sealed class AbrirInspecaoValidator : AbstractValidator<AbrirInspecaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirInspecaoValidator()
    {
        RuleFor(c => c.EstabelecimentoId).NotEmpty();
        RuleFor(c => c.Roteiro).MaximumLength(120);
    }
}

/// <summary>Handler da abertura de inspecao.</summary>
public sealed class AbrirInspecaoHandler(
    IEstabelecimentoFiscalizavelRepository estabelecimentos,
    IInspecaoRepository inspecoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirInspecaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirInspecaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoFiscalizavelId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento fiscalizavel nao encontrado.");

        if (!estabelecimento.EstaFiscalizavel())
        {
            throw new InvalidOperationException("Estabelecimento inativo/interditado nao pode ser inspecionado.");
        }

        var fiscal = request.FiscalId is { } id ? new DomainProfissionalId(id) : (DomainProfissionalId?)null;
        var inspecao = Inspecao.Abrir(tenant.TenantId, estabelecimento.Id, request.DataInspecao, fiscal, request.Roteiro);

        inspecoes.Adicionar(inspecao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return inspecao.Id.Value;
    }
}

/// <summary>Registra um item do roteiro de uma inspecao aberta (conforme/nao conforme).</summary>
/// <param name="InspecaoId">Inspecao alvo.</param>
/// <param name="Requisito">Requisito sanitario verificado.</param>
/// <param name="Conformidade">Conformidade aferida.</param>
/// <param name="Observacao">Observacao (obrigatoria se nao conforme).</param>
public sealed record RegistrarItemInspecaoCommand(
    Guid InspecaoId,
    string Requisito,
    ConformidadeItem Conformidade,
    string? Observacao) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de item.</summary>
public sealed class RegistrarItemInspecaoValidator : AbstractValidator<RegistrarItemInspecaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarItemInspecaoValidator()
    {
        RuleFor(c => c.InspecaoId).NotEmpty();
        RuleFor(c => c.Requisito).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Conformidade).IsInEnum();
        RuleFor(c => c.Observacao).MaximumLength(1000);
    }
}

/// <summary>Handler do registro de item de inspecao.</summary>
public sealed class RegistrarItemInspecaoHandler(IInspecaoRepository inspecoes, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarItemInspecaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarItemInspecaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(request.InspecaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inspecao nao encontrada.");

        var item = inspecao.RegistrarItem(request.Requisito, request.Conformidade, request.Observacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id.Value;
    }
}

/// <summary>
/// Conclui a inspecao: deriva o resultado das conformidades (com travamento da edicao). O parametro
/// <paramref name="HouveInfracaoGrave"/> eleva o desfecho a Reprovado quando ha risco iminente a saude.
/// </summary>
/// <param name="InspecaoId">Inspecao a concluir.</param>
/// <param name="HouveInfracaoGrave">Indica nao conformidade grave (risco iminente).</param>
public sealed record ConcluirInspecaoCommand(Guid InspecaoId, bool HouveInfracaoGrave) : ICommand<ResultadoInspecao>;

/// <summary>Handler da conclusao de inspecao.</summary>
public sealed class ConcluirInspecaoHandler(IInspecaoRepository inspecoes, IUnitOfWork unitOfWork)
    : ICommandHandler<ConcluirInspecaoCommand, ResultadoInspecao>
{
    /// <inheritdoc />
    public async Task<ResultadoInspecao> Handle(ConcluirInspecaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(request.InspecaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inspecao nao encontrada.");

        var resultado = inspecao.Concluir(request.HouveInfracaoGrave);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return resultado;
    }
}

/// <summary>Cancela uma inspecao aberta (vistoria invalidada).</summary>
/// <param name="InspecaoId">Inspecao a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarInspecaoCommand(Guid InspecaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do cancelamento de inspecao.</summary>
public sealed class CancelarInspecaoValidator : AbstractValidator<CancelarInspecaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarInspecaoValidator()
    {
        RuleFor(c => c.InspecaoId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler do cancelamento de inspecao.</summary>
public sealed class CancelarInspecaoHandler(IInspecaoRepository inspecoes, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarInspecaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarInspecaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inspecao = await inspecoes.ObterPorIdAsync(new InspecaoId(request.InspecaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inspecao nao encontrada.");

        inspecao.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
