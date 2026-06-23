using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Censo;

/// <summary>
/// 3d.2: abre (se preciso) e (re)consolida o formulario do Censo SUAS de uma unidade num exercicio,
/// DERIVANDO o volume anual de atendimentos do RMA e a quantidade de familias referenciadas do cadastro
/// de familias (sem dupla digitacao). Idempotente enquanto em consolidacao.
/// </summary>
/// <param name="UnidadeId">Unidade socioassistencial a consolidar.</param>
/// <param name="Exercicio">Exercicio (ano) de referencia.</param>
public sealed record ConsolidarCensoCommand(Guid UnidadeId, int Exercicio) : ICommand<Guid>;

/// <summary>Validacao da consolidacao do Censo.</summary>
public sealed class ConsolidarCensoValidator : AbstractValidator<ConsolidarCensoCommand>
{
    /// <summary>Define as regras.</summary>
    public ConsolidarCensoValidator()
    {
        RuleFor(c => c.UnidadeId).NotEmpty();
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
    }
}

/// <summary>Handler da consolidacao do Censo SUAS a partir das fontes locais.</summary>
public sealed class ConsolidarCensoHandler(
    IFormularioCensoSuasRepository formularios,
    IUnidadeSocioassistencialRepository unidades,
    IConsolidacaoCensoReadModel consolidacao,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConsolidarCensoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConsolidarCensoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var unidadeId = new UnidadeSocioassistencialId(request.UnidadeId);

        var unidade = await unidades.ObterPorIdAsync(unidadeId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade socioassistencial inexistente no tenant.");

        var formulario = await formularios.ObterPorUnidadeExercicioAsync(unidadeId, request.Exercicio, cancellationToken).ConfigureAwait(false);
        if (formulario is null)
        {
            formulario = FormularioCensoSuas.Abrir(tenant.TenantId, unidadeId, request.Exercicio);
            await formularios.AdicionarAsync(formulario, cancellationToken).ConfigureAwait(false);
        }

        // Volume e familias DERIVAM do RMA/Familia (fontes unicas) — sem digitacao paralela. O Id da
        // unidade do Censo deve ser o mesmo UnidadeAtendimentoId referenciado por RMA/Familia (Id reusado).
        // // TODO(M10): ao migrar do read model UnidadeAtendimentoLookup para este cadastro, cadastrar a
        // unidade com o UnidadeAtendimentoId pre-existente (Id estavel) para amarrar volumes 1-1.
        var volume = await consolidacao.SomarAtendimentosDoExercicioAsync(request.UnidadeId, request.Exercicio, cancellationToken).ConfigureAwait(false);
        var familias = await consolidacao.ContarFamiliasReferenciadasAsync(request.UnidadeId, cancellationToken).ConfigureAwait(false);

        formulario.Consolidar(
            quantidadeProfissionais: unidade.QuantidadeProfissionais,
            quantidadeServicosOfertados: unidade.Servicos.Count,
            familiasReferenciadas: familias,
            volumeAtendimentosAno: volume);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return formulario.Id.Value;
    }
}

/// <summary>3d.2: fecha (sela) o formulario do Censo de uma unidade no exercicio para envio ao SAGI/MDS.</summary>
/// <param name="UnidadeId">Unidade socioassistencial.</param>
/// <param name="Exercicio">Exercicio (ano) a fechar.</param>
public sealed record FecharCensoCommand(Guid UnidadeId, int Exercicio) : ICommand;

/// <summary>Validacao do fechamento do Censo.</summary>
public sealed class FecharCensoValidator : AbstractValidator<FecharCensoCommand>
{
    /// <summary>Define as regras.</summary>
    public FecharCensoValidator()
    {
        RuleFor(c => c.UnidadeId).NotEmpty();
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
    }
}

/// <summary>Handler do fechamento do Censo.</summary>
public sealed class FecharCensoHandler(
    IFormularioCensoSuasRepository formularios,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<FecharCensoCommand>
{
    /// <inheritdoc />
    public async Task Handle(FecharCensoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var formulario = await formularios.ObterPorUnidadeExercicioAsync(new UnidadeSocioassistencialId(request.UnidadeId), request.Exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Formulario do Censo inexistente no tenant (consolide antes de fechar).");

        formulario.Fechar(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
