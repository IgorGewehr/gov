using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Inspecao/vistoria sanitaria de um <see cref="EstabelecimentoFiscalizavel"/>. Raiz de agregado e
/// fronteira de consistencia do roteiro: enquanto Aberta acumula itens (checklist conforme/nao conforme);
/// ao Concluir, o resultado consolidado e DERIVADO dos itens (invariante, nunca arbitrado), travando a
/// edicao. A maquina de estado (Aberta → Concluida/Cancelada) garante que pendencias e autos so derivem
/// de uma vistoria efetivamente concluida. O fiscal responsavel (Profissional) e opcional (reuso por Id).
/// </summary>
public sealed class Inspecao : AggregateRoot<InspecaoId>, IMustHaveTenant
{
    private readonly List<ItemInspecao> _itens = [];

    private Inspecao()
    {
    }

    private Inspecao(
        InspecaoId id,
        Guid tenantId,
        EstabelecimentoFiscalizavelId estabelecimentoId,
        DateOnly dataInspecao,
        ProfissionalId? fiscalId,
        string? roteiro)
        : base(id)
    {
        TenantId = tenantId;
        EstabelecimentoFiscalizavelId = estabelecimentoId;
        DataInspecao = dataInspecao;
        FiscalId = fiscalId;
        Roteiro = roteiro;
        Situacao = SituacaoInspecao.Aberta;
        RaiseDomainEvent(new InspecaoAberta(id, estabelecimentoId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Estabelecimento inspecionado (referencia por Id).</summary>
    public EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId { get; private set; }

    /// <summary>Data da vistoria (in loco).</summary>
    public DateOnly DataInspecao { get; private set; }

    /// <summary>Fiscal/responsavel tecnico que conduziu a inspecao (opcional — reuso de Profissional por Id).</summary>
    public ProfissionalId? FiscalId { get; private set; }

    /// <summary>Identificacao do roteiro/checklist aplicado (ex.: "Roteiro Alimentacao RDC 216/2004").</summary>
    public string? Roteiro { get; private set; }

    /// <summary>Situacao da inspecao (Aberta/Concluida/Cancelada).</summary>
    public SituacaoInspecao Situacao { get; private set; }

    /// <summary>Resultado consolidado (definido na conclusao; nulo enquanto Aberta).</summary>
    public ResultadoInspecao? Resultado { get; private set; }

    /// <summary>Itens do roteiro verificados (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<ItemInspecao> Itens => _itens;

    /// <summary>
    /// Abre uma inspecao (situacao Aberta) para um estabelecimento. As pre-condicoes (estabelecimento
    /// existente, ativo e fiscalizavel) sao verificadas na orquestracao.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="estabelecimentoId">Estabelecimento a inspecionar.</param>
    /// <param name="dataInspecao">Data da vistoria.</param>
    /// <param name="fiscalId">Fiscal responsavel (opcional).</param>
    /// <param name="roteiro">Roteiro/checklist aplicado (opcional).</param>
    /// <returns>Nova <see cref="Inspecao"/> aberta.</returns>
    /// <exception cref="ArgumentException">Se o estabelecimento for vazio.</exception>
    public static Inspecao Abrir(
        Guid tenantId,
        EstabelecimentoFiscalizavelId estabelecimentoId,
        DateOnly dataInspecao,
        ProfissionalId? fiscalId = null,
        string? roteiro = null)
    {
        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento e obrigatorio.", nameof(estabelecimentoId));
        }

        return new Inspecao(InspecaoId.New(), tenantId, estabelecimentoId, dataInspecao, fiscalId, roteiro?.Trim());
    }

    /// <summary>
    /// Registra um item do roteiro (checklist). Permitido somente enquanto a inspecao esta Aberta
    /// (I-VISA-1: vistoria concluida e imutavel).
    /// </summary>
    /// <param name="requisito">Requisito sanitario verificado.</param>
    /// <param name="conformidade">Conformidade aferida.</param>
    /// <param name="observacao">Observacao (obrigatoria se nao conforme).</param>
    /// <returns>O item registrado.</returns>
    /// <exception cref="InvalidOperationException">Se a inspecao nao estiver Aberta.</exception>
    public ItemInspecao RegistrarItem(string requisito, ConformidadeItem conformidade, string? observacao)
    {
        if (Situacao != SituacaoInspecao.Aberta)
        {
            throw new InvalidOperationException("Itens so podem ser registrados em inspecao aberta.");
        }

        var item = ItemInspecao.Criar(requisito, conformidade, observacao);
        _itens.Add(item);
        return item;
    }

    /// <summary>
    /// Conclui a inspecao: exige ao menos um item, DERIVA o resultado das conformidades (sem item nao
    /// conforme = Aprovado; com pendencias e <paramref name="houveInfracaoGrave"/> falso =
    /// AprovadoComPendencias; com pendencias e infracao grave = Reprovado) e trava a edicao. Emite
    /// <see cref="InspecaoConcluida"/>. O encaminhamento (intimacao/auto) e decidido na orquestracao.
    /// </summary>
    /// <param name="houveInfracaoGrave">
    /// Indica se ha nao conformidade GRAVE (risco iminente a saude) — eleva o desfecho a Reprovado.
    /// </param>
    /// <returns>O resultado consolidado.</returns>
    /// <exception cref="InvalidOperationException">Se nao estiver Aberta ou nao houver itens.</exception>
    public ResultadoInspecao Concluir(bool houveInfracaoGrave)
    {
        if (Situacao != SituacaoInspecao.Aberta)
        {
            throw new InvalidOperationException("So e possivel concluir inspecao aberta.");
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("A inspecao exige ao menos um item no roteiro.");
        }

        var temPendencias = _itens.Any(i => i.EhPendencia());
        Resultado = (temPendencias, houveInfracaoGrave) switch
        {
            (false, _) => ResultadoInspecao.Aprovado,
            (true, true) => ResultadoInspecao.Reprovado,
            (true, false) => ResultadoInspecao.AprovadoComPendencias,
        };

        Situacao = SituacaoInspecao.Concluida;
        RaiseDomainEvent(new InspecaoConcluida(Id, EstabelecimentoFiscalizavelId, Resultado.Value));
        return Resultado.Value;
    }

    /// <summary>Cancela a inspecao (vistoria invalidada) — sem efeitos fiscalizatorios. So enquanto Aberta.</summary>
    /// <param name="motivo">Motivo do cancelamento (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a inspecao ja estiver concluida.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoInspecao.Concluida)
        {
            throw new InvalidOperationException("Inspecao concluida nao pode ser cancelada.");
        }

        Situacao = SituacaoInspecao.Cancelada;
    }

    /// <summary>Quantidade de pendencias (itens nao conformes) — base da intimacao/auto.</summary>
    /// <returns>Numero de itens nao conformes.</returns>
    public int QuantidadePendencias() => _itens.Count(i => i.EhPendencia());

    /// <summary>Indica se a inspecao concluida habilita a emissao de licenca (Aprovado ou com pendencias sanaveis).</summary>
    /// <returns><c>true</c> se concluida e nao reprovada.</returns>
    public bool HabilitaLicenca()
        => Situacao == SituacaoInspecao.Concluida && Resultado != ResultadoInspecao.Reprovado;
}
