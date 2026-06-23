using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>Identificador forte do agregado <see cref="DividaAtiva"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DividaAtivaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DividaAtivaId"/>.</returns>
    public static DividaAtivaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação (estado) da Dívida Ativa.</summary>
public enum SituacaoDividaAtiva
{
    /// <summary>Inscrita em Dívida Ativa.</summary>
    Inscrita = 1,

    /// <summary>Com Certidão de Dívida Ativa (CDA) emitida.</summary>
    CdaEmitida = 2,

    /// <summary>Protestada em cartório.</summary>
    Protestada = 3,

    /// <summary>Em execução fiscal (Lei 6.830/80).</summary>
    EmExecucaoFiscal = 4,

    /// <summary>Parcelada (REFIS) — exigibilidade suspensa.</summary>
    Parcelada = 5,

    /// <summary>Quitada.</summary>
    Quitada = 6,

    /// <summary>Cancelada.</summary>
    Cancelada = 7,
}

/// <summary>
/// Crédito tributário inscrito em Dívida Ativa: título exigível com prazo prescricional de 5 anos a
/// partir da CONSTITUIÇÃO DEFINITIVA (CTN art. 174), passível de CDA (LEF art. 2º §5º), protesto
/// extrajudicial (Lei 9.492/97), execução fiscal (LEF) e parcelamento. Guarda os dados do crédito
/// (origem/natureza, fundamento legal, valor originário, regra de encargos parametrizável) exigidos
/// pela CDA. A prescrição é calculada por DATAS DO FATO + eventos de interrupção (CTN art. 174 p.ú.),
/// nunca pelo relógio do servidor (CLAUDE.md §16).
/// </summary>
public sealed class DividaAtiva : AggregateRoot<DividaAtivaId>, IMustHaveTenant
{
    /// <summary>Prazo prescricional do crédito tributário, em anos (CTN art. 174). // TODO(validar-oficial): parametrizável por tenant via <see cref="AnosPrescricaoParametrizado"/>.</summary>
    public const int AnosPrescricao = 5;

    private readonly List<RemessaProtesto> _remessasProtesto = [];

    private DividaAtiva()
    {
    }

    private DividaAtiva(
        DividaAtivaId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        LancamentoId lancamentoId,
        TipoTributo tipoTributo,
        ValorMonetario valorOriginario,
        DateOnly vencimentoOrigem,
        DateOnly dataConstituicaoDefinitiva,
        DateOnly dataInscricao,
        long numeroInscricao,
        string origemNatureza,
        string fundamentoLegal,
        RegraEncargosDivida regraEncargos,
        int anosPrescricaoParametrizado)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        LancamentoId = lancamentoId;
        TipoTributo = tipoTributo;
        ValorOriginario = valorOriginario;
        VencimentoOrigem = vencimentoOrigem;
        DataConstituicaoDefinitiva = dataConstituicaoDefinitiva;
        DataInscricao = dataInscricao;
        NumeroInscricao = numeroInscricao;
        OrigemNatureza = origemNatureza;
        FundamentoLegal = fundamentoLegal;
        RegraEncargos = regraEncargos;
        AnosPrescricaoParametrizado = anosPrescricaoParametrizado;
        Situacao = SituacaoDividaAtiva.Inscrita;
        RaiseDomainEvent(new DividaAtivaInscrita(id, contribuinteId, lancamentoId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte devedor.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Lançamento de origem.</summary>
    public LancamentoId LancamentoId { get; private set; }

    /// <summary>Espécie tributária de origem (sobrevive à extinção do tributo — ex.: ISS pós-2033).</summary>
    public TipoTributo TipoTributo { get; private set; }

    /// <summary>Valor originário inscrito (R$) — inc. II da CDA.</summary>
    public ValorMonetario ValorOriginario { get; private set; } = default!;

    /// <summary>Vencimento do crédito de origem (termo inicial dos encargos).</summary>
    public DateOnly VencimentoOrigem { get; private set; }

    /// <summary>
    /// Data da constituição definitiva do crédito (marco inicial da prescrição — CTN art. 174). Em regra,
    /// o fim do prazo de impugnação administrativa; aqui informado pelo caso de uso. // TODO(validar-oficial).
    /// </summary>
    public DateOnly DataConstituicaoDefinitiva { get; private set; }

    /// <summary>Data de inscrição em Dívida Ativa.</summary>
    public DateOnly DataInscricao { get; private set; }

    /// <summary>Origem e natureza da dívida (inc. III da CDA).</summary>
    public string OrigemNatureza { get; private set; } = default!;

    /// <summary>Fundamento legal da dívida (inc. III da CDA — lei municipal).</summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Regra de encargos (multa/juros/correção) parametrizável por tenant.</summary>
    public RegraEncargosDivida RegraEncargos { get; private set; } = default!;

    /// <summary>Prazo prescricional parametrizado em anos (default 5 — CTN art. 174).</summary>
    public int AnosPrescricaoParametrizado { get; private set; } = AnosPrescricao;

    /// <summary>Número da CDA, quando emitida.</summary>
    public string? NumeroCda { get; private set; }

    /// <summary>Número sequencial da inscrição no Registro de Dívida Ativa (inc. V da CDA).</summary>
    public long NumeroInscricao { get; private set; }

    /// <summary>
    /// Marco a partir do qual a prescrição volta a correr do zero quando interrompida (CTN art. 174 p.ú.).
    /// Nulo enquanto não houver interrupção; nesse caso o termo inicial é a constituição definitiva.
    /// </summary>
    public DateOnly? DataUltimaInterrupcaoPrescricao { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoDividaAtiva Situacao { get; private set; }

    /// <summary>Remessas de protesto geradas (atos auditáveis ao CRA/cartório).</summary>
    public IReadOnlyCollection<RemessaProtesto> RemessasProtesto => _remessasProtesto.AsReadOnly();

    /// <summary>
    /// Termo inicial efetivo da contagem da prescrição: a data da última interrupção, se houver; senão a
    /// data da constituição definitiva (CTN art. 174 + p.ú.).
    /// </summary>
    public DateOnly TermoInicialPrescricao => DataUltimaInterrupcaoPrescricao ?? DataConstituicaoDefinitiva;

    /// <summary>Data-limite da prescrição = termo inicial + prazo parametrizado (anos).</summary>
    public DateOnly DataPrescricao => TermoInicialPrescricao.AddYears(AnosPrescricaoParametrizado);

    /// <summary>
    /// Inscreve um crédito vencido em Dívida Ativa com TODOS os dados do crédito exigidos pela CDA
    /// (origem/natureza, fundamento legal, valor originário, regra de encargos), o marco de constituição
    /// definitiva (início da prescrição) e o número sequencial da inscrição.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte devedor.</param>
    /// <param name="lancamentoId">Lançamento de origem.</param>
    /// <param name="tipoTributo">Espécie tributária de origem.</param>
    /// <param name="valorOriginario">Valor originário (R$).</param>
    /// <param name="vencimentoOrigem">Vencimento do crédito (termo inicial dos encargos).</param>
    /// <param name="dataConstituicaoDefinitiva">Constituição definitiva (início da prescrição — CTN 174).</param>
    /// <param name="dataInscricao">Data da inscrição.</param>
    /// <param name="numeroInscricao">Número sequencial da inscrição (inc. V da CDA).</param>
    /// <param name="origemNatureza">Origem e natureza da dívida (inc. III).</param>
    /// <param name="fundamentoLegal">Fundamento legal (inc. III — lei municipal).</param>
    /// <param name="regraEncargos">Regra de encargos parametrizável (multa/juros/correção).</param>
    /// <param name="anosPrescricao">Prazo prescricional (anos) — default 5 (CTN art. 174).</param>
    /// <returns>Nova <see cref="DividaAtiva"/>.</returns>
    public static DividaAtiva Inscrever(
        Guid tenantId,
        ContribuinteId contribuinteId,
        LancamentoId lancamentoId,
        TipoTributo tipoTributo,
        ValorMonetario valorOriginario,
        DateOnly vencimentoOrigem,
        DateOnly dataConstituicaoDefinitiva,
        DateOnly dataInscricao,
        long numeroInscricao,
        string origemNatureza,
        string fundamentoLegal,
        RegraEncargosDivida regraEncargos,
        int anosPrescricao = AnosPrescricao)
    {
        ArgumentNullException.ThrowIfNull(valorOriginario);
        ArgumentNullException.ThrowIfNull(regraEncargos);
        ArgumentException.ThrowIfNullOrWhiteSpace(origemNatureza);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        if (!Enum.IsDefined(tipoTributo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoTributo), tipoTributo, "Espécie tributária inválida.");
        }

        if (numeroInscricao <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroInscricao), numeroInscricao, "Número da inscrição deve ser positivo.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(anosPrescricao, 1);
        if (dataInscricao < dataConstituicaoDefinitiva)
        {
            throw new ArgumentOutOfRangeException(nameof(dataInscricao), "A inscrição não pode anteceder a constituição definitiva.");
        }

        return new DividaAtiva(
            DividaAtivaId.New(),
            tenantId,
            contribuinteId,
            lancamentoId,
            tipoTributo,
            valorOriginario,
            vencimentoOrigem,
            dataConstituicaoDefinitiva,
            dataInscricao,
            numeroInscricao,
            origemNatureza.Trim(),
            fundamentoLegal.Trim(),
            regraEncargos,
            anosPrescricao);
    }

    /// <summary>
    /// Apura os encargos (correção/multa/juros) sobre o valor originário na data-base informada, pela
    /// regra parametrizada. Determinístico: depende só de datas do fato.
    /// </summary>
    /// <param name="dataBaseCalculo">Data-base do cálculo (ex.: data da emissão da CDA, do pagamento).</param>
    /// <returns>Os encargos apurados e o valor atualizado.</returns>
    public EncargosApurados ApurarEncargos(DateOnly dataBaseCalculo)
        => RegraEncargos.Apurar(ValorOriginario, VencimentoOrigem, dataBaseCalculo);

    /// <summary>
    /// Emite a Certidão de Dívida Ativa (CDA) com os requisitos legais obrigatórios (LEF art. 2º §5º
    /// I–VI / CTN art. 202). RECUSA se faltar requisito (a própria <see cref="CertidaoDividaAtiva"/> valida).
    /// O número da CDA fica registrado no agregado e a CDA válida é retornada para impressão/exportação.
    /// </summary>
    /// <param name="numeroCda">Número da CDA.</param>
    /// <param name="nomeDevedor">Nome do devedor (inc. I).</param>
    /// <param name="domicilioDevedor">Domicílio do devedor (inc. I, opcional).</param>
    /// <param name="coResponsaveis">Co-responsáveis (inc. I, opcional).</param>
    /// <param name="dataBaseEncargos">Data-base para descrever a forma de cálculo dos encargos.</param>
    /// <param name="processoAdministrativo">Nº do processo administrativo (inc. VI, opcional).</param>
    /// <returns>A CDA válida.</returns>
    /// <exception cref="InvalidOperationException">Se a dívida não estiver recém-inscrita.</exception>
    /// <exception cref="CdaRequisitoAusenteException">Se faltar requisito legal.</exception>
    public CertidaoDividaAtiva EmitirCda(
        string numeroCda,
        string nomeDevedor,
        string? domicilioDevedor,
        string? coResponsaveis,
        DateOnly dataBaseEncargos,
        string? processoAdministrativo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroCda);
        if (Situacao != SituacaoDividaAtiva.Inscrita)
        {
            throw new InvalidOperationException($"A CDA só pode ser emitida para dívida recém-inscrita. Situação atual: {Situacao}.");
        }

        var formaCalculo =
            $"Multa de mora {RegraEncargos.MultaMoraPercentual}%, juros de mora {RegraEncargos.JurosMoraPercentualMensal}% a.m. e correção {RegraEncargos.CorrecaoPercentualMensal}% a.m. desde o vencimento ({VencimentoOrigem:dd/MM/yyyy}); fund.: {RegraEncargos.FundamentoLegal}.";

        var certidao = CertidaoDividaAtiva.Emitir(
            numeroCda,
            nomeDevedor,
            domicilioDevedor,
            coResponsaveis,
            ValorOriginario,
            VencimentoOrigem,
            formaCalculo,
            OrigemNatureza,
            FundamentoLegal,
            RegraEncargos.FundamentoLegal,
            DataInscricao,
            NumeroInscricao,
            processoAdministrativo);

        NumeroCda = certidao.Numero;
        Situacao = SituacaoDividaAtiva.CdaEmitida;
        RaiseDomainEvent(new CdaEmitida(Id, certidao.Numero));
        return certidao;
    }

    /// <summary>
    /// Gera a remessa de protesto extrajudicial da CDA ao CRA estadual (Lei 9.492/97; STF ADI 5.135).
    /// Registra o ATO no agregado (auditável). A geração do leiaute e a transmissão real ficam na ACL.
    /// </summary>
    /// <param name="identificadorCra">CRA estadual de destino (ex.: "CRA-RS").</param>
    /// <param name="dataGeracao">Data de geração da remessa (data do fato).</param>
    /// <returns>A remessa gerada.</returns>
    /// <exception cref="InvalidOperationException">Se não houver CDA emitida ou a dívida não estiver exigível.</exception>
    public RemessaProtesto GerarRemessaProtesto(string identificadorCra, DateOnly dataGeracao)
    {
        GarantirExigivel();
        if (Situacao != SituacaoDividaAtiva.CdaEmitida || string.IsNullOrWhiteSpace(NumeroCda))
        {
            throw new InvalidOperationException("O protesto requer CDA emitida.");
        }

        var remessa = RemessaProtesto.Gerar(TenantId, Id, NumeroCda!, identificadorCra, dataGeracao);
        _remessasProtesto.Add(remessa);
        Situacao = SituacaoDividaAtiva.Protestada;
        RaiseDomainEvent(new RemessaProtestoGerada(Id, TenantId, NumeroCda!, identificadorCra));
        return remessa;
    }

    /// <summary>Marca uma remessa de protesto como transmitida ao CRA.</summary>
    /// <param name="remessaId">Remessa a transmitir.</param>
    /// <param name="dataTransmissao">Data da transmissão.</param>
    /// <exception cref="InvalidOperationException">Se a remessa não pertencer à dívida.</exception>
    public void RegistrarTransmissaoProtesto(RemessaProtestoId remessaId, DateOnly dataTransmissao)
    {
        var remessa = ObterRemessa(remessaId);
        remessa.MarcarTransmitida(dataTransmissao);
    }

    /// <summary>
    /// Processa o retorno do CRA/cartório de uma remessa de protesto. Quando a ocorrência é pagamento/retirada,
    /// a dívida é quitada (o pagamento ocorre no cartório). Demais ocorrências apenas registram o ato.
    /// </summary>
    /// <param name="remessaId">Remessa cujo retorno chegou.</param>
    /// <param name="ocorrencia">Ocorrência de retorno.</param>
    /// <param name="dataRetorno">Data do retorno.</param>
    /// <param name="protocoloCartorio">Protocolo do cartório (opcional).</param>
    /// <exception cref="InvalidOperationException">Se a remessa não pertencer à dívida.</exception>
    public void ProcessarRetornoProtesto(
        RemessaProtestoId remessaId,
        OcorrenciaProtesto ocorrencia,
        DateOnly dataRetorno,
        string? protocoloCartorio)
    {
        var remessa = ObterRemessa(remessaId);
        remessa.ProcessarRetorno(ocorrencia, dataRetorno, protocoloCartorio);
        RaiseDomainEvent(new RetornoProtestoProcessado(Id, TenantId, ocorrencia));

        if (ocorrencia == OcorrenciaProtesto.PagoOuRetirado && Situacao is not (SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada))
        {
            Situacao = SituacaoDividaAtiva.Quitada;
            RaiseDomainEvent(new DividaQuitada(Id));
        }
    }

    /// <summary>
    /// Ajuíza a execução fiscal (Lei 6.830/80) — GANCHO de saída: o ERP gera/exporta a CDA + petição; o
    /// ajuizamento ocorre no PJe/eproc-RS (integração de saída é decisão de produto). Apenas muda o estado.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se não houver CDA emitida/protestada ou a dívida não estiver exigível.</exception>
    public void AjuizarExecucaoFiscal()
    {
        GarantirExigivel();
        if (Situacao is not (SituacaoDividaAtiva.CdaEmitida or SituacaoDividaAtiva.Protestada))
        {
            throw new InvalidOperationException("A execução fiscal requer CDA emitida (eventualmente protestada).");
        }

        Situacao = SituacaoDividaAtiva.EmExecucaoFiscal;
        RaiseDomainEvent(new ExecucaoFiscalAjuizada(Id, TenantId, NumeroCda));
    }

    /// <summary>
    /// Firma um parcelamento (REFIS): suspende a exigibilidade e INTERROMPE a prescrição (CTN art. 174 p.ú.
    /// IV — reconhecimento do débito). O prazo prescricional reinicia da data do reconhecimento.
    /// </summary>
    /// <param name="dataReconhecimento">Data do reconhecimento/parcelamento (interrompe a prescrição).</param>
    /// <exception cref="InvalidOperationException">Se a dívida não estiver exigível.</exception>
    public void FirmarParcelamento(DateOnly dataReconhecimento)
    {
        GarantirExigivel();
        DataUltimaInterrupcaoPrescricao = dataReconhecimento;
        Situacao = SituacaoDividaAtiva.Parcelada;
        RaiseDomainEvent(new ParcelamentoFirmado(Id));
    }

    /// <summary>
    /// Registra a interrupção da prescrição (CTN art. 174 p.ú.: despacho que ordena a citação — retroage
    /// ao ajuizamento; protesto judicial; mora; reconhecimento do débito). Reinicia o quinquênio.
    /// </summary>
    /// <param name="dataInterrupcao">Data do marco interruptivo (data do fato).</param>
    /// <exception cref="InvalidOperationException">Se a dívida já estiver encerrada.</exception>
    public void InterromperPrescricao(DateOnly dataInterrupcao)
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada)
        {
            throw new InvalidOperationException($"Não há prescrição a interromper: dívida encerrada. Situação atual: {Situacao}.");
        }

        DataUltimaInterrupcaoPrescricao = dataInterrupcao;
        RaiseDomainEvent(new PrescricaoInterrompida(Id, TenantId, dataInterrupcao));
    }

    /// <summary>Quita a dívida.</summary>
    /// <exception cref="InvalidOperationException">Se a dívida já estiver encerrada.</exception>
    public void Quitar()
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada)
        {
            throw new InvalidOperationException($"Dívida já encerrada. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoDividaAtiva.Quitada;
        RaiseDomainEvent(new DividaQuitada(Id));
    }

    /// <summary>
    /// Indica se a dívida está prescrita na data informada (CTN art. 174). Considera o termo inicial
    /// efetivo (constituição definitiva ou última interrupção). Dívida suspensa (parcelada), quitada ou
    /// cancelada não prescreve.
    /// </summary>
    /// <param name="referencia">Data de referência (informada — sem relógio no domínio).</param>
    /// <returns><c>true</c> se prescrita.</returns>
    public bool EstaPrescrita(DateOnly referencia)
        => Situacao is not (SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Parcelada or SituacaoDividaAtiva.Cancelada)
        && referencia > DataPrescricao;

    private RemessaProtesto ObterRemessa(RemessaProtestoId remessaId)
        => _remessasProtesto.FirstOrDefault(remessa => remessa.Id == remessaId)
        ?? throw new InvalidOperationException("Remessa de protesto não pertence a esta dívida.");

    private void GarantirExigivel()
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada or SituacaoDividaAtiva.Parcelada)
        {
            throw new InvalidOperationException($"A dívida não está exigível. Situação atual: {Situacao}.");
        }
    }
}
