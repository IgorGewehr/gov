using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using DomainProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Dispensa um ou mais medicamentos a um paciente: valida paciente ativo/CNS confirmado e estabelecimento
/// ativo, baixa cada item do estoque por FEFO (lote nao vencido, primeiro a vencer) e registra a entrega
/// vinculada (opcionalmente) a uma prescricao do PEP — tudo na MESMA transacao (consistencia estoque +
/// dispensacao). Dado pessoal SENSIVEL de saude (LGPD): a entrega fica no historico do paciente, lido sob
/// trilha de acesso.
///
/// CONTROLE ESPECIAL (Portaria SVS/MS 344/1998 — base do SNGPC): para item cujo
/// <see cref="Tensorroot.Gov.Modules.Saude.Domain.Farmacia.Medicamento.ExigeReceitaControlada"/> e
/// verdadeiro (entorpecente/psicotropico, listas A/B etc.), a dispensacao exige — FAIL-CLOSED — uma
/// <c>PrescricaoId</c> de origem que EXISTA no tenant e PERTENCA ao mesmo paciente (retencao de receita
/// com rastro). Sem isso, a dispensacao e barrada. // TODO(M10): escrituracao SNGPC e integracao HORUS.
/// </summary>
/// <param name="PacienteId">Paciente que retira.</param>
/// <param name="EstabelecimentoId">Estabelecimento/farmacia.</param>
/// <param name="ProfissionalId">Profissional que dispensa (farmaceutico/responsavel).</param>
/// <param name="PrescricaoId">Prescricao de origem (opcional).</param>
/// <param name="Itens">Itens a dispensar (medicamento + quantidade + posologia).</param>
public sealed record DispensarMedicamentoCommand(
    Guid PacienteId,
    Guid EstabelecimentoId,
    Guid ProfissionalId,
    Guid? PrescricaoId,
    IReadOnlyList<ItemDispensacaoInput> Itens) : ICommand<Guid>;

/// <summary>Regras de validacao da dispensacao.</summary>
public sealed class DispensarMedicamentoValidator : AbstractValidator<DispensarMedicamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public DispensarMedicamentoValidator()
    {
        RuleFor(c => c.PacienteId).NotEmpty();
        RuleFor(c => c.EstabelecimentoId).NotEmpty();
        RuleFor(c => c.ProfissionalId).NotEmpty();
        RuleFor(c => c.Itens).NotEmpty().WithMessage("A dispensacao exige ao menos um item.");
        RuleForEach(c => c.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicamentoId).NotEmpty();
            item.RuleFor(i => i.Quantidade).GreaterThan(0m);
            item.RuleFor(i => i.Posologia).NotEmpty().MaximumLength(500);
        });
    }
}

/// <summary>Handler da dispensacao de medicamento.</summary>
public sealed class DispensarMedicamentoHandler(
    IPacienteRepository pacientes,
    IEstabelecimentoRepository estabelecimentos,
    IMedicamentoRepository medicamentos,
    IEstoqueMedicamentoRepository estoques,
    IDispensacaoRepository dispensacoes,
    IAtendimentoRepository atendimentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<DispensarMedicamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(DispensarMedicamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Itens);

        var agora = timeProvider.GetUtcNow();
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);

        // Paciente ativo com CNS confirmado (mesma pre-condicao do atendimento).
        var paciente = await pacientes
            .ObterPorIdAsync(new DomainPacienteId(request.PacienteId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");
        if (!paciente.CnsConfirmado)
        {
            throw new InvalidOperationException("Paciente sem CNS valido/confirmado.");
        }

        var estabelecimentoId = new EstabelecimentoId(request.EstabelecimentoId);
        var competencia = Competencia.De(agora);
        var estabelecimentoAtivo = await estabelecimentos
            .EstabelecimentoAtivoAsync(estabelecimentoId, competencia, cancellationToken)
            .ConfigureAwait(false);
        if (!estabelecimentoAtivo)
        {
            throw new InvalidOperationException("Estabelecimento (CNES) inativo na competencia.");
        }

        var prescricaoId = request.PrescricaoId is { } p ? new PrescricaoId(p) : (PrescricaoId?)null;

        // Controle especial (Portaria 344/1998): a prescricao de origem so legitima a dispensacao de
        // controlado se EXISTIR no tenant e PERTENCER ao mesmo paciente. Resolvido uma vez (vale para
        // todos os itens da entrega) e SO quando ha PrescricaoId — evita consulta desnecessaria.
        var prescricaoValidaParaPaciente = false;
        if (prescricaoId is { } prescricao)
        {
            prescricaoValidaParaPaciente = await atendimentos
                .PrescricaoPertenceAoPacienteAsync(
                    prescricao,
                    new Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId(request.PacienteId),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var dispensacao = Dispensacao.Iniciar(
            tenant.TenantId,
            new DomainPacienteId(request.PacienteId),
            estabelecimentoId,
            new DomainProfissionalId(request.ProfissionalId),
            agora,
            prescricaoId);

        foreach (var item in request.Itens)
        {
            var medicamentoId = new MedicamentoId(item.MedicamentoId);

            // Carrega o agregado de controle. Medicamento inexistente/inativo no catalogo nao dispensa.
            var medicamento = await medicamentos
                .ObterPorIdAsync(medicamentoId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Medicamento nao encontrado no catalogo (REMUME).");
            if (!medicamento.Ativo)
            {
                throw new InvalidOperationException("Medicamento inativo no catalogo (REMUME) — dispensacao vedada.");
            }

            // FAIL-CLOSED (Portaria 344/1998 / SNGPC): controlado exige receita/prescricao valida com rastro.
            if (medicamento.ExigeReceitaControlada && !prescricaoValidaParaPaciente)
            {
                throw new InvalidOperationException(
                    "Dispensacao de medicamento sob controle especial (Portaria 344/1998) exige prescricao "
                    + "valida do paciente (retencao de receita / rastro SNGPC).");
            }

            var estoque = await estoques
                .ObterPorEstabelecimentoEMedicamentoAsync(estabelecimentoId, medicamentoId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Sem estoque do medicamento neste estabelecimento.");

            // FEFO: baixa lotes nao vencidos, primeiro a vencer. Saldo insuficiente lanca (I-FARM-1/3).
            var baixas = estoque.Baixar(item.Quantidade, hoje);
            dispensacao.AdicionarItem(medicamentoId, item.Quantidade, item.Posologia, baixas);
        }

        dispensacao.Concluir();
        dispensacoes.Adicionar(dispensacao);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dispensacao.Id.Value;
    }
}
