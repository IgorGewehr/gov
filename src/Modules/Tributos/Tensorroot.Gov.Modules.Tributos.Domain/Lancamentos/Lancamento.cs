using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

/// <summary>Identificador forte do agregado <see cref="Lancamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LancamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LancamentoId"/>.</returns>
    public static LancamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Espécie tributária do lançamento.</summary>
public enum TipoTributo
{
    /// <summary>Imposto Predial e Territorial Urbano.</summary>
    Iptu = 1,

    /// <summary>Imposto Sobre Serviços de Qualquer Natureza.</summary>
    Iss = 2,

    /// <summary>Imposto sobre Transmissão de Bens Imóveis.</summary>
    Itbi = 3,

    /// <summary>Taxa (poder de polícia ou serviço).</summary>
    Taxa = 4,

    /// <summary>Contribuição de Melhoria.</summary>
    ContribuicaoMelhoria = 5,

    /// <summary>Contribuição para Custeio da Iluminação Pública.</summary>
    Cosip = 6,
}

/// <summary>Situação (estado) do lançamento tributário.</summary>
public enum SituacaoLancamento
{
    /// <summary>Em aberto (exigível, ainda não pago).</summary>
    Aberto = 1,

    /// <summary>Pago/quitado.</summary>
    Pago = 2,

    /// <summary>Inscrito em Dívida Ativa.</summary>
    InscritoEmDividaAtiva = 3,

    /// <summary>Cancelado.</summary>
    Cancelado = 4,
}

/// <summary>
/// Modalidade do lançamento, que determina o TERMO INICIAL da decadência (R4) —
/// [revisao-humana-juridica].
/// </summary>
public enum TipoLancamento
{
    /// <summary>
    /// Lançamento de ofício (CTN art. 149) — IPTU, taxas, ITBI, COSIP, contribuição de melhoria.
    /// Decadência conta do 1º dia do exercício SEGUINTE ao fato gerador (CTN art. 173, I).
    /// </summary>
    Oficio = 1,

    /// <summary>
    /// Lançamento por homologação (CTN art. 150) — ISS. Decadência conta do FATO GERADOR (art. 150,
    /// §4º) quando houve pagamento antecipado e não há dolo/fraude/simulação; do contrário, art. 173, I.
    /// </summary>
    Homologacao = 2,
}

/// <summary>
/// Lançamento tributário (crédito tributário constituído — CTN art. 142): o ato que
/// torna a obrigação líquida e exigível contra o contribuinte.
/// </summary>
public sealed class Lancamento : AggregateRoot<LancamentoId>, IMustHaveTenant
{
    /// <summary>
    /// Prazo decadencial padrão do direito de lançar, em anos (CTN art. 173, I). Parametrizável por
    /// tenant via o parâmetro <c>anosDecadencia</c> das factories — nunca hardcoded no cálculo.
    /// </summary>
    public const int AnosDecadenciaPadrao = 5;

    private Lancamento()
    {
    }

    private Lancamento(
        LancamentoId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        TipoTributo tipoTributo,
        Competencia competencia,
        ValorMonetario valorPrincipal,
        DateOnly vencimento,
        DateOnly dataFatoGerador,
        DateOnly dataConstituicao,
        int anosDecadencia,
        TipoLancamento tipoLancamento,
        bool houvePagamentoAntecipado,
        bool doloFraudeSimulacao,
        ImovelId? imovelId)
        : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(anosDecadencia, 1);

        // INVARIANTE DE DECADÊNCIA — [revisao-humana-juridica] (R4): o TERMO INICIAL depende da
        // modalidade do lançamento.
        //  • Ofício (IPTU/Taxas/ITBI/COSIP/Contrib.Melhoria): 1º dia do exercício SEGUINTE (art. 173, I).
        //  • Homologação (ISS) COM pagamento antecipado e SEM dolo/fraude/simulação: do FATO GERADOR (art. 150, §4º).
        //  • Homologação SEM pagamento (Súmula 555/STJ) ou COM dolo/fraude/simulação: recai no art. 173, I.
        // O direito extingue-se APÓS o prazo; constituição EM ou APÓS a data-limite é NULA e insanável —
        // recusada AQUI (fail-closed), antes de qualquer mutação de estado ou publicação de evento.
        var dataLimite = CalcularDataLimiteDecadencia(
            dataFatoGerador, anosDecadencia, tipoLancamento, houvePagamentoAntecipado, doloFraudeSimulacao);
        if (dataConstituicao >= dataLimite)
        {
            throw new CreditoTributarioDecaidoException(dataFatoGerador.Year, dataLimite, dataConstituicao);
        }

        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        TipoTributo = tipoTributo;
        Competencia = competencia;
        ValorPrincipal = valorPrincipal;
        Vencimento = vencimento;
        DataFatoGerador = dataFatoGerador;
        DataConstituicao = dataConstituicao;
        AnosDecadencia = anosDecadencia;
        TipoLancamento = tipoLancamento;
        HouvePagamentoAntecipado = houvePagamentoAntecipado;
        DoloFraudeSimulacao = doloFraudeSimulacao;
        ImovelId = imovelId;
        Situacao = SituacaoLancamento.Aberto;
        RaiseDomainEvent(new CreditoTributarioLancado(id, contribuinteId, valorPrincipal.Valor));
    }

    /// <summary>
    /// Data-limite da decadência por OFÍCIO (CTN art. 173, I): 1º dia do exercício SEGUINTE ao fato
    /// gerador + prazo (anos). Sobrecarga de compatibilidade — delega à regra completa (R4).
    /// </summary>
    /// <param name="dataFatoGerador">Data do fato gerador.</param>
    /// <param name="anosDecadencia">Prazo decadencial em anos (parametrizável por tenant).</param>
    /// <returns>Data-limite para constituir o crédito sem decadência.</returns>
    public static DateOnly CalcularDataLimiteDecadencia(DateOnly dataFatoGerador, int anosDecadencia)
        => CalcularDataLimiteDecadencia(
            dataFatoGerador, anosDecadencia, TipoLancamento.Oficio,
            houvePagamentoAntecipado: false, doloFraudeSimulacao: false);

    /// <summary>
    /// Data-limite da decadência segundo a MODALIDADE do lançamento (R4) — [revisao-humana-juridica].
    /// Determinístico (só datas/flags do fato; sem relógio): CTN art. 150 §4º (homologação COM
    /// pagamento e SEM dolo) conta do FATO GERADOR; caso contrário, CTN art. 173, I (exercício seguinte).
    /// </summary>
    /// <param name="dataFatoGerador">Data do fato gerador.</param>
    /// <param name="anosDecadencia">Prazo decadencial em anos (parametrizável por tenant).</param>
    /// <param name="tipoLancamento">Modalidade (ofício/homologação).</param>
    /// <param name="houvePagamentoAntecipado">Se houve pagamento antecipado (relevante só na homologação).</param>
    /// <param name="doloFraudeSimulacao">Se há dolo/fraude/simulação (afasta o §4º).</param>
    /// <returns>Data-limite para constituir o crédito sem decadência.</returns>
    public static DateOnly CalcularDataLimiteDecadencia(
        DateOnly dataFatoGerador,
        int anosDecadencia,
        TipoLancamento tipoLancamento,
        bool houvePagamentoAntecipado,
        bool doloFraudeSimulacao)
    {
        // CTN 150 §4º (conta do FATO GERADOR) só se aplica à HOMOLOGAÇÃO com pagamento antecipado e
        // sem dolo/fraude/simulação. Senão (ofício, ou homologação sem pagamento [Súmula 555/STJ],
        // ou com dolo): CTN 173, I — 1º dia do exercício seguinte.
        var aplica150 = tipoLancamento == TipoLancamento.Homologacao
            && houvePagamentoAntecipado
            && !doloFraudeSimulacao;

        var termoInicial = aplica150
            ? dataFatoGerador
            : new DateOnly(dataFatoGerador.Year + 1, 1, 1);

        return termoInicial.AddYears(anosDecadencia);
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte devedor.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Espécie tributária.</summary>
    public TipoTributo TipoTributo { get; private set; }

    /// <summary>Competência fiscal.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Valor principal lançado.</summary>
    public ValorMonetario ValorPrincipal { get; private set; } = default!;

    /// <summary>Data de vencimento.</summary>
    public DateOnly Vencimento { get; private set; }

    /// <summary>
    /// Data do fato gerador da obrigação tributária — marco a partir do qual se conta a decadência
    /// (CTN art. 173, I: do 1º dia do exercício seguinte). Persistida para auditoria e rastreabilidade.
    /// </summary>
    public DateOnly DataFatoGerador { get; private set; }

    /// <summary>Data em que o crédito foi efetivamente constituído (lançado).</summary>
    public DateOnly DataConstituicao { get; private set; }

    /// <summary>Prazo decadencial aplicado a este lançamento, em anos (parametrizável por tenant).</summary>
    public int AnosDecadencia { get; private set; } = AnosDecadenciaPadrao;

    /// <summary>
    /// Modalidade do lançamento (R4) — determina o termo inicial da decadência. [revisao-humana-juridica]
    /// </summary>
    public TipoLancamento TipoLancamento { get; private set; }

    /// <summary>
    /// Se houve pagamento antecipado pelo sujeito passivo (relevante só na homologação/ISS): com
    /// pagamento e sem dolo, a decadência conta do fato gerador (CTN 150 §4º); sem pagamento, recai
    /// no art. 173, I (Súmula 555/STJ). [revisao-humana-juridica]
    /// </summary>
    public bool HouvePagamentoAntecipado { get; private set; }

    /// <summary>
    /// Dolo, fraude ou simulação — afasta a contagem do §4º e recai no CTN 173, I (R4). [revisao-humana-juridica]
    /// </summary>
    public bool DoloFraudeSimulacao { get; private set; }

    /// <summary>
    /// Imóvel de origem, quando o lançamento decorre do cadastro imobiliário (IPTU). Nulo para
    /// lançamentos não vinculados a imóvel (ex.: ISS).
    /// </summary>
    public ImovelId? ImovelId { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoLancamento Situacao { get; private set; }

    /// <summary>Constitui (lança) o crédito tributário.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte devedor.</param>
    /// <param name="tipoTributo">Espécie tributária.</param>
    /// <param name="competencia">Competência fiscal.</param>
    /// <param name="valorPrincipal">Valor principal.</param>
    /// <param name="vencimento">Data de vencimento.</param>
    /// <param name="dataFatoGerador">Data do fato gerador (marco da decadência — CTN art. 173, I).</param>
    /// <param name="dataConstituicao">Data da constituição do crédito (data do fato — "hoje" administrativo).</param>
    /// <param name="anosDecadencia">Prazo decadencial em anos (parametrizável por tenant); padrão 5 (CTN art. 173).</param>
    /// <param name="tipoLancamento">Modalidade (ofício/homologação) — define o termo inicial da decadência (R4).</param>
    /// <param name="houvePagamentoAntecipado">Se houve pagamento antecipado (relevante só na homologação).</param>
    /// <param name="doloFraudeSimulacao">Se há dolo/fraude/simulação (afasta o §4º da homologação).</param>
    /// <returns>Novo <see cref="Lancamento"/> em aberto.</returns>
    /// <exception cref="CreditoTributarioDecaidoException">Se o crédito já estiver decaído (CTN art. 173, I).</exception>
    public static Lancamento Lancar(
        Guid tenantId,
        ContribuinteId contribuinteId,
        TipoTributo tipoTributo,
        Competencia competencia,
        ValorMonetario valorPrincipal,
        DateOnly vencimento,
        DateOnly dataFatoGerador,
        DateOnly dataConstituicao,
        int anosDecadencia = AnosDecadenciaPadrao,
        TipoLancamento tipoLancamento = TipoLancamento.Oficio,
        bool houvePagamentoAntecipado = false,
        bool doloFraudeSimulacao = false)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentNullException.ThrowIfNull(valorPrincipal);
        return new Lancamento(LancamentoId.New(), tenantId, contribuinteId, tipoTributo, competencia, valorPrincipal, vencimento, dataFatoGerador, dataConstituicao, anosDecadencia, tipoLancamento, houvePagamentoAntecipado, doloFraudeSimulacao, imovelId: null);
    }

    /// <summary>
    /// Constitui (lança) o crédito tributário com vínculo OPCIONAL a um imóvel — usado por espécies que
    /// podem decorrer de imóvel (taxas de serviço/poder de polícia vinculadas ao imóvel, contribuição de
    /// melhoria) sem que o vínculo seja obrigatório.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte devedor.</param>
    /// <param name="tipoTributo">Espécie tributária.</param>
    /// <param name="competencia">Competência fiscal.</param>
    /// <param name="valorPrincipal">Valor principal.</param>
    /// <param name="vencimento">Data de vencimento.</param>
    /// <param name="dataFatoGerador">Data do fato gerador (marco da decadência — CTN art. 173, I).</param>
    /// <param name="dataConstituicao">Data da constituição do crédito (data do fato — "hoje" administrativo).</param>
    /// <param name="imovelId">Imóvel de origem (opcional).</param>
    /// <param name="anosDecadencia">Prazo decadencial em anos (parametrizável por tenant); padrão 5 (CTN art. 173).</param>
    /// <returns>Novo <see cref="Lancamento"/> em aberto.</returns>
    /// <exception cref="CreditoTributarioDecaidoException">Se o crédito já estiver decaído (CTN art. 173, I).</exception>
    public static Lancamento LancarComImovel(
        Guid tenantId,
        ContribuinteId contribuinteId,
        TipoTributo tipoTributo,
        Competencia competencia,
        ValorMonetario valorPrincipal,
        DateOnly vencimento,
        DateOnly dataFatoGerador,
        DateOnly dataConstituicao,
        ImovelId? imovelId,
        int anosDecadencia = AnosDecadenciaPadrao)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentNullException.ThrowIfNull(valorPrincipal);
        if (!Enum.IsDefined(tipoTributo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoTributo), tipoTributo, "Espécie tributária inválida.");
        }

        return new Lancamento(LancamentoId.New(), tenantId, contribuinteId, tipoTributo, competencia, valorPrincipal, vencimento, dataFatoGerador, dataConstituicao, anosDecadencia, TipoLancamento.Oficio, houvePagamentoAntecipado: false, doloFraudeSimulacao: false, imovelId);
    }

    /// <summary>
    /// Lança o IPTU anual de ofício (CTN art. 142/149) de um imóvel: uma constituição por exercício,
    /// vinculada ao imóvel de origem. A competência usa o mês 1 (lançamento anual).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte (proprietário do imóvel).</param>
    /// <param name="imovelId">Imóvel de origem.</param>
    /// <param name="exercicio">Exercício fiscal (ano).</param>
    /// <param name="valorPrincipal">IPTU devido apurado.</param>
    /// <param name="vencimento">Vencimento da cota única / 1ª parcela.</param>
    /// <param name="dataConstituicao">Data da constituição do crédito (data do fato — "hoje" administrativo).</param>
    /// <param name="anosDecadencia">Prazo decadencial em anos (parametrizável por tenant); padrão 5 (CTN art. 173).</param>
    /// <returns>Novo <see cref="Lancamento"/> de IPTU em aberto.</returns>
    /// <exception cref="CreditoTributarioDecaidoException">Se o crédito já estiver decaído (CTN art. 173, I).</exception>
    public static Lancamento LancarIptu(
        Guid tenantId,
        ContribuinteId contribuinteId,
        ImovelId imovelId,
        int exercicio,
        ValorMonetario valorPrincipal,
        DateOnly vencimento,
        DateOnly dataConstituicao,
        int anosDecadencia = AnosDecadenciaPadrao)
    {
        ArgumentNullException.ThrowIfNull(valorPrincipal);

        // Fato gerador do IPTU: 1º de janeiro do exercício (CTN art. 32 + lei municipal). Marco para
        // a contagem da decadência (CTN art. 173, I).
        var dataFatoGerador = new DateOnly(exercicio, 1, 1);
        return new Lancamento(
            LancamentoId.New(),
            tenantId,
            contribuinteId,
            TipoTributo.Iptu,
            Competencia.De(exercicio, 1),
            valorPrincipal,
            vencimento,
            dataFatoGerador,
            dataConstituicao,
            anosDecadencia,
            TipoLancamento.Oficio,
            houvePagamentoAntecipado: false,
            doloFraudeSimulacao: false,
            imovelId);
    }

    /// <summary>
    /// Lança o ISS por HOMOLOGAÇÃO (CTN art. 150) — R4, [revisao-humana-juridica]. A decadência conta
    /// do FATO GERADOR quando houve pagamento antecipado e não há dolo/fraude/simulação (art. 150 §4º);
    /// sem pagamento antecipado (Súmula 555/STJ) ou com dolo/fraude/simulação, recai no art. 173, I.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte (prestador) devedor do ISS próprio.</param>
    /// <param name="competencia">Competência fiscal.</param>
    /// <param name="valorPrincipal">ISS próprio apurado.</param>
    /// <param name="vencimento">Data de vencimento.</param>
    /// <param name="dataFatoGerador">Data do fato gerador (prestação do serviço na competência).</param>
    /// <param name="dataConstituicao">Data da constituição do crédito ("hoje" administrativo).</param>
    /// <param name="houvePagamentoAntecipado">Se houve recolhimento antecipado do ISS na competência.</param>
    /// <param name="doloFraudeSimulacao">Se há dolo/fraude/simulação (afasta o §4º). Padrão: não.</param>
    /// <param name="anosDecadencia">Prazo decadencial em anos (parametrizável por tenant); padrão 5.</param>
    /// <returns>Novo <see cref="Lancamento"/> de ISS em aberto.</returns>
    /// <exception cref="CreditoTributarioDecaidoException">Se o crédito já estiver decaído.</exception>
    public static Lancamento LancarIssPorHomologacao(
        Guid tenantId,
        ContribuinteId contribuinteId,
        Competencia competencia,
        ValorMonetario valorPrincipal,
        DateOnly vencimento,
        DateOnly dataFatoGerador,
        DateOnly dataConstituicao,
        bool houvePagamentoAntecipado,
        bool doloFraudeSimulacao = false,
        int anosDecadencia = AnosDecadenciaPadrao)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentNullException.ThrowIfNull(valorPrincipal);
        return new Lancamento(
            LancamentoId.New(),
            tenantId,
            contribuinteId,
            TipoTributo.Iss,
            competencia,
            valorPrincipal,
            vencimento,
            dataFatoGerador,
            dataConstituicao,
            anosDecadencia,
            TipoLancamento.Homologacao,
            houvePagamentoAntecipado,
            doloFraudeSimulacao,
            imovelId: null);
    }

    /// <summary>Registra a quitação do lançamento.</summary>
    /// <exception cref="InvalidOperationException">Se o lançamento não estiver em aberto.</exception>
    public void RegistrarPagamento()
    {
        if (Situacao != SituacaoLancamento.Aberto)
        {
            throw new InvalidOperationException($"Só é possível pagar um lançamento em aberto. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoLancamento.Pago;
        RaiseDomainEvent(new PagamentoRegistrado(Id));
    }

    /// <summary>Marca o lançamento como inscrito em Dívida Ativa (somente se vencido e em aberto).</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <exception cref="InvalidOperationException">Se não estiver em aberto ou ainda não vencido.</exception>
    public void InscreverEmDividaAtiva(DateOnly hoje)
    {
        if (Situacao != SituacaoLancamento.Aberto)
        {
            throw new InvalidOperationException($"Apenas lançamentos em aberto podem ser inscritos em dívida ativa. Situação atual: {Situacao}.");
        }

        if (hoje <= Vencimento)
        {
            throw new InvalidOperationException("O lançamento ainda não está vencido.");
        }

        Situacao = SituacaoLancamento.InscritoEmDividaAtiva;
        RaiseDomainEvent(new LancamentoInscritoEmDividaAtiva(Id, ContribuinteId));
    }

    /// <summary>Cancela o lançamento (vedado se já pago).</summary>
    /// <exception cref="InvalidOperationException">Se o lançamento já estiver pago.</exception>
    public void Cancelar()
    {
        if (Situacao == SituacaoLancamento.Pago)
        {
            throw new InvalidOperationException("Não é possível cancelar um lançamento já pago.");
        }

        Situacao = SituacaoLancamento.Cancelado;
    }
}
