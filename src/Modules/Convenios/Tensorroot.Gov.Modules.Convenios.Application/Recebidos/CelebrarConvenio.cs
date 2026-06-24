using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Contracts;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

namespace Tensorroot.Gov.Modules.Convenios.Application.Recebidos;

/// <summary>
/// Celebra um convenio federal recebido (fluxo A): exige plano aprovado (transicao previa), valida a
/// contrapartida contra o percentual minimo do tenant (A-INV-2) e atribui o numero do Transferegov. Publica,
/// via Outbox, <see cref="ConvenioRecebidoCelebradoIntegrationEvent"/> (receita + reserva) e
/// <see cref="ContrapartidaConvenioAEmpenharIntegrationEvent"/> (empenho da contrapartida em Financas).
/// </summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="ModalidadeContrapartida">Modalidade da contrapartida (financeira/bens-servicos).</param>
/// <param name="ValorContrapartida">Valor pactuado da contrapartida.</param>
/// <param name="NumeroConvenioTransferegov">Numero do convenio no Transferegov.</param>
/// <param name="ClassificacaoSugerida">Classificacao orcamentaria sugerida do empenho (opcional).</param>
public sealed record CelebrarConvenioCommand(
    Guid ConvenioId,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    ModalidadeContrapartida ModalidadeContrapartida,
    decimal ValorContrapartida,
    string NumeroConvenioTransferegov,
    string? ClassificacaoSugerida) : ICommand;

/// <summary>Regras de validacao da celebracao.</summary>
public sealed class CelebrarConvenioValidator : AbstractValidator<CelebrarConvenioCommand>
{
    /// <summary>Define as regras.</summary>
    public CelebrarConvenioValidator()
    {
        RuleFor(comando => comando.ConvenioId).NotEmpty();
        RuleFor(comando => comando.VigenciaFim).GreaterThanOrEqualTo(comando => comando.VigenciaInicio);
        RuleFor(comando => comando.ValorContrapartida).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.NumeroConvenioTransferegov).NotEmpty().MaximumLength(60);
    }
}

/// <summary>Handler da celebracao de convenio recebido.</summary>
public sealed class CelebrarConvenioHandler(
    IConvenioRecebidoRepository convenios,
    IConveniosParametros parametros,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider relogio)
    : ICommandHandler<CelebrarConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(CelebrarConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);

        // Percentual minimo de contrapartida do tenant (sem numero magico — A-INV-2 / S16).
        var parametroPercentual = parametros.PercentualContrapartidaMinimo(tenant.TenantId);
        var contrapartida = Contrapartida.Criar(
            request.ModalidadeContrapartida,
            Dinheiro.De(request.ValorContrapartida),
            parametroPercentual.Percentual,
            parametroPercentual.NormaFonte);

        var vigencia = Vigencia.Criar(request.VigenciaInicio, request.VigenciaFim);
        convenio.Celebrar(vigencia, contrapartida, request.NumeroConvenioTransferegov);

        var agora = relogio.GetUtcNow().UtcDateTime;

        integrationEvents.Enfileirar(new ConvenioRecebidoCelebradoIntegrationEvent(
            Guid.NewGuid(),
            agora,
            tenant.TenantId,
            convenio.Id.Value,
            convenio.Concedente.Cnpj.Digitos,
            convenio.Concedente.Nome,
            request.NumeroConvenioTransferegov,
            convenio.Plano.ValorGlobal.Valor,
            contrapartida.ValorPactuado.Valor,
            request.VigenciaInicio,
            request.VigenciaFim));

        integrationEvents.Enfileirar(new ContrapartidaConvenioAEmpenharIntegrationEvent(
            Guid.NewGuid(),
            agora,
            tenant.TenantId,
            convenio.Id.Value,
            contrapartida.ValorPactuado.Valor,
            request.ClassificacaoSugerida));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
