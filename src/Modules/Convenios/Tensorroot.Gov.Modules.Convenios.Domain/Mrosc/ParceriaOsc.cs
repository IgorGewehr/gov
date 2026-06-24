using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Events;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

/// <summary>Identificador forte do agregado <see cref="ParceriaOsc"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ParceriaOscId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ParceriaOscId"/>.</returns>
    public static ParceriaOscId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Parceria-saida com OSC (fluxo B — MROSC, Lei 13.019/2014 + Dec. 8.726/2016): instrumento em que o
/// municipio e a Administracao parceira e o dinheiro SAI (repasse a OSC). Raiz de agregado tenant-scoped,
/// com forma de selecao (chamamento OU dispensa/inexigibilidade), tipo de instrumento (colaboracao/fomento/
/// cooperacao), plano de trabalho, repasses (espelho de empenho-&gt;liquidacao-&gt;pagamento), gestor/comissao
/// e PC da OSC. Maquina de estados em <see cref="SituacaoParceriaOsc"/>; invariantes B-INV-1..12. Prazos
/// CALCULADOS via <see cref="ICalendarioDiasUteis"/> a partir dos parametros do tenant. Gatilho de
/// inadimplencia (LRF art. 48): BLOQUEIA novos repasses (B-INV-9).
/// </summary>
public sealed class ParceriaOsc : AggregateRoot<ParceriaOscId>, IMustHaveTenant
{
    private readonly List<RepasseOsc> _repasses = [];

    private ParceriaOsc()
    {
    }

    private ParceriaOsc(
        ParceriaOscId id,
        Guid tenantId,
        Osc osc,
        TipoInstrumentoMrosc tipoInstrumento,
        FormaSelecao formaSelecao)
        : base(id)
    {
        TenantId = tenantId;
        Osc = osc;
        TipoInstrumento = tipoInstrumento;
        FormaSelecao = formaSelecao;
        Situacao = SituacaoParceriaOsc.EmSelecao;
        RaiseDomainEvent(new ParceriaOscIniciada(id, tipoInstrumento));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>OSC parceira (com requisitos de habilitacao e certidoes).</summary>
    public Osc Osc { get; private set; } = default!;

    /// <summary>Tipo de instrumento (colaboracao/fomento/cooperacao).</summary>
    public TipoInstrumentoMrosc TipoInstrumento { get; private set; }

    /// <summary>Forma de selecao (chamamento/dispensa/inexigibilidade).</summary>
    public FormaSelecao FormaSelecao { get; private set; } = default!;

    /// <summary>Plano de trabalho (nulo ate ser registrado).</summary>
    public PlanoDeTrabalhoOsc? Plano { get; private set; }

    /// <summary>Vigencia (nula ate a celebracao).</summary>
    public Vigencia? Vigencia { get; private set; }

    /// <summary>Gestor da parceria designado (art. 35 §4) — nulo ate a celebracao.</summary>
    public Guid? GestorParceriaId { get; private set; }

    /// <summary>Comissao de monitoramento e avaliacao referenciada (art. 35 §5) — nula ate a celebracao.</summary>
    public Guid? ComissaoMonitoramentoId { get; private set; }

    /// <summary>Situacao atual (maquina de estados B).</summary>
    public SituacaoParceriaOsc Situacao { get; private set; }

    /// <summary>Motivo da inadimplencia (nulo se nao inadimplente).</summary>
    public string? MotivoInadimplencia { get; private set; }

    /// <summary>Repasses (parcelas de saida a OSC).</summary>
    public IReadOnlyList<RepasseOsc> Repasses => _repasses;

    /// <summary>Prestacao de contas da OSC (nula ate a abertura).</summary>
    public PrestacaoContasOsc? Prestacao { get; private set; }

    /// <summary>Verdadeiro se o instrumento admite repasse (todos menos Acordo de Cooperacao — B-INV-2).</summary>
    public bool PermiteRepasse => TipoInstrumento != TipoInstrumentoMrosc.AcordoCooperacao;

    /// <summary>
    /// Inicia a selecao da parceria (estado inicial <see cref="SituacaoParceriaOsc.EmSelecao"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="osc">OSC parceira.</param>
    /// <param name="tipoInstrumento">Tipo de instrumento.</param>
    /// <param name="formaSelecao">Forma de selecao (chamamento/dispensa/inexigibilidade).</param>
    /// <returns>Nova <see cref="ParceriaOsc"/> em selecao.</returns>
    public static ParceriaOsc IniciarSelecao(
        Guid tenantId,
        Osc osc,
        TipoInstrumentoMrosc tipoInstrumento,
        FormaSelecao formaSelecao)
    {
        ArgumentNullException.ThrowIfNull(osc);
        ArgumentNullException.ThrowIfNull(formaSelecao);
        return new ParceriaOsc(ParceriaOscId.New(), tenantId, osc, tipoInstrumento, formaSelecao);
    }

    /// <summary>Homologa o edital do chamamento (so quando a selecao e por chamamento publico).</summary>
    /// <exception cref="InvalidParceriaStateException">Se nao estiver em selecao.</exception>
    public void HomologarChamamento()
    {
        if (Situacao != SituacaoParceriaOsc.EmSelecao)
        {
            throw new InvalidParceriaStateException($"Homologacao exige parceria em selecao. Situacao atual: {Situacao}.");
        }

        FormaSelecao = FormaSelecao.Homologar();
    }

    /// <summary>
    /// Registra/atualiza o plano de trabalho. Para Acordo de Cooperacao o plano nao admite repasse (B-INV-2).
    /// </summary>
    /// <param name="objeto">Objeto da parceria.</param>
    /// <param name="valorGlobal">Valor global.</param>
    /// <param name="metas">Metas com indicadores.</param>
    /// <param name="parcelas">Cronograma de desembolso.</param>
    /// <exception cref="InvalidParceriaStateException">Se a parceria ja foi celebrada/terminal.</exception>
    public void RegistrarPlanoTrabalho(
        string objeto,
        Dinheiro valorGlobal,
        IReadOnlyList<MetaOsc> metas,
        IReadOnlyList<ParcelaRepasse> parcelas)
    {
        if (Situacao != SituacaoParceriaOsc.EmSelecao)
        {
            throw new InvalidParceriaStateException($"Plano de trabalho exige parceria em selecao. Situacao atual: {Situacao}.");
        }

        Plano = PlanoDeTrabalhoOsc.Criar(objeto, valorGlobal, metas, parcelas, PermiteRepasse);
    }

    /// <summary>Aprova o plano de trabalho da parceria (pre-requisito do repasse — B-INV-4).</summary>
    /// <exception cref="InvalidParceriaStateException">Se nao houver plano ou nao estiver em selecao.</exception>
    public void AprovarPlanoTrabalho()
    {
        if (Situacao != SituacaoParceriaOsc.EmSelecao || Plano is null)
        {
            throw new InvalidParceriaStateException("Aprovacao do plano exige parceria em selecao com plano registrado.");
        }

        Plano.Aprovar();
        RaiseDomainEvent(new PlanoTrabalhoOscAprovado(Id));
    }

    /// <summary>
    /// Celebra a parceria (EmSelecao -&gt; Celebrada): exige selecao fundamentada (B-INV-1), habilitacao da OSC
    /// na data (B-INV-3), plano aprovado, gestor e comissao designados (B-INV-10). Materializa os repasses
    /// previstos (exceto Acordo de Cooperacao — B-INV-2). Emite <see cref="ParceriaOscCelebrada"/>.
    /// </summary>
    /// <param name="vigencia">Vigencia pactuada.</param>
    /// <param name="gestorParceriaId">Gestor da parceria designado (art. 35 §4).</param>
    /// <param name="comissaoMonitoramentoId">Comissao de monitoramento referenciada (art. 35 §5).</param>
    /// <param name="dataCelebracao">Data de referencia da habilitacao (relogio externo).</param>
    /// <exception cref="InvalidParceriaStateException">Se selecao nao fundamentada (B-INV-1), OSC nao habilitada (B-INV-3), plano nao aprovado, ou gestor/comissao ausentes (B-INV-10).</exception>
    public void Celebrar(
        Vigencia vigencia,
        Guid gestorParceriaId,
        Guid comissaoMonitoramentoId,
        DateOnly dataCelebracao)
    {
        ArgumentNullException.ThrowIfNull(vigencia);

        if (Situacao != SituacaoParceriaOsc.EmSelecao)
        {
            throw new InvalidParceriaStateException($"Celebracao exige parceria em selecao. Situacao atual: {Situacao}.");
        }

        // B-INV-1: selecao fundamentada (chamamento homologado OU direta com fundamento + justificativa).
        if (!FormaSelecao.AptaParaCelebrar)
        {
            throw new InvalidParceriaStateException(
                "Celebracao exige selecao fundamentada: chamamento homologado, ou dispensa/inexigibilidade com fundamento + justificativa (B-INV-1).");
        }

        // B-INV-3: habilitacao da OSC (certidoes validas + requisitos art. 33-34) na data.
        if (!Osc.Habilitada(dataCelebracao))
        {
            throw new InvalidParceriaStateException(
                "Celebracao exige OSC habilitada: certidoes validas na data e requisitos dos arts. 33-34 atendidos (B-INV-3).");
        }

        // B-INV-4: plano aprovado.
        if (Plano is null || !Plano.Aprovado)
        {
            throw new InvalidParceriaStateException("Celebracao exige plano de trabalho aprovado (B-INV-4).");
        }

        // B-INV-10: gestor e comissao designados.
        if (gestorParceriaId == Guid.Empty || comissaoMonitoramentoId == Guid.Empty)
        {
            throw new InvalidParceriaStateException("Celebracao exige gestor (art. 35 §4) e comissao de monitoramento designados (B-INV-10).");
        }

        Vigencia = vigencia;
        GestorParceriaId = gestorParceriaId;
        ComissaoMonitoramentoId = comissaoMonitoramentoId;

        // B-INV-2: Acordo de Cooperacao nao gera repasses.
        if (PermiteRepasse)
        {
            foreach (var parcela in Plano.Parcelas)
            {
                _repasses.Add(RepasseOsc.Prever(parcela.NumeroOrdem, parcela.Valor, parcela.DataPrevista, parcela.Condicionantes));
            }
        }

        Situacao = SituacaoParceriaOsc.Celebrada;
        RaiseDomainEvent(new ParceriaOscCelebrada(Id, Plano.ValorGlobal.Valor));
    }

    /// <summary>
    /// Vincula o espelho de execucao orcamentaria (empenho/liquidacao/pagamento) de Financas a uma parcela de
    /// repasse, por numero de ordem (B-INV-5). Idempotente: revincular o mesmo id e no-op.
    /// </summary>
    /// <param name="numeroOrdem">Numero de ordem da parcela.</param>
    /// <param name="empenhoId">Empenho (opcional).</param>
    /// <param name="liquidacaoId">Liquidacao (opcional).</param>
    /// <param name="pagamentoId">Pagamento (opcional).</param>
    /// <exception cref="InvalidParceriaStateException">Se a parcela nao existir.</exception>
    public void VincularExecucaoOrcamentaria(int numeroOrdem, Guid? empenhoId, Guid? liquidacaoId, Guid? pagamentoId)
    {
        var repasse = _repasses.Find(r => r.NumeroOrdem == numeroOrdem)
            ?? throw new InvalidParceriaStateException($"Repasse {numeroOrdem} nao previsto no cronograma.");

        if (empenhoId is { } emp && emp != Guid.Empty)
        {
            repasse.VincularEmpenho(emp);
        }

        if (liquidacaoId is { } liq && liq != Guid.Empty)
        {
            repasse.VincularLiquidacao(liq);
        }

        if (pagamentoId is { } pag && pag != Guid.Empty)
        {
            repasse.VincularPagamento(pag);
        }
    }

    /// <summary>
    /// Libera um repasse a OSC (B-INV-2/4/5/9): proibido no Acordo de Cooperacao (B-INV-2); exige plano
    /// aprovado + vigencia vigente (B-INV-4); espelho de execucao orcamentaria completo (B-INV-5); parceria
    /// adimplente (B-INV-9); e a condicionante da parcela anterior (PC parcial) quando aplicavel. Move para
    /// EmExecucao na primeira liberacao. Emite <see cref="RepasseOscLiberado"/>.
    /// </summary>
    /// <param name="numeroOrdem">Numero de ordem da parcela.</param>
    /// <param name="dataLiberada">Data de liberacao (relogio externo).</param>
    /// <exception cref="InvalidParceriaStateException">Se inadimplente (B-INV-9), Acordo de Cooperacao (B-INV-2), sem plano/vigencia (B-INV-4) ou estado invalido.</exception>
    public void LiberarRepasse(int numeroOrdem, DateOnly dataLiberada)
    {
        // B-INV-9: inadimplencia bloqueia novos repasses (gatilho LRF).
        if (Situacao == SituacaoParceriaOsc.Inadimplente)
        {
            throw new InvalidParceriaStateException("Parceria inadimplente: novos repasses bloqueados (LRF art. 48 — B-INV-9).");
        }

        // B-INV-2: Acordo de Cooperacao nao repassa.
        if (!PermiteRepasse)
        {
            throw new InvalidParceriaStateException("Acordo de Cooperacao nao admite repasse de recursos (B-INV-2).");
        }

        if (Situacao is not (SituacaoParceriaOsc.Celebrada or SituacaoParceriaOsc.EmExecucao))
        {
            throw new InvalidParceriaStateException($"Liberacao de repasse exige parceria celebrada/em execucao. Situacao atual: {Situacao}.");
        }

        // B-INV-4: plano aprovado + vigencia vigente.
        if (Plano is null || !Plano.Aprovado || Vigencia is null)
        {
            throw new InvalidParceriaStateException("Repasse exige plano aprovado e vigencia (B-INV-4).");
        }

        if (Vigencia.Encerrada(dataLiberada))
        {
            throw new InvalidParceriaStateException("Repasse exige vigencia vigente na data da liberacao (B-INV-4).");
        }

        var repasse = _repasses.Find(r => r.NumeroOrdem == numeroOrdem)
            ?? throw new InvalidParceriaStateException($"Repasse {numeroOrdem} nao previsto no cronograma.");

        repasse.Liberar(dataLiberada);

        if (Situacao == SituacaoParceriaOsc.Celebrada)
        {
            Situacao = SituacaoParceriaOsc.EmExecucao;
        }

        RaiseDomainEvent(new RepasseOscLiberado(Id, numeroOrdem, repasse.Valor.Valor));
    }

    /// <summary>
    /// Abre a PC da OSC (B-INV-6): calcula o prazo de ENTREGA (90 d a partir do fim da vigencia) via
    /// parametro do tenant + calendario. So uma PC por parceria.
    /// </summary>
    /// <param name="prazoEntrega">Parametro do prazo de entrega (do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <exception cref="InvalidParceriaStateException">Se nao houver vigencia, ja houver PC, ou estado terminal/selecao.</exception>
    public void AbrirPrestacaoOsc(ParametroPrazo prazoEntrega, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        GarantirNaoTerminal();
        if (Vigencia is null)
        {
            throw new InvalidParceriaStateException("PC da OSC exige parceria celebrada (com vigencia).");
        }

        if (Prestacao is not null)
        {
            throw new InvalidParceriaStateException("Ja existe PC para esta parceria.");
        }

        var prazo = prazoEntrega.Resolver(Vigencia.Fim, calendario);
        Prestacao = PrestacaoContasOsc.Abrir(prazo);
        Situacao = SituacaoParceriaOsc.EmPrestacaoContas;
    }

    /// <summary>Prorroga UMA vez o prazo de entrega da PC da OSC (+30 d — B-INV-6).</summary>
    /// <param name="prazoProrrogacao">Parametro da prorrogacao (do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <exception cref="InvalidParceriaStateException">Se nao houver PC.</exception>
    public void ProrrogarEntregaPrestacao(ParametroPrazo prazoProrrogacao, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        var pc = GarantirPrestacao();
        var prazoProrrogado = pc.PrazoEntrega.Prorrogar(prazoProrrogacao.Quantidade, prazoProrrogacao.NormaFonte, calendario);
        pc.ProrrogarEntrega(prazoProrrogado);
    }

    /// <summary>
    /// Recebe a PC da OSC (B-INV-7): calcula o prazo de analise (150 d) e move para EmAnalise. Emite
    /// <see cref="PrestacaoContasOscRecebida"/>.
    /// </summary>
    /// <param name="dataRecebimento">Data de recebimento.</param>
    /// <param name="prazoAnalise">Parametro do prazo de analise (do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    public void ReceberPrestacaoOsc(DateOnly dataRecebimento, ParametroPrazo prazoAnalise, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        var pc = GarantirPrestacao();
        var prazo = prazoAnalise.Resolver(dataRecebimento, calendario);
        pc.Receber(dataRecebimento, prazo);
        Situacao = SituacaoParceriaOsc.EmAnalise;
        RaiseDomainEvent(new PrestacaoContasOscRecebida(Id, dataRecebimento, prazo.Vencimento));
    }

    /// <summary>Inicia a analise da PC da OSC.</summary>
    public void IniciarAnalisePrestacao() => GarantirPrestacao().IniciarAnalise();

    /// <summary>Abre o saneamento da PC da OSC (45 d, uma vez — B-INV-8).</summary>
    /// <param name="dataNotificacao">Data da notificacao de pendencia.</param>
    /// <param name="prazoSaneamento">Parametro do prazo de saneamento (do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    public void AbrirSaneamentoPrestacao(DateOnly dataNotificacao, ParametroPrazo prazoSaneamento, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        var pc = GarantirPrestacao();
        var prazo = prazoSaneamento.Resolver(dataNotificacao, calendario);
        pc.AbrirSaneamento(prazo);
    }

    /// <summary>Retoma a analise da PC apos o saneamento.</summary>
    public void RetomarAnalisePrestacao() => GarantirPrestacao().RetomarAnalise();

    /// <summary>
    /// Conclui a analise da PC da OSC (B-INV-11): aprovada/ressalva/rejeitada define o estado terminal da
    /// parceria. Rejeicao enseja inadimplencia/devolucao. Emite <see cref="PrestacaoContasOscAnalisada"/>.
    /// </summary>
    /// <param name="resultado">Resultado da analise.</param>
    public void ConcluirAnalisePrestacao(ResultadoAnalise resultado)
    {
        var pc = GarantirPrestacao();
        pc.Concluir(resultado);
        RaiseDomainEvent(new PrestacaoContasOscAnalisada(Id, resultado));

        Situacao = resultado switch
        {
            ResultadoAnalise.Aprovada => SituacaoParceriaOsc.Aprovada,
            ResultadoAnalise.AprovadaComRessalva => SituacaoParceriaOsc.AprovadaComRessalva,
            ResultadoAnalise.Rejeitada => SituacaoParceriaOsc.Rejeitada,
            _ => Situacao,
        };
    }

    /// <summary>
    /// Calcula a devolucao da PC (B-INV-12): saldo + glosas atualizados pelo indice do tenant conforme o
    /// atraso.
    /// </summary>
    /// <param name="saldoEGlosas">Saldo nao aplicado + glosas.</param>
    /// <param name="indice">Indice de devolucao (Selic) do tenant.</param>
    /// <param name="diasAtraso">Dias decorridos desde o vencimento da devolucao.</param>
    public void CalcularDevolucao(Dinheiro saldoEGlosas, IndiceDevolucao indice, int diasAtraso)
    {
        ArgumentNullException.ThrowIfNull(saldoEGlosas);
        ArgumentNullException.ThrowIfNull(indice);
        GarantirPrestacao().CalcularDevolucao(saldoEGlosas, indice, diasAtraso);
    }

    /// <summary>
    /// Declara a inadimplencia (B-INV-9 — gatilho LRF art. 48): estado terminal-bloqueio; bloqueia TODOS os
    /// repasses ainda previstos; nenhum novo repasse e liberado. Emite <see cref="ParceriaOscInadimplente"/>.
    /// </summary>
    /// <param name="motivo">Motivo (PC nao entregue/rejeitada, irregularidade, impedimento art. 39).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidParceriaStateException">Se ja estiver em estado terminal.</exception>
    public void DeclararInadimplencia(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirNaoTerminal();

        // B-INV-9: bloqueia todas as parcelas futuras (gatilho LRF — bloqueia novos repasses).
        foreach (var repasse in _repasses)
        {
            repasse.Bloquear();
        }

        Situacao = SituacaoParceriaOsc.Inadimplente;
        MotivoInadimplencia = motivo.Trim();
        RaiseDomainEvent(new ParceriaOscInadimplente(Id, MotivoInadimplencia));
    }

    private PrestacaoContasOsc GarantirPrestacao()
        => Prestacao ?? throw new InvalidParceriaStateException("A parceria ainda nao tem PC da OSC aberta.");

    private void GarantirNaoTerminal()
    {
        // B-INV-11: estados terminais nao admitem novas transicoes.
        if (Situacao is SituacaoParceriaOsc.Aprovada
            or SituacaoParceriaOsc.AprovadaComRessalva
            or SituacaoParceriaOsc.Rejeitada
            or SituacaoParceriaOsc.Inadimplente)
        {
            throw new InvalidParceriaStateException($"Parceria em estado terminal nao admite a operacao. Situacao atual: {Situacao}.");
        }
    }
}
