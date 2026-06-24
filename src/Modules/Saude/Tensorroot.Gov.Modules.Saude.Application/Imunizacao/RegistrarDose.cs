using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using DomainEstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using DomainProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Imunizacao;

/// <summary>
/// Registra a aplicacao de uma dose de imunobiologico na carteira do paciente: abre a carteira na 1a dose,
/// calcula o aprazamento da proxima (intervalo do esquema) e, se o imunobiologico tiver item de estoque
/// vinculado e o estabelecimento informado, baixa 1 unidade do lote por FEFO — tudo na MESMA transacao.
/// Dado pessoal SENSIVEL de saude (LGPD). // TODO(M10): transmissao ao SI-PNI/RNDS via Outbox apos A1.
/// </summary>
/// <param name="PacienteId">Paciente vacinado.</param>
/// <param name="ImunobiologicoId">Imunobiologico aplicado.</param>
/// <param name="TipoDose">Tipo da dose.</param>
/// <param name="NumeroDose">Numero da dose no esquema (1..TotalDoses).</param>
/// <param name="Lote">Lote aplicado.</param>
/// <param name="AplicadorId">Profissional aplicador.</param>
/// <param name="DataAplicacao">Data de aplicacao.</param>
/// <param name="EstabelecimentoId">Estabelecimento da aplicacao (opcional — habilita baixa de estoque).</param>
public sealed record RegistrarDoseCommand(
    Guid PacienteId,
    Guid ImunobiologicoId,
    TipoDose TipoDose,
    int NumeroDose,
    string Lote,
    Guid AplicadorId,
    DateOnly DataAplicacao,
    Guid? EstabelecimentoId) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de dose.</summary>
public sealed class RegistrarDoseValidator : AbstractValidator<RegistrarDoseCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarDoseValidator()
    {
        RuleFor(c => c.PacienteId).NotEmpty();
        RuleFor(c => c.ImunobiologicoId).NotEmpty();
        RuleFor(c => c.TipoDose).IsInEnum();
        RuleFor(c => c.NumeroDose).GreaterThanOrEqualTo(1);
        RuleFor(c => c.Lote).NotEmpty().MaximumLength(40);
        RuleFor(c => c.AplicadorId).NotEmpty();
    }
}

/// <summary>Handler do registro de dose aplicada.</summary>
public sealed class RegistrarDoseHandler(
    ICarteiraVacinacaoRepository carteiras,
    IImunobiologicoRepository imunobiologicos,
    IPacienteRepository pacientes,
    IEstoqueMedicamentoRepository estoques,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarDoseCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarDoseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pacienteId = new DomainPacienteId(request.PacienteId);

        var paciente = await pacientes.ObterPorIdAsync(pacienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");
        if (!paciente.CnsConfirmado)
        {
            throw new InvalidOperationException("Paciente sem CNS valido/confirmado.");
        }

        var imunobiologico = await imunobiologicos
            .ObterPorIdAsync(new ImunobiologicoId(request.ImunobiologicoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Imunobiologico nao encontrado.");
        if (!imunobiologico.Ativo)
        {
            throw new InvalidOperationException("Imunobiologico inativo no catalogo.");
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var carteira = await carteiras.ObterPorPacienteAsync(pacienteId, cancellationToken).ConfigureAwait(false);
        if (carteira is null)
        {
            carteira = CarteiraVacinacao.Abrir(tenant.TenantId, pacienteId);
            carteiras.Adicionar(carteira);
        }

        // Baixa de estoque do imunobiologico (1 dose), quando vinculado e o estabelecimento informado.
        if (imunobiologico.MedicamentoEstoqueId is { } medicamentoId && request.EstabelecimentoId is { } estabId)
        {
            var estoque = await estoques
                .ObterPorEstabelecimentoEMedicamentoAsync(
                    new DomainEstabelecimentoId(estabId), medicamentoId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Sem estoque do imunobiologico neste estabelecimento.");

            // 1 unidade por dose aplicada (baixa FEFO; saldo insuficiente lanca).
            estoque.Baixar(1m, hoje);
        }

        carteira.RegistrarDose(
            imunobiologico,
            request.TipoDose,
            request.NumeroDose,
            request.Lote,
            new DomainProfissionalId(request.AplicadorId),
            request.DataAplicacao,
            hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return carteira.Id.Value;
    }
}
