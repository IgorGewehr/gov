using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

/// <summary>
/// Certidao de Tempo de Servico / Tempo de Contribuicao (CTC) de um servidor: documento oficial que apura e
/// declara o tempo do servidor (efetivo exercicio proprio + tempo averbado de outros orgaos/regimes), com a
/// finalidade que determina o regime de contagem (aposentadoria/disponibilidade — tempo de contribuicao;
/// adicional/licenca-premio — tempo de efetivo exercicio). Numeracao oficial sequencial por exercicio e
/// codigo de autenticacao para validacao publica.
/// <para>
/// INVARIANTES LEGAIS modeladas (// TODO(validar-oficial): regulamento do regime de destino + Portaria MPS
/// 154/2008 + EC 103/2019 + Lei 8.213/1991 art. 96):
/// <list type="bullet">
/// <item>I-1: ao menos um periodo computado (certidao vazia nao tem efeito).</item>
/// <item>I-2: VEDACAO de contagem CONCOMITANTE (art. 96, II, Lei 8.213/1991) — periodos nao podem se sobrepor
/// no tempo; tempo concomitante e' contado UMA so vez.</item>
/// <item>I-3: o total e' a soma dos dias EQUIVALENTES (apos fator de conversao e abatimento de nao-computaveis).</item>
/// <item>I-4: a numeracao consumida e' preservada apos a anulacao (a sequencia do exercicio NAO retrocede).</item>
/// </list>
/// </para>
/// Raiz de agregado <see cref="IMustHaveTenant"/>; nasce valida via <see cref="Emitir"/>.
/// </summary>
public sealed class CertidaoTempoServico : AggregateRoot<CertidaoTempoServicoId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome do orgao emissor.</summary>
    public const int ComprimentoMaximoOrgaoEmissor = 200;

    /// <summary>Comprimento maximo da finalidade descrita (texto-fim).</summary>
    public const int ComprimentoMaximoFinalidadeDescrita = 500;

    /// <summary>Comprimento maximo da observacao geral.</summary>
    public const int ComprimentoMaximoObservacao = 2000;

    private readonly List<PeriodoTempo> _periodos = [];

    private CertidaoTempoServico()
    {
    }

    private CertidaoTempoServico(
        CertidaoTempoServicoId id,
        Guid tenantId,
        ServidorId servidorId,
        NumeroCertidao numero,
        FinalidadeCertidao finalidade,
        DateOnly dataEmissao,
        string orgaoEmissor,
        string? finalidadeDescrita,
        string? observacao,
        IReadOnlyCollection<PeriodoTempo> periodos)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        Exercicio = numero.Exercicio;
        Sequencial = numero.Sequencial;
        Finalidade = finalidade;
        DataEmissao = dataEmissao;
        OrgaoEmissor = orgaoEmissor;
        FinalidadeDescrita = finalidadeDescrita;
        Observacao = observacao;
        _periodos.AddRange(periodos);
        Situacao = SituacaoCertidao.Emitida;
        // CodigoAutenticacao e' selado por DefinirAutenticacao na mesma transacao (digest calculado na borda).
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor certificado.</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Exercicio (ano civil) da numeracao oficial — persistido para indexar/ordenar.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Sequencial da numeracao oficial dentro do exercicio — persistido para indexar/ordenar.</summary>
    public int Sequencial { get; private set; }

    /// <summary>Numeracao oficial (VO) reconstruida do par <see cref="Exercicio"/>/<see cref="Sequencial"/>.</summary>
    public NumeroCertidao Numero => NumeroCertidao.De(Exercicio, Sequencial);

    /// <summary>Finalidade da certidao (determina o regime de contagem e o texto-fim).</summary>
    public FinalidadeCertidao Finalidade { get; private set; }

    /// <summary>Data de emissao (data civil do tenant, fornecida pela borda).</summary>
    public DateOnly DataEmissao { get; private set; }

    /// <summary>Orgao/setor emissor (ex.: Departamento de Recursos Humanos da Prefeitura).</summary>
    public string OrgaoEmissor { get; private set; } = default!;

    /// <summary>Texto descritivo da finalidade (ex.: "averbacao no RGPS para aposentadoria"); opcional.</summary>
    public string? FinalidadeDescrita { get; private set; }

    /// <summary>Observacao geral da certidao (texto livre); opcional.</summary>
    public string? Observacao { get; private set; }

    /// <summary>Situacao (estado) atual da certidao.</summary>
    public SituacaoCertidao Situacao { get; private set; }

    /// <summary>Codigo de autenticacao para validacao publica; selado por <see cref="DefinirAutenticacao"/> na emissao.</summary>
    public CodigoAutenticacao CodigoAutenticacao { get; private set; } = default!;

    /// <summary>Motivo da anulacao, quando anulada; nulo enquanto vigente.</summary>
    public string? MotivoAnulacao { get; private set; }

    /// <summary>Periodos computados na certidao (ordenados por inicio), expostos somente pela raiz.</summary>
    public IReadOnlyList<PeriodoTempo> Periodos => _periodos;

    /// <summary>Total de dias EQUIVALENTES certificados (soma dos periodos apos fatores/abatimentos — I-3).</summary>
    public int TotalDias => _periodos.Sum(periodo => periodo.DiasEquivalentes);

    /// <summary>Total de dias LIQUIDOS (sem aplicar fator de conversao — tempo simples para conferencia).</summary>
    public int TotalDiasLiquidos => _periodos.Sum(periodo => periodo.DiasLiquidos);

    /// <summary>Tempo total decomposto em anos/meses/dias (convencao civil 365/30) — view do <see cref="TotalDias"/>.</summary>
    public TempoDecomposto TempoTotal => TempoDecomposto.De(TotalDias);

    /// <summary>
    /// Emite uma certidao de tempo apurando os periodos informados (efetivo exercicio + averbados). Valida
    /// as invariantes I-1 (ao menos um periodo) e I-2 (vedacao de concomitancia — periodos nao se sobrepoem).
    /// A numeracao SEQUENCIAL e' apurada na borda. Nasce em <see cref="SituacaoCertidao.Emitida"/>; o codigo de
    /// autenticacao e' selado em seguida por <see cref="DefinirAutenticacao"/> (digest calculado na Application).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor certificado.</param>
    /// <param name="numero">Numeracao oficial atribuida (sequencial/exercicio).</param>
    /// <param name="finalidade">Finalidade da certidao.</param>
    /// <param name="dataEmissao">Data de emissao (data civil do tenant).</param>
    /// <param name="orgaoEmissor">Orgao/setor emissor (nao vazio, ate 200 caracteres).</param>
    /// <param name="periodos">Periodos computados (ao menos um; sem sobreposicao).</param>
    /// <param name="finalidadeDescrita">Texto descritivo da finalidade (opcional).</param>
    /// <param name="observacao">Observacao geral (opcional).</param>
    /// <returns>Nova <see cref="CertidaoTempoServico"/> em situacao <see cref="SituacaoCertidao.Emitida"/>.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="numero"/> ou <paramref name="periodos"/> forem nulos.</exception>
    /// <exception cref="ArgumentException">Se o orgao emissor for vazio/longo demais, ou os textos excederem o limite.</exception>
    /// <exception cref="CertidaoTempoServicoException">Se nao houver periodo (I-1) ou houver concomitancia (I-2).</exception>
    public static CertidaoTempoServico Emitir(
        Guid tenantId,
        ServidorId servidorId,
        NumeroCertidao numero,
        FinalidadeCertidao finalidade,
        DateOnly dataEmissao,
        string orgaoEmissor,
        IReadOnlyCollection<PeriodoTempo> periodos,
        string? finalidadeDescrita = null,
        string? observacao = null)
    {
        ArgumentNullException.ThrowIfNull(numero);
        ArgumentNullException.ThrowIfNull(periodos);
        ArgumentException.ThrowIfNullOrWhiteSpace(orgaoEmissor);

        var orgaoNormalizado = orgaoEmissor.Trim();
        if (orgaoNormalizado.Length > ComprimentoMaximoOrgaoEmissor)
        {
            throw new ArgumentException($"Orgao emissor excede {ComprimentoMaximoOrgaoEmissor} caracteres.", nameof(orgaoEmissor));
        }

        var finalidadeNormalizada = NormalizarTexto(finalidadeDescrita, ComprimentoMaximoFinalidadeDescrita, nameof(finalidadeDescrita));
        var observacaoNormalizada = NormalizarTexto(observacao, ComprimentoMaximoObservacao, nameof(observacao));

        // I-1: a certidao precisa de ao menos um periodo computado.
        if (periodos.Count == 0)
        {
            throw new CertidaoTempoServicoException("A certidao exige ao menos um periodo computado.");
        }

        // Ordena por inicio (estabilidade do documento e da deteccao de sobreposicao).
        var ordenados = periodos.OrderBy(periodo => periodo.Inicio).ThenBy(periodo => periodo.Fim).ToList();

        // I-2: VEDACAO de contagem concomitante (art. 96, II, Lei 8.213/1991): nenhum par de periodos pode
        // se sobrepor no tempo. Como estao ordenados por inicio, basta comparar cada periodo com o anterior.
        GarantirSemConcomitancia(ordenados);

        return new CertidaoTempoServico(
            CertidaoTempoServicoId.New(),
            tenantId,
            servidorId,
            numero,
            finalidade,
            dataEmissao,
            orgaoNormalizado,
            finalidadeNormalizada,
            observacaoNormalizada,
            ordenados);
    }

    /// <summary>
    /// Sela o codigo de autenticacao da certidao (digest hexadecimal calculado na borda sobre os campos
    /// estaveis) e emite <see cref="CertidaoTempoServicoEmitida"/>. Chamado UMA vez, na mesma transacao da
    /// emissao — o dominio nao computa hash/IO; recebe o digest pronto.
    /// </summary>
    /// <param name="codigo">Codigo de autenticacao ja construido a partir do digest.</param>
    /// <exception cref="ArgumentNullException">Se o codigo for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se o codigo ja houver sido selado.</exception>
    public void DefinirAutenticacao(CodigoAutenticacao codigo)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        if (CodigoAutenticacao is not null)
        {
            throw new InvalidOperationException("Codigo de autenticacao da certidao ja foi selado.");
        }

        CodigoAutenticacao = codigo;
        RaiseDomainEvent(new CertidaoTempoServicoEmitida(Id, ServidorId, Finalidade, TotalDias, codigo.Valor));
    }

    /// <summary>
    /// Anula a certidao (torna sem efeito; estado terminal). A numeracao consumida e' preservada (I-4 — a
    /// sequencia do exercicio nao retrocede). Emite <see cref="CertidaoTempoServicoAnulada"/>.
    /// </summary>
    /// <param name="motivo">Motivo da anulacao (nao vazio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a certidao ja estiver anulada.</exception>
    public void Anular(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoCertidao.Anulada)
        {
            throw new InvalidOperationException("Certidao ja anulada; estado terminal.");
        }

        Situacao = SituacaoCertidao.Anulada;
        MotivoAnulacao = motivo.Trim();
        RaiseDomainEvent(new CertidaoTempoServicoAnulada(Id, MotivoAnulacao));
    }

    private static void GarantirSemConcomitancia(List<PeriodoTempo> ordenados)
    {
        for (var indice = 1; indice < ordenados.Count; indice++)
        {
            var anterior = ordenados[indice - 1];
            var atual = ordenados[indice];

            // Sobreposicao em intervalos fechados [Inicio, Fim]: o atual inicia em data <= ao fim do anterior.
            if (atual.Inicio <= anterior.Fim)
            {
                throw new CertidaoTempoServicoException(
                    $"Contagem concomitante vedada (art. 96, II, Lei 8.213/1991): os periodos {anterior.Inicio:yyyy-MM-dd}..{anterior.Fim:yyyy-MM-dd} e {atual.Inicio:yyyy-MM-dd}..{atual.Fim:yyyy-MM-dd} se sobrepoem.");
            }
        }
    }

    private static string? NormalizarTexto(string? texto, int limite, string nomeParametro)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var normalizado = texto.Trim();
        if (normalizado.Length > limite)
        {
            throw new ArgumentException($"Texto excede {limite} caracteres.", nomeParametro);
        }

        return normalizado;
    }
}
