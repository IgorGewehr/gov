using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Convenios.Application.Recebidos;

/// <summary>
/// Registra a proposta + plano de trabalho de um convenio federal recebido (fluxo A) — estado inicial
/// <c>EmProposta</c>. As somas do plano sao conferidas no agregado (A-INV-3).
/// </summary>
/// <param name="Concedente">Orgao concedente.</param>
/// <param name="Objeto">Objeto/finalidade do plano.</param>
/// <param name="ValorRepasse">Valor do repasse previsto.</param>
/// <param name="ValorContrapartida">Valor da contrapartida prevista.</param>
/// <param name="Etapas">Etapas fisico-financeiras.</param>
/// <param name="Parcelas">Cronograma de desembolso.</param>
public sealed record RegistrarPropostaConvenioCommand(
    ConcedentePayload Concedente,
    string Objeto,
    decimal ValorRepasse,
    decimal ValorContrapartida,
    IReadOnlyList<EtapaPayload> Etapas,
    IReadOnlyList<ParcelaPayload> Parcelas) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de proposta.</summary>
public sealed class RegistrarPropostaConvenioValidator : AbstractValidator<RegistrarPropostaConvenioCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarPropostaConvenioValidator()
    {
        RuleFor(comando => comando.Concedente).NotNull();
        RuleFor(comando => comando.Concedente.Cnpj).NotEmpty();
        RuleFor(comando => comando.Concedente.Nome).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Objeto).NotEmpty().MaximumLength(1000);
        RuleFor(comando => comando.ValorRepasse).GreaterThan(0);
        RuleFor(comando => comando.ValorContrapartida).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.Etapas).NotEmpty();
        RuleFor(comando => comando.Parcelas).NotEmpty();
    }
}

/// <summary>Handler do registro de proposta de convenio recebido.</summary>
public sealed class RegistrarPropostaConvenioHandler(
    IConvenioRecebidoRepository convenios,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<RegistrarPropostaConvenioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarPropostaConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var concedente = OrgaoConcedente.Criar(
            Cnpj.Create(request.Concedente.Cnpj),
            request.Concedente.Nome,
            request.Concedente.Esfera,
            request.Concedente.SistemaOrigem);

        var etapas = request.Etapas
            .Select(etapa => new EtapaPlanoTrabalho(
                etapa.Ordem, etapa.Descricao, Dinheiro.De(etapa.Valor), etapa.InicioPrevisto, etapa.FimPrevisto))
            .ToList();

        var parcelas = request.Parcelas
            .Select(parcela => new ParcelaPrevista(parcela.NumeroOrdem, Dinheiro.De(parcela.Valor), parcela.DataPrevista))
            .ToList();

        var plano = PlanoDeTrabalho.Criar(
            request.Objeto,
            Dinheiro.De(request.ValorRepasse),
            Dinheiro.De(request.ValorContrapartida),
            etapas,
            parcelas);

        var convenio = ConvenioRecebido.RegistrarProposta(tenant.TenantId, concedente, plano);
        convenios.Adicionar(convenio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return convenio.Id.Value;
    }
}
