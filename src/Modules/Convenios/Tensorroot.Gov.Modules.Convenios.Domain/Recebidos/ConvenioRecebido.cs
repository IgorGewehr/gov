using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

/// <summary>Identificador forte do agregado <see cref="ConvenioRecebido"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ConvenioRecebidoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ConvenioRecebidoId"/>.</returns>
    public static ConvenioRecebidoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Convenio federal RECEBIDO (fluxo A — Dec. 11.531/2023 + Transferegov.br): instrumento em que o municipio
/// e CONVENENTE/recebedor e o dinheiro ENTRA. Raiz de agregado tenant-scoped, com plano de trabalho, repasses
/// recebidos, contrapartida (espelho do empenho), rendimentos de conta vinculada e prestacoes de contas
/// (N parciais continuas + 1 final). Maquina de estados em <see cref="SituacaoConvenioRecebido"/>; invariantes
/// A-INV-1..12. Prazos CALCULADOS via <see cref="ICalendarioDiasUteis"/> a partir dos parametros do tenant
/// (nunca digitados — CLAUDE.md S7/S16). Vinculo orcamentario: receita de convenio + empenho da contrapartida.
/// </summary>
public sealed class ConvenioRecebido : AggregateRoot<ConvenioRecebidoId>, IMustHaveTenant
{
    private readonly List<Repasse> _repasses = [];
    private readonly List<RendimentoAplicacaoFinanceira> _rendimentos = [];
    private readonly List<PrestacaoContasConvenio> _prestacoes = [];

    private ConvenioRecebido()
    {
    }

    private ConvenioRecebido(
        ConvenioRecebidoId id,
        Guid tenantId,
        OrgaoConcedente concedente,
        PlanoDeTrabalho plano)
        : base(id)
    {
        TenantId = tenantId;
        Concedente = concedente;
        Plano = plano;
        Situacao = SituacaoConvenioRecebido.EmProposta;
        RaiseDomainEvent(new ConvenioRecebidoRegistrado(id));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Orgao concedente (Uniao/Estado).</summary>
    public OrgaoConcedente Concedente { get; private set; } = default!;

    /// <summary>Plano de trabalho (entidade filha unica).</summary>
    public PlanoDeTrabalho Plano { get; private set; } = default!;

    /// <summary>Contrapartida pactuada (nula ate a celebracao — definida com o plano aprovado).</summary>
    public Contrapartida? Contrapartida { get; private set; }

    /// <summary>Vigencia do convenio (nula ate a celebracao).</summary>
    public Vigencia? Vigencia { get; private set; }

    /// <summary>Situacao atual (maquina de estados A).</summary>
    public SituacaoConvenioRecebido Situacao { get; private set; }

    /// <summary>Motivo da inadimplencia (nulo se nao inadimplente).</summary>
    public string? MotivoInadimplencia { get; private set; }

    /// <summary>Repasses (parcelas recebidas).</summary>
    public IReadOnlyList<Repasse> Repasses => _repasses;

    /// <summary>Rendimentos de aplicacao financeira da conta vinculada.</summary>
    public IReadOnlyList<RendimentoAplicacaoFinanceira> Rendimentos => _rendimentos;

    /// <summary>Prestacoes de contas (N parciais + 1 final).</summary>
    public IReadOnlyList<PrestacaoContasConvenio> Prestacoes => _prestacoes;

    /// <summary>
    /// Registra a proposta + plano de trabalho (estado inicial <see cref="SituacaoConvenioRecebido.EmProposta"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="concedente">Orgao concedente.</param>
    /// <param name="plano">Plano de trabalho (somas conferidas — A-INV-3).</param>
    /// <returns>Novo <see cref="ConvenioRecebido"/> em proposta.</returns>
    public static ConvenioRecebido RegistrarProposta(Guid tenantId, OrgaoConcedente concedente, PlanoDeTrabalho plano)
    {
        ArgumentNullException.ThrowIfNull(concedente);
        ArgumentNullException.ThrowIfNull(plano);
        return new ConvenioRecebido(ConvenioRecebidoId.New(), tenantId, concedente, plano);
    }

    /// <summary>Aprova o plano de trabalho pelo concedente (pre-requisito da celebracao — A-INV-1).</summary>
    /// <exception cref="InvalidConvenioStateException">Se nao estiver em proposta.</exception>
    public void AprovarPlanoTrabalho()
    {
        if (Situacao != SituacaoConvenioRecebido.EmProposta)
        {
            throw new InvalidConvenioStateException($"Aprovacao do plano exige convenio em proposta. Situacao atual: {Situacao}.");
        }

        Plano.Aprovar();
        RaiseDomainEvent(new PlanoTrabalhoConvenioAprovado(Id));
    }

    /// <summary>
    /// Celebra o convenio (EmProposta -&gt; Celebrado): exige plano APROVADO + vigencia (A-INV-1) e
    /// contrapartida que atinja o minimo legal sobre o repasse (A-INV-2). Gera os repasses previstos do
    /// cronograma e emite <see cref="ConvenioRecebidoCelebrado"/> (gatilho da receita + reserva da contrapartida).
    /// </summary>
    /// <param name="vigencia">Vigencia pactuada.</param>
    /// <param name="contrapartida">Contrapartida pactuada (percentual minimo do tenant embutido).</param>
    /// <param name="numeroConvenioTransferegov">Numero do convenio no Transferegov (atribuido na celebracao).</param>
    /// <exception cref="InvalidConvenioStateException">Se nao estiver em proposta ou o plano nao estiver aprovado (A-INV-1).</exception>
    /// <exception cref="ArgumentException">Se a contrapartida nao atingir o minimo legal (A-INV-2).</exception>
    public void Celebrar(Vigencia vigencia, Contrapartida contrapartida, string numeroConvenioTransferegov)
    {
        ArgumentNullException.ThrowIfNull(vigencia);
        ArgumentNullException.ThrowIfNull(contrapartida);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroConvenioTransferegov);

        // A-INV-1: plano aprovado + vigencia definida.
        if (Situacao != SituacaoConvenioRecebido.EmProposta)
        {
            throw new InvalidConvenioStateException($"Celebracao exige convenio em proposta. Situacao atual: {Situacao}.");
        }

        if (!Plano.Aprovado)
        {
            throw new InvalidConvenioStateException("Celebracao exige plano de trabalho aprovado pelo concedente (A-INV-1).");
        }

        // A-INV-2: contrapartida >= percentual minimo x repasse total (percentual e parametro do tenant).
        if (!contrapartida.AtendeMinimo(Plano.ValorRepasse))
        {
            throw new ArgumentException(
                $"Contrapartida abaixo do minimo de {contrapartida.PercentualMinimo}% sobre o repasse ({contrapartida.NormaFontePercentual}) — A-INV-2.",
                nameof(contrapartida));
        }

        Vigencia = vigencia;
        Contrapartida = contrapartida;
        Concedente = Concedente.ComNumeroTransferegov(numeroConvenioTransferegov);

        // Materializa os repasses previstos do cronograma do plano.
        foreach (var parcela in Plano.Parcelas)
        {
            _repasses.Add(Repasse.Prever(parcela.NumeroOrdem, parcela.Valor, parcela.DataPrevista));
        }

        Situacao = SituacaoConvenioRecebido.Celebrado;
        RaiseDomainEvent(new ConvenioRecebidoCelebrado(Id, Plano.ValorGlobal.Valor, contrapartida.ValorPactuado.Valor));
    }

    /// <summary>
    /// Reconhece o empenho da contrapartida (reage ao evento de Financas): acumula no espelho
    /// <c>Contrapartida.ValorEmpenhado</c>. Quando totalmente empenhada e o convenio esta Celebrado, libera a
    /// execucao (Celebrado -&gt; EmExecucao — A-INV-5). Emite <see cref="ContrapartidaConvenioEmpenhada"/>.
    /// </summary>
    /// <param name="valorEmpenhado">Valor empenhado reconhecido.</param>
    /// <exception cref="InvalidConvenioStateException">Se o convenio nao admite execucao (terminal/inadimplente) ou nao foi celebrado.</exception>
    public void RegistrarContrapartidaEmpenhada(Dinheiro valorEmpenhado)
    {
        ArgumentNullException.ThrowIfNull(valorEmpenhado);
        GarantirNaoTerminal();
        if (Contrapartida is null || Situacao is SituacaoConvenioRecebido.EmProposta)
        {
            throw new InvalidConvenioStateException("Empenho de contrapartida exige convenio celebrado (A-INV-5).");
        }

        Contrapartida = Contrapartida.ComEmpenho(valorEmpenhado);
        RaiseDomainEvent(new ContrapartidaConvenioEmpenhada(Id, Contrapartida.ValorEmpenhado.Valor));

        // A-INV-5: nao ha execucao sem a contrapartida totalmente empenhada.
        if (Situacao == SituacaoConvenioRecebido.Celebrado && Contrapartida.TotalmenteEmpenhada)
        {
            Situacao = SituacaoConvenioRecebido.EmExecucao;
        }
    }

    /// <summary>
    /// Registra a liberacao/recebimento de uma parcela (A-INV-4/5/10): exige execucao iniciada, parcela
    /// anterior com PC parcial submetida (regra de liberacao por etapa) e convenio adimplente. Emite
    /// <see cref="ParcelaConvenioLiberada"/>.
    /// </summary>
    /// <param name="numeroOrdem">Numero de ordem da parcela.</param>
    /// <param name="dataLiberada">Data de liberacao (relogio externo).</param>
    /// <exception cref="InvalidConvenioStateException">Se inadimplente (A-INV-10), sem execucao, ou a etapa anterior nao foi comprovada (A-INV-4).</exception>
    public void RegistrarLiberacaoParcela(int numeroOrdem, DateOnly dataLiberada)
    {
        // A-INV-10: inadimplencia bloqueia qualquer liberacao.
        if (Situacao == SituacaoConvenioRecebido.Inadimplente)
        {
            throw new InvalidConvenioStateException("Convenio inadimplente nao libera novas parcelas (A-INV-10).");
        }

        if (Situacao is not (SituacaoConvenioRecebido.EmExecucao or SituacaoConvenioRecebido.EmPrestacaoContas))
        {
            throw new InvalidConvenioStateException($"Liberacao de parcela exige convenio em execucao. Situacao atual: {Situacao}.");
        }

        var repasse = _repasses.Find(r => r.NumeroOrdem == numeroOrdem)
            ?? throw new InvalidConvenioStateException($"Parcela {numeroOrdem} nao prevista no cronograma.");

        // A-INV-4: a parcela n so libera se a n-1 tiver PC parcial SUBMETIDA.
        if (numeroOrdem > 1 && !ExistePrestacaoParcialSubmetidaParaEtapa(numeroOrdem - 1))
        {
            throw new InvalidConvenioStateException(
                $"Liberacao da parcela {numeroOrdem} exige PC parcial submetida da etapa anterior (A-INV-4).");
        }

        repasse.Liberar(dataLiberada);
        RaiseDomainEvent(new ParcelaConvenioLiberada(Id, numeroOrdem, repasse.Valor.Valor));
    }

    /// <summary>Registra um rendimento de aplicacao financeira da conta vinculada (integra o objeto — A-INV-6).</summary>
    /// <param name="valor">Valor do rendimento.</param>
    /// <param name="data">Data do rendimento.</param>
    /// <exception cref="InvalidConvenioStateException">Se o convenio estiver em estado terminal.</exception>
    public void RegistrarRendimento(Dinheiro valor, DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirNaoTerminal();
        _rendimentos.Add(RendimentoAplicacaoFinanceira.Registrar(valor, data));
    }

    /// <summary>
    /// Abre uma PC parcial (continua) para a etapa/competencia informada (EmExecucao -&gt; EmPrestacaoContas).
    /// </summary>
    /// <param name="competenciaRef">Competencia/etapa de referencia.</param>
    /// <returns>A PC parcial criada.</returns>
    /// <exception cref="InvalidConvenioStateException">Se o convenio nao estiver em execucao/PC.</exception>
    public PrestacaoContasConvenio AbrirPrestacaoParcial(string competenciaRef)
    {
        if (Situacao is not (SituacaoConvenioRecebido.EmExecucao or SituacaoConvenioRecebido.EmPrestacaoContas))
        {
            throw new InvalidConvenioStateException($"PC parcial exige convenio em execucao. Situacao atual: {Situacao}.");
        }

        var pc = PrestacaoContasConvenio.CriarParcial(competenciaRef);
        _prestacoes.Add(pc);
        Situacao = SituacaoConvenioRecebido.EmPrestacaoContas;
        return pc;
    }

    /// <summary>
    /// Encerra a vigencia e abre a PC FINAL (A-INV-7): exige <c>Vigencia.Fim &lt;= hoje</c> ou encerramento
    /// explicito. So uma PC final por convenio.
    /// </summary>
    /// <param name="hoje">Data de referencia (relogio externo).</param>
    /// <param name="competenciaRef">Referencia da PC final (ex.: "Final").</param>
    /// <returns>A PC final criada.</returns>
    /// <exception cref="InvalidConvenioStateException">Se a vigencia nao encerrou (A-INV-7), ja existe PC final, ou estado terminal.</exception>
    public PrestacaoContasConvenio EncerrarVigenciaAbrirPrestacaoFinal(DateOnly hoje, string competenciaRef)
    {
        GarantirNaoTerminal();
        if (Vigencia is null || !Vigencia.Encerrada(hoje))
        {
            throw new InvalidConvenioStateException("PC final exige vigencia encerrada (A-INV-7).");
        }

        if (_prestacoes.Exists(pc => pc.Tipo == TipoPrestacaoConvenio.Final))
        {
            throw new InvalidConvenioStateException("Ja existe PC final para este convenio.");
        }

        var pc = PrestacaoContasConvenio.CriarFinal(competenciaRef);
        _prestacoes.Add(pc);
        Situacao = SituacaoConvenioRecebido.EmPrestacaoContas;
        return pc;
    }

    /// <summary>
    /// Submete uma PC (A-INV-8): calcula o prazo de analise via calendario a partir do parametro do tenant
    /// (parcial/final) e move o convenio para <see cref="SituacaoConvenioRecebido.EmAnalise"/>. Emite
    /// <see cref="PrestacaoContasConvenioSubmetida"/>.
    /// </summary>
    /// <param name="prestacaoId">Identificador da PC.</param>
    /// <param name="dataSubmissao">Data de submissao.</param>
    /// <param name="prazoParcial">Parametro de prazo da analise parcial (do tenant).</param>
    /// <param name="prazoFinal">Parametro de prazo da analise final (do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant (resolve o prazo).</param>
    /// <exception cref="InvalidConvenioStateException">Se a PC nao for encontrada.</exception>
    public void SubmeterPrestacao(
        Guid prestacaoId,
        DateOnly dataSubmissao,
        Parametros.ParametroPrazo prazoParcial,
        Parametros.ParametroPrazo prazoFinal,
        ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        var pc = ObterPrestacao(prestacaoId);

        var parametro = pc.Tipo == TipoPrestacaoConvenio.Parcial ? prazoParcial : prazoFinal;
        var prazo = parametro.Resolver(dataSubmissao, calendario);
        pc.Submeter(dataSubmissao, prazo);

        Situacao = SituacaoConvenioRecebido.EmAnalise;
        RaiseDomainEvent(new PrestacaoContasConvenioSubmetida(Id, pc.Tipo, dataSubmissao, prazo.Vencimento));
    }

    /// <summary>Inicia a analise de uma PC submetida.</summary>
    /// <param name="prestacaoId">Identificador da PC.</param>
    public void IniciarAnalisePrestacao(Guid prestacaoId) => ObterPrestacao(prestacaoId).IniciarAnalise();

    /// <summary>
    /// Abre o saneamento de uma PC em analise (A-INV-9): concede o prazo (uma vez) a partir da notificacao,
    /// calculado via calendario do parametro do tenant.
    /// </summary>
    /// <param name="prestacaoId">Identificador da PC.</param>
    /// <param name="dataNotificacao">Data da notificacao de pendencia (relogio externo).</param>
    /// <param name="prazoSaneamento">Parametro do prazo de saneamento (do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    public void AbrirSaneamentoPrestacao(
        Guid prestacaoId,
        DateOnly dataNotificacao,
        Parametros.ParametroPrazo prazoSaneamento,
        ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        var pc = ObterPrestacao(prestacaoId);
        var prazo = prazoSaneamento.Resolver(dataNotificacao, calendario);
        pc.AbrirSaneamento(prazo);
    }

    /// <summary>Retoma a analise de uma PC apos o saneamento.</summary>
    /// <param name="prestacaoId">Identificador da PC.</param>
    public void RetomarAnalisePrestacao(Guid prestacaoId) => ObterPrestacao(prestacaoId).RetomarAnalise();

    /// <summary>
    /// Conclui a analise de uma PC (A-INV-11): aprovada/ressalva/rejeitada. O resultado da PC FINAL define o
    /// estado terminal do convenio; rejeicao final enseja inadimplencia/devolucao (TCE). Emite
    /// <see cref="PrestacaoContasConvenioAnalisada"/>.
    /// </summary>
    /// <param name="prestacaoId">Identificador da PC.</param>
    /// <param name="resultado">Resultado da analise.</param>
    public void ConcluirAnalisePrestacao(Guid prestacaoId, ResultadoAnalise resultado)
    {
        var pc = ObterPrestacao(prestacaoId);
        pc.Concluir(resultado);
        RaiseDomainEvent(new PrestacaoContasConvenioAnalisada(Id, pc.Tipo, resultado));

        if (pc.Tipo == TipoPrestacaoConvenio.Final)
        {
            Situacao = resultado switch
            {
                ResultadoAnalise.Aprovada => SituacaoConvenioRecebido.Aprovado,
                ResultadoAnalise.AprovadaComRessalva => SituacaoConvenioRecebido.AprovadoComRessalva,
                ResultadoAnalise.Rejeitada => SituacaoConvenioRecebido.Rejeitado,
                _ => Situacao,
            };
        }
        else if (Situacao == SituacaoConvenioRecebido.EmAnalise)
        {
            // PC parcial concluida: volta a execucao para seguir liberando/prestando.
            Situacao = SituacaoConvenioRecebido.EmExecucao;
        }
    }

    /// <summary>
    /// Calcula a devolucao de saldo da PC final (A-INV-6/Selic): saldo nao aplicado (rendimentos retidos),
    /// atualizado pelo indice do tenant conforme o atraso. Marca os rendimentos como considerados na devolucao.
    /// </summary>
    /// <param name="prestacaoId">Identificador da PC final.</param>
    /// <param name="saldoBaseNaoAplicado">Saldo bruto nao aplicado (alem dos rendimentos retidos).</param>
    /// <param name="indice">Indice de devolucao (Selic) do tenant.</param>
    /// <param name="diasAtraso">Dias decorridos desde o vencimento da devolucao.</param>
    public void CalcularDevolucaoFinal(
        Guid prestacaoId,
        Dinheiro saldoBaseNaoAplicado,
        IndiceDevolucao indice,
        int diasAtraso)
    {
        ArgumentNullException.ThrowIfNull(saldoBaseNaoAplicado);
        ArgumentNullException.ThrowIfNull(indice);

        var pc = ObterPrestacao(prestacaoId);
        var rendimentosRetidos = _rendimentos
            .Where(rendimento => !rendimento.Aplicado)
            .Aggregate(Dinheiro.Zero, (acc, rendimento) => acc.Somar(rendimento.Valor));
        var saldoTotal = saldoBaseNaoAplicado.Somar(rendimentosRetidos);
        pc.CalcularDevolucao(saldoTotal, indice, diasAtraso);
    }

    /// <summary>
    /// Declara a inadimplencia (A-INV-10): estado terminal-bloqueio; nenhuma nova parcela e liberada e os
    /// repasses ainda previstos sao bloqueados. Emite <see cref="ConvenioRecebidoInadimplente"/> (gatilho LRF).
    /// </summary>
    /// <param name="motivo">Motivo da inadimplencia (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidConvenioStateException">Se ja estiver em estado terminal.</exception>
    public void DeclararInadimplencia(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirNaoTerminal();

        foreach (var repasse in _repasses)
        {
            repasse.Bloquear();
        }

        Situacao = SituacaoConvenioRecebido.Inadimplente;
        MotivoInadimplencia = motivo.Trim();
        RaiseDomainEvent(new ConvenioRecebidoInadimplente(Id, MotivoInadimplencia));
    }

    private bool ExistePrestacaoParcialSubmetidaParaEtapa(int numeroEtapa)
        => _prestacoes.Exists(pc =>
            pc.Tipo == TipoPrestacaoConvenio.Parcial
            && pc.FoiSubmetida
            && pc.CompetenciaRef.Contains(numeroEtapa.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));

    private PrestacaoContasConvenio ObterPrestacao(Guid prestacaoId)
        => _prestacoes.Find(pc => pc.Id == prestacaoId)
            ?? throw new InvalidConvenioStateException($"PC {prestacaoId} nao encontrada no convenio.");

    private void GarantirNaoTerminal()
    {
        // A-INV-11: estados terminais nao admitem novas transicoes (exceto recurso fora deste escopo).
        if (Situacao is SituacaoConvenioRecebido.Aprovado
            or SituacaoConvenioRecebido.AprovadoComRessalva
            or SituacaoConvenioRecebido.Rejeitado
            or SituacaoConvenioRecebido.Inadimplente)
        {
            throw new InvalidConvenioStateException($"Convenio em estado terminal nao admite a operacao. Situacao atual: {Situacao}.");
        }
    }
}
