using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;

/// <summary>Identificador forte do agregado <see cref="ObraContribuicaoMelhoria"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ObraContribuicaoMelhoriaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ObraContribuicaoMelhoriaId"/>.</returns>
    public static ObraContribuicaoMelhoriaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Estado do processo da obra de Contribuição de Melhoria (CTN art. 82): exige edital prévio e prazo de
/// impugnação ≥ 30 dias ANTES do rateio/lançamento.
/// </summary>
public enum EstadoObraMelhoria
{
    /// <summary>Edital publicado; prazo de impugnação em curso (≥ 30 dias).</summary>
    EditalPublicado = 1,

    /// <summary>Prazo de impugnação encerrado; apto a ratear e lançar.</summary>
    ImpugnacaoEncerrada = 2,

    /// <summary>Rateio efetuado e lançamentos gerados por imóvel.</summary>
    Rateada = 3,

    /// <summary>Obra/processo cancelado.</summary>
    Cancelada = 4,
}

/// <summary>
/// Obra pública sujeita a Contribuição de Melhoria (CTN arts. 81–82 + DL 195/1967). O fato gerador é a
/// VALORIZAÇÃO imobiliária decorrente da obra (não a obra). Modela o EDITAL prévio obrigatório (memorial,
/// custo, zona, fator de absorção, parcela a financiar), o prazo de IMPUGNAÇÃO ≥ 30 dias (não opcional) e
/// o RATEIO proporcional à valorização individual, respeitando os DOIS LIMITES: total ≤ parcela do custo
/// a financiar; individual ≤ valorização de cada imóvel. Gera um lançamento por imóvel (à parte). Nenhum
/// valor é hardcoded — tudo vem do edital/lei específica da obra. Ver M6-DESIGN §3.5.
/// </summary>
public sealed class ObraContribuicaoMelhoria : AggregateRoot<ObraContribuicaoMelhoriaId>, IMustHaveTenant
{
    /// <summary>Prazo mínimo legal de impugnação ao edital (CTN art. 82, II): 30 dias.</summary>
    public const int PrazoMinimoImpugnacaoDias = 30;

    private readonly List<ImovelBeneficiado> _imoveis = [];

    private ObraContribuicaoMelhoria()
    {
    }

    private ObraContribuicaoMelhoria(
        ObraContribuicaoMelhoriaId id,
        Guid tenantId,
        string identificacaoObra,
        string memorialDescritivo,
        ValorMonetario custoTotalObra,
        decimal parcelaCustoFinanciadaPercentual,
        string zonaBeneficiada,
        decimal fatorAbsorcaoPercentual,
        DateOnly dataPublicacaoEdital,
        DateOnly fimPrazoImpugnacao,
        string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        IdentificacaoObra = identificacaoObra;
        MemorialDescritivo = memorialDescritivo;
        CustoTotalObra = custoTotalObra;
        ParcelaCustoFinanciadaPercentual = parcelaCustoFinanciadaPercentual;
        ZonaBeneficiada = zonaBeneficiada;
        FatorAbsorcaoPercentual = fatorAbsorcaoPercentual;
        DataPublicacaoEdital = dataPublicacaoEdital;
        FimPrazoImpugnacao = fimPrazoImpugnacao;
        FundamentoLegal = fundamentoLegal;
        Estado = EstadoObraMelhoria.EditalPublicado;
        RaiseDomainEvent(new ObraMelhoriaEditalPublicado(id, tenantId, identificacaoObra));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificação da obra (nº/nome).</summary>
    public string IdentificacaoObra { get; private set; } = default!;

    /// <summary>Memorial descritivo do projeto (edital — CTN art. 82, I).</summary>
    public string MemorialDescritivo { get; private set; } = default!;

    /// <summary>Custo total orçado da obra (limite TOTAL da contribuição — CTN art. 81).</summary>
    public ValorMonetario CustoTotalObra { get; private set; } = default!;

    /// <summary>
    /// Percentual do custo da obra a ser financiado pela contribuição (parcela definida no edital —
    /// CTN art. 82, I). O teto da arrecadação é esse percentual do custo total.
    /// </summary>
    public decimal ParcelaCustoFinanciadaPercentual { get; private set; }

    /// <summary>Zona beneficiada (delimitação do edital — CTN art. 82, I).</summary>
    public string ZonaBeneficiada { get; private set; } = default!;

    /// <summary>Fator de absorção do benefício da valorização (edital — CTN art. 82, I), em %.</summary>
    public decimal FatorAbsorcaoPercentual { get; private set; }

    /// <summary>Data de publicação do edital.</summary>
    public DateOnly DataPublicacaoEdital { get; private set; }

    /// <summary>Fim do prazo de impugnação (≥ 30 dias após a publicação — CTN art. 82, II).</summary>
    public DateOnly FimPrazoImpugnacao { get; private set; }

    /// <summary>
    /// Fundamento legal (lei específica da obra + CTM). // TODO(validar-oficial): lei específica da obra
    /// no município de Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Estado do processo.</summary>
    public EstadoObraMelhoria Estado { get; private set; }

    /// <summary>Imóveis beneficiados (com valorização individual e parcela rateada).</summary>
    public IReadOnlyList<ImovelBeneficiado> Imoveis => _imoveis;

    /// <summary>Limite total da contribuição = custo total × percentual a financiar (CTN art. 81).</summary>
    public ValorMonetario LimiteTotal
        => ValorMonetario.De(decimal.Round(CustoTotalObra.Valor * ParcelaCustoFinanciadaPercentual / 100m, 2, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Publica o edital da obra (CTN art. 82): início obrigatório do processo. O prazo de impugnação deve
    /// ser de no mínimo 30 dias. Nenhum lançamento ocorre antes do encerramento do prazo.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="identificacaoObra">Identificação da obra.</param>
    /// <param name="memorialDescritivo">Memorial descritivo do projeto.</param>
    /// <param name="custoTotalObra">Custo total orçado.</param>
    /// <param name="parcelaCustoFinanciadaPercentual">Percentual do custo a financiar (0 a 100).</param>
    /// <param name="zonaBeneficiada">Zona beneficiada.</param>
    /// <param name="fatorAbsorcaoPercentual">Fator de absorção do benefício (0 a 100).</param>
    /// <param name="dataPublicacaoEdital">Data de publicação do edital.</param>
    /// <param name="fimPrazoImpugnacao">Fim do prazo de impugnação (≥ 30 dias após a publicação).</param>
    /// <param name="fundamentoLegal">Lei específica da obra + CTM.</param>
    /// <returns>Nova <see cref="ObraContribuicaoMelhoria"/> com edital publicado.</returns>
    /// <exception cref="ArgumentException">Se o prazo de impugnação for inferior a 30 dias.</exception>
    public static ObraContribuicaoMelhoria PublicarEdital(
        Guid tenantId,
        string identificacaoObra,
        string memorialDescritivo,
        ValorMonetario custoTotalObra,
        decimal parcelaCustoFinanciadaPercentual,
        string zonaBeneficiada,
        decimal fatorAbsorcaoPercentual,
        DateOnly dataPublicacaoEdital,
        DateOnly fimPrazoImpugnacao,
        string fundamentoLegal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificacaoObra);
        ArgumentException.ThrowIfNullOrWhiteSpace(memorialDescritivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(zonaBeneficiada);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        ArgumentNullException.ThrowIfNull(custoTotalObra);
        if (parcelaCustoFinanciadaPercentual is <= 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(parcelaCustoFinanciadaPercentual), parcelaCustoFinanciadaPercentual, "A parcela do custo a financiar deve estar em (0, 100].");
        }

        if (fatorAbsorcaoPercentual is <= 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(fatorAbsorcaoPercentual), fatorAbsorcaoPercentual, "O fator de absorção deve estar em (0, 100].");
        }

        // CTN art. 82, II: prazo de impugnação NÃO inferior a 30 dias — invariante de domínio, não opcional.
        var diasDeImpugnacao = fimPrazoImpugnacao.DayNumber - dataPublicacaoEdital.DayNumber;
        if (diasDeImpugnacao < PrazoMinimoImpugnacaoDias)
        {
            throw new ArgumentException($"O prazo de impugnação deve ser de no mínimo {PrazoMinimoImpugnacaoDias} dias (CTN art. 82, II).", nameof(fimPrazoImpugnacao));
        }

        return new ObraContribuicaoMelhoria(
            ObraContribuicaoMelhoriaId.New(),
            tenantId,
            identificacaoObra.Trim(),
            memorialDescritivo.Trim(),
            custoTotalObra,
            parcelaCustoFinanciadaPercentual,
            zonaBeneficiada.Trim(),
            fatorAbsorcaoPercentual,
            dataPublicacaoEdital,
            fimPrazoImpugnacao,
            fundamentoLegal.Trim());
    }

    /// <summary>
    /// Adiciona um imóvel beneficiado com a valorização individual apurada. Só durante a fase de edital
    /// (antes do rateio).
    /// </summary>
    /// <param name="imovelId">Imóvel beneficiado.</param>
    /// <param name="proprietarioId">Contribuinte proprietário.</param>
    /// <param name="valorizacaoIndividual">Valorização individual apurada.</param>
    /// <exception cref="InvalidOperationException">Se já rateada/cancelada ou o imóvel já constar.</exception>
    public void AdicionarImovelBeneficiado(ImovelId imovelId, ContribuinteId proprietarioId, ValorMonetario valorizacaoIndividual)
    {
        ArgumentNullException.ThrowIfNull(valorizacaoIndividual);
        if (Estado is EstadoObraMelhoria.Rateada or EstadoObraMelhoria.Cancelada)
        {
            throw new InvalidOperationException("Não é possível adicionar imóveis a uma obra já rateada ou cancelada.");
        }

        if (_imoveis.Any(i => i.ImovelId == imovelId))
        {
            throw new InvalidOperationException("O imóvel já consta entre os beneficiados desta obra.");
        }

        _imoveis.Add(ImovelBeneficiado.Criar(Id, imovelId, proprietarioId, valorizacaoIndividual));
    }

    /// <summary>
    /// Encerra o prazo de impugnação (CTN art. 82): só a partir da data de fim do prazo. Habilita o rateio.
    /// </summary>
    /// <param name="hoje">Data de referência.</param>
    /// <exception cref="InvalidOperationException">Se não estiver com edital publicado ou o prazo não tiver decorrido.</exception>
    public void EncerrarPrazoImpugnacao(DateOnly hoje)
    {
        if (Estado != EstadoObraMelhoria.EditalPublicado)
        {
            throw new InvalidOperationException($"O prazo de impugnação só pode ser encerrado no estado EditalPublicado. Estado atual: {Estado}.");
        }

        if (hoje < FimPrazoImpugnacao)
        {
            throw new InvalidOperationException("O prazo de impugnação ainda não decorreu (CTN art. 82, II).");
        }

        Estado = EstadoObraMelhoria.ImpugnacaoEncerrada;
        RaiseDomainEvent(new ObraMelhoriaImpugnacaoEncerrada(Id, TenantId));
    }

    /// <summary>
    /// Rateia a contribuição entre os imóveis beneficiados, PROPORCIONALMENTE à valorização individual,
    /// respeitando os dois limites (CTN art. 81): a soma rateada ≤ limite total; e a parcela de cada
    /// imóvel ≤ sua própria valorização. O resíduo de arredondamento é absorvido pelo último imóvel.
    /// Só após o encerramento do prazo de impugnação. Determinístico e auditável.
    /// </summary>
    /// <returns>O valor total efetivamente rateado.</returns>
    /// <exception cref="InvalidOperationException">Se não estiver com impugnação encerrada ou sem imóveis.</exception>
    public ValorMonetario Ratear()
    {
        if (Estado != EstadoObraMelhoria.ImpugnacaoEncerrada)
        {
            throw new InvalidOperationException($"O rateio só pode ocorrer após o encerramento do prazo de impugnação. Estado atual: {Estado}.");
        }

        if (_imoveis.Count == 0)
        {
            throw new InvalidOperationException("Não há imóveis beneficiados para ratear.");
        }

        var somaValorizacoes = _imoveis.Sum(i => i.ValorizacaoIndividual.Valor);
        if (somaValorizacoes <= 0m)
        {
            throw new InvalidOperationException("A soma das valorizações individuais deve ser positiva para o rateio.");
        }

        var limiteTotal = LimiteTotal.Valor;

        // O total a ratear é o MENOR entre o limite total (custo financiável) e a soma das valorizações
        // (limite individual agregado) — assim nenhum dos dois limites do CTN art. 81 é ultrapassado.
        var totalARatear = Math.Min(limiteTotal, somaValorizacoes);

        var acumulado = 0m;
        for (var indice = 0; indice < _imoveis.Count; indice++)
        {
            var imovel = _imoveis[indice];
            decimal parcela;
            if (indice == _imoveis.Count - 1)
            {
                // Último imóvel absorve o resíduo (garante soma exata = totalARatear).
                parcela = totalARatear - acumulado;
            }
            else
            {
                parcela = decimal.Round(totalARatear * imovel.ValorizacaoIndividual.Valor / somaValorizacoes, 2, MidpointRounding.AwayFromZero);
                acumulado += parcela;
            }

            // Limite individual (CTN art. 81): a parcela nunca pode exceder a valorização do imóvel.
            parcela = Math.Min(parcela, imovel.ValorizacaoIndividual.Valor);
            imovel.DefinirContribuicaoRateada(ValorMonetario.De(parcela));
        }

        Estado = EstadoObraMelhoria.Rateada;
        var totalRateado = ValorMonetario.De(_imoveis.Sum(i => i.ContribuicaoRateada.Valor));
        RaiseDomainEvent(new ObraMelhoriaRateada(Id, TenantId, totalRateado.Valor));
        return totalRateado;
    }

    /// <summary>Cancela a obra/processo (vedado após o rateio).</summary>
    /// <exception cref="InvalidOperationException">Se já rateada ou cancelada.</exception>
    public void Cancelar()
    {
        if (Estado is EstadoObraMelhoria.Rateada or EstadoObraMelhoria.Cancelada)
        {
            throw new InvalidOperationException($"Não é possível cancelar uma obra no estado {Estado}.");
        }

        Estado = EstadoObraMelhoria.Cancelada;
        RaiseDomainEvent(new ObraMelhoriaCancelada(Id, TenantId));
    }
}
