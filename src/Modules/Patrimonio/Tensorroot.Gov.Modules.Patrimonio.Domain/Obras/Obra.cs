using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>Identificador forte do agregado <see cref="Obra"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ObraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ObraId"/>.</returns>
    public static ObraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Obra pública / serviço de engenharia (Lei 14.133/2021). É-um <b>bem patrimonial em formação</b>
/// (imobilizado em curso — MCASP) que, ao concluir, é incorporado ao acervo como <see cref="BemPatrimonial"/>.
/// Nasce de um contrato NLLC (vínculo por <c>ContratoId</c>, cross-context) e controla a execução física:
/// cronograma físico-financeiro (curva S), medição/RDO e fiscalização (fiscal designado, ocorrências,
/// paralisação/reinício). Raiz de agregado; espelha o arquétipo do <see cref="Frota.Veiculo"/> (composição
/// com <c>BemPatrimonialId</c> populado só na incorporação). Invariantes I-1…I-16 protegidas no agregado.
/// </summary>
public sealed partial class Obra : AggregateRoot<ObraId>, IMustHaveTenant
{
    private readonly List<EtapaCronograma> _etapas = [];
    private readonly List<Medicao> _medicoes = [];
    private readonly List<RegistroDiarioObra> _registrosDiarios = [];
    private readonly List<DesignacaoFiscal> _designacoesFiscais = [];
    private readonly List<OcorrenciaFiscalizacao> _ocorrencias = [];
    private readonly List<EventoParalisacao> _paralisacoes = [];

    private Obra()
    {
    }

    private Obra(
        ObraId id,
        Guid tenantId,
        Guid contratoId,
        Guid fornecedorId,
        string objeto,
        LocalizacaoObra localizacao,
        RegimeExecucao regimeExecucao,
        ValorMonetario valorContratado,
        DateOnly dataAssinaturaContrato)
        : base(id)
    {
        TenantId = tenantId;
        ContratoId = contratoId;
        FornecedorId = fornecedorId;
        Objeto = objeto;
        Localizacao = localizacao;
        RegimeExecucao = regimeExecucao;
        ValorContratado = valorContratado;
        DataAssinaturaContrato = dataAssinaturaContrato;
        ValorMedidoAcumulado = ValorMonetario.Zero;
        PercentualFisicoAcumulado = 0m;
        Situacao = SituacaoObra.Planejada;
        RaiseDomainEvent(new ObraAberta(id, contratoId, valorContratado.Valor));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Vínculo ao contrato NLLC de origem (FK lógica entre contextos — Administração).</summary>
    public Guid ContratoId { get; private set; }

    /// <summary>Contratada/executora (snapshot do evento de contrato).</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Bem patrimonial resultante; nulo até a conclusão/incorporação (I-13).</summary>
    public BemPatrimonialId? BemPatrimonialId { get; private set; }

    /// <summary>Descrição da obra/serviço de engenharia.</summary>
    public string Objeto { get; private set; } = default!;

    /// <summary>Localização da obra.</summary>
    public LocalizacaoObra Localizacao { get; private set; } = default!;

    /// <summary>Regime de execução (Lei 14.133/2021, art. 46).</summary>
    public RegimeExecucao RegimeExecucao { get; private set; }

    /// <summary>Valor contratado — teto da medição acumulada (I-1); reajustado por aditivo (I-2).</summary>
    public ValorMonetario ValorContratado { get; private set; } = default!;

    /// <summary>Data de assinatura do contrato — base do relógio art. 94 §3 (25 d.u.).</summary>
    public DateOnly DataAssinaturaContrato { get; private set; }

    /// <summary>Data da emissão da Ordem de Início de Serviço (nula antes da ordem).</summary>
    public DateOnly? DataInicioOrdemServico { get; private set; }

    /// <summary>Data de conclusão — base do relógio art. 94 §3 (45 d.u.); nula antes de concluir.</summary>
    public DateOnly? DataConclusao { get; private set; }

    /// <summary>Situação da obra no ciclo de execução.</summary>
    public SituacaoObra Situacao { get; private set; }

    /// <summary>Percentual físico acumulado (derivado das etapas/medições — I-6); nunca atribuído direto.</summary>
    public decimal PercentualFisicoAcumulado { get; private set; }

    /// <summary>Valor medido acumulado (soma das medições aprovadas — I-1).</summary>
    public ValorMonetario ValorMedidoAcumulado { get; private set; } = default!;

    /// <summary>Fiscal atualmente designado (vigente); nulo enquanto não designado (I-10).</summary>
    public Guid? FiscalDesignadoId { get; private set; }

    /// <summary>Etapas do cronograma físico-financeiro (curva S).</summary>
    public IReadOnlyCollection<EtapaCronograma> Etapas => _etapas.AsReadOnly();

    /// <summary>Boletins de medição periódica.</summary>
    public IReadOnlyCollection<Medicao> Medicoes => _medicoes.AsReadOnly();

    /// <summary>Relatórios Diários de Obra (RDO).</summary>
    public IReadOnlyCollection<RegistroDiarioObra> RegistrosDiarios => _registrosDiarios.AsReadOnly();

    /// <summary>Histórico de designações de fiscal (art. 117).</summary>
    public IReadOnlyCollection<DesignacaoFiscal> DesignacoesFiscais => _designacoesFiscais.AsReadOnly();

    /// <summary>Ocorrências de fiscalização registradas.</summary>
    public IReadOnlyCollection<OcorrenciaFiscalizacao> Ocorrencias => _ocorrencias.AsReadOnly();

    /// <summary>Eventos de paralisação/reinício.</summary>
    public IReadOnlyCollection<EventoParalisacao> Paralisacoes => _paralisacoes.AsReadOnly();

    /// <summary>Indica se há cronograma físico-financeiro definido.</summary>
    public bool TemCronograma => _etapas.Count > 0;

    /// <summary>
    /// Tolerância de arredondamento (em pontos percentuais / Reais) ao comparar somatórios de percentual
    /// e valor com os totais (I-3/I-12). Constante nomeada — sem número mágico solto (CLAUDE.md §7).
    /// </summary>
    private const decimal ToleranciaArredondamento = 0.01m;

    /// <summary>
    /// Abre uma obra a partir de um contrato NLLC (situação inicial <see cref="SituacaoObra.Planejada"/>).
    /// O vínculo ao contrato é apenas por <c>ContratoId</c> (Guid); o valor contratado é snapshot do evento
    /// de contrato (reajustado por aditivo via <see cref="AplicarAditivoValor"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contratoId">Contrato NLLC de origem (obrigatório).</param>
    /// <param name="fornecedorId">Contratada/executora (obrigatória).</param>
    /// <param name="objeto">Descrição da obra/serviço de engenharia (obrigatória).</param>
    /// <param name="localizacao">Localização da obra (não nula).</param>
    /// <param name="regimeExecucao">Regime de execução (art. 46).</param>
    /// <param name="valorContratado">Valor contratado (positivo) — teto da medição (I-1).</param>
    /// <param name="dataAssinaturaContrato">Data de assinatura (base do relógio art. 94 §3 — 25 d.u.).</param>
    /// <returns>Nova obra em <see cref="SituacaoObra.Planejada"/>.</returns>
    /// <exception cref="ArgumentException">Se o objeto for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se contrato/fornecedor forem vazios ou o valor não for positivo.</exception>
    public static Obra AbrirObra(
        Guid tenantId,
        Guid contratoId,
        Guid fornecedorId,
        string objeto,
        LocalizacaoObra localizacao,
        RegimeExecucao regimeExecucao,
        ValorMonetario valorContratado,
        DateOnly dataAssinaturaContrato)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objeto);
        ArgumentNullException.ThrowIfNull(localizacao);
        ArgumentNullException.ThrowIfNull(valorContratado);
        if (contratoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(contratoId), "Contrato de origem é obrigatório.");
        }

        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorId), "Fornecedor (contratada) é obrigatório.");
        }

        if (valorContratado.Valor <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(valorContratado), "Valor contratado deve ser positivo.");
        }

        return new Obra(
            ObraId.New(),
            tenantId,
            contratoId,
            fornecedorId,
            objeto.Trim(),
            localizacao,
            regimeExecucao,
            valorContratado,
            dataAssinaturaContrato);
    }

    /// <summary>
    /// Define (ou redefine) o cronograma físico-financeiro da obra, validando a coerência físico-financeira
    /// (I-3): Σ percentual físico previsto = 100 e Σ valor previsto = valor contratado (com tolerância de
    /// arredondamento). Só permitido enquanto a obra estiver <see cref="SituacaoObra.Planejada"/>.
    /// </summary>
    /// <param name="etapas">Etapas do cronograma (curva S), pelo menos uma.</param>
    /// <exception cref="ArgumentException">Se não houver etapas.</exception>
    /// <exception cref="InvalidOperationException">Se a obra não estiver planejada ou a coerência físico-financeira falhar (I-3).</exception>
    public void DefinirCronograma(IReadOnlyCollection<EtapaCronograma> etapas)
    {
        ArgumentNullException.ThrowIfNull(etapas);
        if (etapas.Count == 0)
        {
            throw new ArgumentException("O cronograma exige ao menos uma etapa.", nameof(etapas));
        }

        if (Situacao != SituacaoObra.Planejada)
        {
            throw new InvalidOperationException(
                $"O cronograma só pode ser definido com a obra planejada. Situação atual: {Situacao}.");
        }

        // I-3: coerência físico-financeira do cronograma (curva S fecha com o edital).
        var somaPercentual = etapas.Sum(etapa => etapa.PercentualFisicoPrevisto);
        if (Math.Abs(somaPercentual - 100m) > ToleranciaArredondamento)
        {
            throw new InvalidOperationException(
                $"A soma dos percentuais físicos das etapas ({somaPercentual}%) deve ser 100%.");
        }

        var somaValor = etapas.Aggregate(0m, (total, etapa) => total + etapa.ValorPrevisto.Valor);
        if (Math.Abs(somaValor - ValorContratado.Valor) > ToleranciaArredondamento)
        {
            throw new InvalidOperationException(
                $"A soma dos valores previstos das etapas ({somaValor}) deve igualar o valor contratado ({ValorContratado}).");
        }

        _etapas.Clear();
        _etapas.AddRange(etapas.OrderBy(etapa => etapa.Ordem));
    }

    /// <summary>
    /// Emite a Ordem de Início de Serviço, transitando a obra de <see cref="SituacaoObra.Planejada"/> para
    /// <see cref="SituacaoObra.EmExecucao"/>. Exige cronograma definido (I-3) e fiscal designado (I-10).
    /// </summary>
    /// <param name="data">Data de emissão da ordem.</param>
    /// <exception cref="InvalidOperationException">Se não houver cronograma/fiscal ou a obra não estiver planejada.</exception>
    public void EmitirOrdemInicio(DateOnly data)
    {
        if (Situacao != SituacaoObra.Planejada)
        {
            throw new InvalidOperationException(
                $"A ordem de início exige obra planejada. Situação atual: {Situacao}.");
        }

        if (!TemCronograma)
        {
            throw new InvalidOperationException("A ordem de início exige cronograma físico-financeiro definido (I-3).");
        }

        if (FiscalDesignadoId is null)
        {
            throw new InvalidOperationException("A ordem de início exige fiscal designado (art. 117 / I-10).");
        }

        DataInicioOrdemServico = data;
        Situacao = SituacaoObra.EmExecucao;
    }

    /// <summary>
    /// Registra um Relatório Diário de Obra (RDO). Exige obra em execução (I-11) e dia único por obra (I-9).
    /// </summary>
    /// <param name="data">Data do RDO (única por obra — I-9).</param>
    /// <param name="condicaoTempo">Condição de tempo/clima.</param>
    /// <param name="efetivoMaoDeObra">Efetivo de mão de obra.</param>
    /// <param name="equipamentosMobilizados">Equipamentos mobilizados.</param>
    /// <param name="atividadesExecutadas">Atividades executadas.</param>
    /// <param name="responsavelTecnicoId">Responsável técnico.</param>
    /// <param name="ocorrencias">Ocorrências do dia (opcional).</param>
    /// <returns>Identificador do RDO registrado.</returns>
    /// <exception cref="InvalidOperationException">Se a obra não estiver em execução (I-11) ou já houver RDO no dia (I-9).</exception>
    public RegistroDiarioObraId RegistrarRdo(
        DateOnly data,
        string condicaoTempo,
        int efetivoMaoDeObra,
        string equipamentosMobilizados,
        string atividadesExecutadas,
        Guid responsavelTecnicoId,
        string? ocorrencias = null)
    {
        GarantirEmExecucao();
        // I-9: RDO único por dia na mesma obra.
        if (_registrosDiarios.Any(rdo => rdo.Data == data))
        {
            throw new InvalidOperationException($"Já existe RDO para a data {data:yyyy-MM-dd} nesta obra (I-9).");
        }

        var registro = RegistroDiarioObra.Criar(
            data,
            condicaoTempo,
            efetivoMaoDeObra,
            equipamentosMobilizados,
            atividadesExecutadas,
            responsavelTecnicoId,
            ocorrencias);
        _registrosDiarios.Add(registro);
        return registro.Id;
    }

    /// <summary>
    /// Aplica um aditivo de valor recebido de Administração (via <c>AditivoCelerado</c>), reajustando o
    /// teto contratado (I-2). O limite legal (25% / 50% reforma — art. 125) é validado em Administração;
    /// Patrimônio apenas aplica o novo teto. Não permitido após o encerramento da execução.
    /// </summary>
    /// <param name="novoTeto">Novo valor contratado vigente após o aditivo (positivo).</param>
    /// <exception cref="ArgumentNullException">Se o novo teto for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o novo teto não for positivo.</exception>
    /// <exception cref="InvalidOperationException">Se o novo teto for inferior ao já medido (I-1) ou a obra estiver encerrada.</exception>
    public void AplicarAditivoValor(ValorMonetario novoTeto)
    {
        ArgumentNullException.ThrowIfNull(novoTeto);
        if (novoTeto.Valor <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(novoTeto), "O novo teto contratado deve ser positivo.");
        }

        GarantirExecucaoNaoEncerrada();
        // I-2/I-1: o teto não pode cair abaixo do que já foi medido (aprovado).
        if (novoTeto.MenorQue(ValorMedidoAcumulado))
        {
            throw new InvalidOperationException(
                $"O novo teto ({novoTeto}) não pode ser inferior ao já medido ({ValorMedidoAcumulado}).");
        }

        ValorContratado = novoTeto;
    }

    private void GarantirEmExecucao()
    {
        // I-11: RDO/medição exigem obra em execução; bloqueadas nos demais estados.
        if (Situacao != SituacaoObra.EmExecucao)
        {
            throw new InvalidOperationException(
                $"A operação exige obra em execução. Situação atual: {Situacao} (I-11).");
        }
    }

    private void GarantirExecucaoNaoEncerrada()
    {
        if (Situacao is SituacaoObra.Concluida or SituacaoObra.Incorporada or SituacaoObra.Rescindida)
        {
            throw new InvalidOperationException(
                $"Obra encerrada não admite a operação. Situação atual: {Situacao}.");
        }
    }
}
