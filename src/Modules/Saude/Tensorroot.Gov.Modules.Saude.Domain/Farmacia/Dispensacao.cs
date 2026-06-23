using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using PrescricaoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PrescricaoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Entrega de medicamento(s) a um Paciente numa unidade de dispensacao, opcionalmente vinculada a uma
/// Prescricao do PEP. Raiz de agregado: registra QUEM retirou (paciente), ONDE (estabelecimento), QUEM
/// dispensou (profissional/farmaceutico) e o rastro dos lotes (FEFO). Trata dado pessoal SENSIVEL de
/// saude (LGPD art. 11) — o historico de dispensacao por paciente exige trilha de acesso. A baixa de
/// estoque e orquestrada no handler (consistencia transacional: estoque + dispensacao no mesmo SaveChanges).
/// </summary>
public sealed class Dispensacao : AggregateRoot<DispensacaoId>, IMustHaveTenant
{
    private readonly List<ItemDispensado> _itens = [];

    private Dispensacao()
    {
    }

    private Dispensacao(
        DispensacaoId id,
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoId,
        ProfissionalId profissionalId,
        PrescricaoId? prescricaoId,
        DateTimeOffset dataHora)
        : base(id)
    {
        TenantId = tenantId;
        PacienteId = pacienteId;
        EstabelecimentoId = estabelecimentoId;
        ProfissionalId = profissionalId;
        PrescricaoId = prescricaoId;
        DataHora = dataHora;
        Situacao = SituacaoDispensacao.Efetivada;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Paciente que retirou o medicamento (referencia por Id) — dado sensivel (LGPD).</summary>
    public PacienteId PacienteId { get; private set; }

    /// <summary>Estabelecimento/farmacia onde ocorreu a dispensacao (referencia por Id).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Profissional que dispensou (farmaceutico/responsavel — referencia por Id).</summary>
    public ProfissionalId ProfissionalId { get; private set; }

    /// <summary>Prescricao do PEP que originou a dispensacao (opcional — dispensa pode ser avulsa).</summary>
    public PrescricaoId? PrescricaoId { get; private set; }

    /// <summary>Data/hora da entrega.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Situacao da dispensacao (efetivada/estornada).</summary>
    public SituacaoDispensacao Situacao { get; private set; }

    /// <summary>Itens entregues (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<ItemDispensado> Itens => _itens;

    /// <summary>
    /// Inicia uma dispensacao (situacao Efetivada). As baixas de estoque sao adicionadas via
    /// <see cref="AdicionarItem"/> apos o consumo FEFO no <see cref="EstoqueMedicamento"/> (no handler).
    /// As pre-condicoes (paciente ativo/CNS, estabelecimento ativo, profissional vinculado) sao
    /// verificadas na orquestracao.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="pacienteId">Paciente que retira.</param>
    /// <param name="estabelecimentoId">Estabelecimento/farmacia.</param>
    /// <param name="profissionalId">Profissional que dispensa.</param>
    /// <param name="dataHora">Data/hora da entrega.</param>
    /// <param name="prescricaoId">Prescricao de origem (opcional).</param>
    /// <returns>Nova <see cref="Dispensacao"/> efetivada (sem itens ate adicionar).</returns>
    /// <exception cref="ArgumentException">Se algum identificador obrigatorio for vazio.</exception>
    public static Dispensacao Iniciar(
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoId,
        ProfissionalId profissionalId,
        DateTimeOffset dataHora,
        PrescricaoId? prescricaoId = null)
    {
        if (pacienteId.Value == Guid.Empty)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(pacienteId));
        }

        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento e obrigatorio.", nameof(estabelecimentoId));
        }

        if (profissionalId.Value == Guid.Empty)
        {
            throw new ArgumentException("Profissional dispensador e obrigatorio.", nameof(profissionalId));
        }

        return new Dispensacao(DispensacaoId.New(), tenantId, pacienteId, estabelecimentoId, profissionalId, prescricaoId, dataHora);
    }

    /// <summary>
    /// Adiciona um item entregue (com o rastro dos lotes ja baixados no estoque). Permitido somente
    /// enquanto Efetivada.
    /// </summary>
    /// <param name="medicamentoId">Medicamento dispensado.</param>
    /// <param name="quantidade">Quantidade entregue (> 0).</param>
    /// <param name="posologia">Posologia orientada.</param>
    /// <param name="baixas">Rastro das baixas por lote (FEFO).</param>
    /// <exception cref="InvalidOperationException">Se a dispensacao nao estiver Efetivada.</exception>
    public void AdicionarItem(MedicamentoId medicamentoId, decimal quantidade, string posologia, IReadOnlyList<BaixaLote> baixas)
    {
        if (Situacao != SituacaoDispensacao.Efetivada)
        {
            throw new InvalidOperationException("Itens so podem ser adicionados a uma dispensacao efetivada.");
        }

        _itens.Add(ItemDispensado.Criar(medicamentoId, quantidade, posologia, baixas));
    }

    /// <summary>
    /// Conclui a dispensacao (exige ao menos um item) e emite <see cref="MedicamentoDispensado"/> por item
    /// para a trilha LGPD e indicadores. Idempotente quanto a estado (so emite na conclusao Efetivada).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se nao houver itens ou nao estiver Efetivada.</exception>
    public void Concluir()
    {
        if (Situacao != SituacaoDispensacao.Efetivada)
        {
            throw new InvalidOperationException("So e possivel concluir uma dispensacao efetivada.");
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("A dispensacao exige ao menos um item entregue.");
        }

        foreach (var item in _itens)
        {
            RaiseDomainEvent(new MedicamentoDispensado(Id, PacienteId, item.MedicamentoId, item.Quantidade));
        }
    }

    /// <summary>
    /// Estorna a dispensacao (entrega indevida/devolucao): muda a situacao para Estornada. A devolucao do
    /// saldo aos lotes e orquestrada no handler via <see cref="EstoqueMedicamento.Estornar"/>. Emite
    /// <see cref="DispensacaoEstornada"/>.
    /// </summary>
    /// <param name="motivo">Motivo do estorno (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se ja estiver estornada.</exception>
    public void Estornar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoDispensacao.Estornada)
        {
            throw new InvalidOperationException("Dispensacao ja estornada.");
        }

        Situacao = SituacaoDispensacao.Estornada;
        RaiseDomainEvent(new DispensacaoEstornada(Id, PacienteId, motivo.Trim()));
    }

    /// <summary>Baixas por medicamento (para o handler estornar no estoque correto).</summary>
    /// <returns>Sequencia de (medicamento, baixa) cobrindo todos os itens.</returns>
    public IEnumerable<(MedicamentoId MedicamentoId, BaixaLote Baixa)> BaixasPorMedicamento()
        => _itens.SelectMany(i => i.Baixas.Select(b => (i.MedicamentoId, b)));
}
