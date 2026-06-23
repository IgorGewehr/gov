using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Beneficios;

/// <summary>
/// Dados faticos do requerente para a avaliacao de elegibilidade (idade, deficiencia/avaliacao
/// biopsicossocial, acumulacao da Seguridade, inscricao no CadUnico). Subsidiam a decisao do
/// dominio; nao trafegam no barramento (minimizacao — LGPD art. 11).
/// </summary>
/// <param name="Idade">Idade do requerente em anos (criterio BPC idoso).</param>
/// <param name="PossuiDeficiencia">Indica deficiencia (PCD) declarada.</param>
/// <param name="PossuiAvaliacaoBiopsicossocial">Avaliacao biopsicossocial concluida (requisito BPC/PCD).</param>
/// <param name="AcumulaSeguridadeSocial">Indica beneficio concomitante da Seguridade (veda BPC).</param>
/// <param name="InscritoCadUnico">Indica inscricao no CadUnico (preferencia em eventual).</param>
public sealed record DadosElegibilidadeDto(
    int Idade,
    bool PossuiDeficiencia,
    bool PossuiAvaliacaoBiopsicossocial,
    bool AcumulaSeguridadeSocial,
    bool InscritoCadUnico);

/// <summary>
/// Avalia a elegibilidade e registra a concessao ou o indeferimento de um beneficio, aplicando o
/// criterio vigente na competencia (nunca hardcoded — Beneficio I-1). Publica
/// <see cref="BeneficioConcedidoIntegrationEvent"/> ou
/// <see cref="BeneficioIndeferidoIntegrationEvent"/> (Outbox).
/// </summary>
/// <param name="FamiliaId">Familia requerente.</param>
/// <param name="Tipo">Tipo do beneficio (Bpc/Pbf/Eventual).</param>
/// <param name="Competencia">Competencia (ano/mes) de referencia.</param>
/// <param name="Dados">Dados faticos de elegibilidade do requerente.</param>
/// <param name="Valor">Valor a conceder quando elegivel (nulo em cesta basica/provisao em especie).</param>
/// <param name="Modalidade">
/// A-0: modalidade do beneficio EVENTUAL (natalidade/morte/vulnerabilidade temporaria/calamidade) —
/// obrigatoria quando <paramref name="Tipo"/> e <see cref="TipoBeneficio.Eventual"/>; ignorada nos demais.
/// O criterio do eventual e 100% de lei municipal (sem teto federal de 1/4 SM revogado).
/// </param>
public sealed record AvaliarElegibilidadeBeneficioCommand(
    Guid FamiliaId,
    TipoBeneficio Tipo,
    Competencia Competencia,
    DadosElegibilidadeDto Dados,
    decimal? Valor,
    ModalidadeBeneficioEventual? Modalidade = null) : ICommand<Guid>;

/// <summary>Regras de validacao da avaliacao de elegibilidade.</summary>
public sealed class AvaliarElegibilidadeBeneficioValidator : AbstractValidator<AvaliarElegibilidadeBeneficioCommand>
{
    /// <summary>Define as regras.</summary>
    public AvaliarElegibilidadeBeneficioValidator()
    {
        RuleFor(comando => comando.FamiliaId)
            .NotEmpty()
            .WithMessage("Familia e obrigatoria.");

        RuleFor(comando => comando.Tipo)
            .IsInEnum()
            .WithMessage("Tipo de beneficio invalido.");

        RuleFor(comando => comando.Competencia)
            .Must(competencia => competencia.EhValida())
            .WithMessage("Competencia (ano/mes) e obrigatoria e valida.");

        RuleFor(comando => comando.Dados)
            .NotNull()
            .WithMessage("Dados de elegibilidade sao obrigatorios.");

        // A-0: o eventual exige a modalidade (lei municipal); os demais ignoram.
        RuleFor(comando => comando.Modalidade)
            .NotNull()
            .When(comando => comando.Tipo == TipoBeneficio.Eventual)
            .WithMessage("Modalidade do beneficio eventual e obrigatoria (lei municipal).");
    }
}

/// <summary>Handler da avaliacao de elegibilidade do beneficio.</summary>
public sealed class AvaliarElegibilidadeBeneficioHandler(
    IFamiliaRepository familias,
    IBeneficioRepository beneficios,
    IParametroVigenteProvider parametros,
    ICriterioBeneficioEventualProvider criteriosEventual,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IPublisher publisher)
    : ICommandHandler<AvaliarElegibilidadeBeneficioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AvaliarElegibilidadeBeneficioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var familia = await familias.ObterPorIdAsync(new FamiliaId(request.FamiliaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Familia nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var beneficio = Beneficio.Solicitar(tenant.TenantId, request.FamiliaId, request.Tipo, request.Competencia);

        // I-10: familia com cadastro vencido tem novos beneficios condicionados a regularizacao.
        if (!familia.EstaAptaANovosBeneficios(hoje))
        {
            beneficio.Indeferir("Cadastro desatualizado: regularize a atualizacao no CadUnico para concorrer a novos beneficios.", hoje);
            await PersistirEPublicarAsync(beneficio, cancellationToken).ConfigureAwait(false);
            return beneficio.Id.Value;
        }

        // I-1/B-11: salario minimo vigente obtido para a competencia — nunca hardcoded.
        var salarioMinimo = await parametros.ObterSalarioMinimoVigenteAsync(request.Competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Salario minimo nao parametrizado para a competencia {request.Competencia}.");

        var valor = request.Valor.HasValue ? ValorMonetario.De(request.Valor.Value) : null;

        if (request.Tipo == TipoBeneficio.Eventual)
        {
            // A-0: beneficio eventual NAO tem teto federal de 1/4 SM (revogado pela Lei 12.435/2011).
            // O criterio (modalidade + corte de renda, ou sem corte) vem 100% da lei municipal versionada.
            var modalidade = request.Modalidade
                ?? throw new InvalidOperationException("Modalidade do beneficio eventual e obrigatoria (lei municipal).");

            var criterioMunicipal = await criteriosEventual.ObterCriterioVigenteAsync(modalidade, request.Competencia, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Lei municipal nao habilita/parametriza a modalidade {modalidade} de beneficio eventual para a competencia {request.Competencia}. " +
                    "Nao se aplica default federal (sem teto de 1/4 SM revogado).");

            beneficio.AvaliarElegibilidadeEventual(criterioMunicipal, familia.RendaPerCapita, salarioMinimo, valor, hoje);

            await PersistirEPublicarAsync(beneficio, cancellationToken).ConfigureAwait(false);
            return beneficio.Id.Value;
        }

        var criterio = CriterioElegibilidade.Vigente(request.Tipo, salarioMinimo);

        var dados = new DadosElegibilidade(
            request.Dados.Idade,
            request.Dados.PossuiDeficiencia,
            request.Dados.PossuiAvaliacaoBiopsicossocial,
            request.Dados.AcumulaSeguridadeSocial,
            request.Dados.InscritoCadUnico,
            familia.RendaPerCapita.Valor,
            Math.Max(familia.Membros.Count, 1));

        // I-2/I-3: decisao por criterio FEDERAL vigente (BPC/PBF), despachada para Conceder ou Indeferir.
        beneficio.AvaliarElegibilidade(criterio, dados, familia.RendaPerCapita, valor, hoje);

        await PersistirEPublicarAsync(beneficio, cancellationToken).ConfigureAwait(false);
        return beneficio.Id.Value;
    }

    private async Task PersistirEPublicarAsync(Beneficio beneficio, CancellationToken cancellationToken)
    {
        beneficios.Adicionar(beneficio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var agora = timeProvider.GetUtcNow().UtcDateTime;
        if (beneficio.Situacao == SituacaoBeneficio.Concedida)
        {
            var concedido = new BeneficioConcedidoIntegrationEvent(
                Guid.NewGuid(),
                agora,
                tenant.TenantId,
                beneficio.Id.Value,
                beneficio.FamiliaId,
                beneficio.Tipo.ToString(),
                beneficio.Competencia.ToString(),
                beneficio.Valor?.Valor);

            await publisher.Publish(concedido, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var indeferido = new BeneficioIndeferidoIntegrationEvent(
                Guid.NewGuid(),
                agora,
                tenant.TenantId,
                beneficio.Id.Value,
                beneficio.FamiliaId,
                beneficio.Tipo.ToString(),
                beneficio.MotivoIndeferimento ?? string.Empty);

            await publisher.Publish(indeferido, cancellationToken).ConfigureAwait(false);
        }
    }
}
