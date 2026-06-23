using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

/// <summary>Identificador forte do agregado <see cref="Beneficio"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct BeneficioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="BeneficioId"/>.</returns>
    public static BeneficioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Raiz de agregado que avalia a elegibilidade e registra a concessao ou o indeferimento de
/// beneficios socioassistenciais (BPC / PBF / eventuais). Os criterios de renda e o salario
/// minimo sao versionados por vigencia/competencia (nunca hardcoded — Beneficio I-1).
/// CF arts. 203-204; Lei 8.742/1993 (LOAS); Lei 14.601/2023 (PBF).
/// </summary>
public sealed class Beneficio : AggregateRoot<BeneficioId>, IMustHaveTenant
{
    private Beneficio()
    {
    }

    private Beneficio(
        BeneficioId id,
        Guid tenantId,
        Guid familiaId,
        TipoBeneficio tipo,
        Competencia competencia)
        : base(id)
    {
        TenantId = tenantId;
        FamiliaId = familiaId;
        Tipo = tipo;
        Competencia = competencia;
        Situacao = SituacaoBeneficio.EmAvaliacao;
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Familia beneficiaria (vinculo com o agregado Familia).</summary>
    public Guid FamiliaId { get; private set; }

    /// <summary>Tipo do beneficio (BPC / PBF / Eventual).</summary>
    public TipoBeneficio Tipo { get; private set; }

    /// <summary>Competencia (ano/mes) de referencia — define a regra vigente.</summary>
    public Competencia Competencia { get; private set; }

    /// <summary>Valor concedido (nulo ate a concessao; nulo em cesta basica).</summary>
    public ValorMonetario? Valor { get; private set; }

    /// <summary>Situacao atual no ciclo de avaliacao/concessao.</summary>
    public SituacaoBeneficio Situacao { get; private set; }

    /// <summary>Motivo da negativa (preenchido quando <see cref="SituacaoBeneficio.Indeferida"/>).</summary>
    public string? MotivoIndeferimento { get; private set; }

    /// <summary>Data da concessao/indeferimento.</summary>
    public DateOnly? DataDecisao { get; private set; }

    /// <summary>Quantidade de cestas (apenas eventual de cesta basica).</summary>
    public int? QuantidadeCesta { get; private set; }

    /// <summary>Data de entrega da cesta.</summary>
    public DateOnly? DataEntregaCesta { get; private set; }

    /// <summary>Indica se o beneficio ja foi decidido (terminal) — Beneficio I-5.</summary>
    public bool EstaDecidido => Situacao is SituacaoBeneficio.Concedida or SituacaoBeneficio.Indeferida;

    /// <summary>Solicita um beneficio, que nasce em avaliacao (Beneficio I-11).</summary>
    /// <param name="tenantId">Tenant (municipio) dono do registro.</param>
    /// <param name="familiaId">Familia requerente (obrigatoria).</param>
    /// <param name="tipo">Tipo do beneficio.</param>
    /// <param name="competencia">Competencia de referencia (deve ser valida).</param>
    /// <returns>Novo <see cref="Beneficio"/> em <see cref="SituacaoBeneficio.EmAvaliacao"/>.</returns>
    /// <exception cref="ArgumentException">Se a familia for vazia (I-11) ou a competencia for invalida (I-1).</exception>
    public static Beneficio Solicitar(
        Guid tenantId,
        Guid familiaId,
        TipoBeneficio tipo,
        Competencia competencia)
    {
        if (familiaId == Guid.Empty)
        {
            throw new ArgumentException("Familia e obrigatoria.", nameof(familiaId));
        }

        if (!competencia.EhValida())
        {
            throw new ArgumentException("Competencia (ano/mes) e obrigatoria e valida.", nameof(competencia));
        }

        return new Beneficio(BeneficioId.New(), tenantId, familiaId, tipo, competencia);
    }

    /// <summary>
    /// Avalia a elegibilidade conforme o criterio vigente e despacha para concessao ou indeferimento
    /// (Beneficio I-2/I-3/I-4). Operacao terminal: so a partir de <see cref="SituacaoBeneficio.EmAvaliacao"/> (I-5).
    /// </summary>
    /// <param name="criterio">Criterio vigente na competencia (recebe o salario minimo vigente).</param>
    /// <param name="dados">Contexto fatico do requerente.</param>
    /// <param name="rendaPerCapita">Renda per capita apurada da familia.</param>
    /// <param name="valorConcedido">Valor a conceder quando elegivel (nulo em cesta basica).</param>
    /// <param name="dataDecisao">Data da decisao.</param>
    /// <returns>Resultado da avaliacao aplicado ao beneficio.</returns>
    /// <exception cref="ArgumentNullException">Se o criterio ou a renda forem nulos.</exception>
    /// <exception cref="InvalidOperationException">Se o beneficio ja estiver decidido (I-5).</exception>
    public ResultadoElegibilidade AvaliarElegibilidade(
        CriterioElegibilidade criterio,
        DadosElegibilidade dados,
        RendaPerCapita rendaPerCapita,
        ValorMonetario? valorConcedido,
        DateOnly dataDecisao)
    {
        ArgumentNullException.ThrowIfNull(criterio);
        ArgumentNullException.ThrowIfNull(rendaPerCapita);
        GarantirEmAvaliacao();

        var resultado = criterio.Avaliar(dados, rendaPerCapita);
        if (resultado.Elegivel)
        {
            Conceder(valorConcedido, dataDecisao);
        }
        else
        {
            Indeferir(resultado.Motivo, dataDecisao);
        }

        return resultado;
    }

    /// <summary>
    /// <b>A-0:</b> avalia a elegibilidade de um BENEFICIO EVENTUAL pelo criterio de LEI MUNICIPAL
    /// (<see cref="CriterioBeneficioEventual"/>) e despacha para concessao ou indeferimento. NAO ha teto
    /// federal de "1/4 SM" (revogado pela Lei 12.435/2011): o corte de renda — quando existe — vem da lei
    /// municipal, podendo ser superior a 1/4 SM. Operacao terminal: so a partir de
    /// <see cref="SituacaoBeneficio.EmAvaliacao"/> (I-5); exige <see cref="TipoBeneficio.Eventual"/>.
    /// </summary>
    /// <param name="criterioMunicipal">Criterio municipal vigente (modalidade + corte de renda em SM, ou sem corte).</param>
    /// <param name="rendaPerCapita">Renda per capita apurada da familia.</param>
    /// <param name="salarioMinimoVigente">Salario minimo vigente na competencia (parametro versionado).</param>
    /// <param name="valorConcedido">Valor a conceder quando elegivel (nulo em provisao em especie/cesta).</param>
    /// <param name="dataDecisao">Data da decisao.</param>
    /// <returns>Resultado da avaliacao aplicado ao beneficio.</returns>
    /// <exception cref="ArgumentNullException">Se o criterio, a renda ou o salario minimo forem nulos.</exception>
    /// <exception cref="InvalidOperationException">Se o beneficio nao for eventual ou ja estiver decidido (I-5).</exception>
    public ResultadoElegibilidade AvaliarElegibilidadeEventual(
        CriterioBeneficioEventual criterioMunicipal,
        RendaPerCapita rendaPerCapita,
        ValorMonetario salarioMinimoVigente,
        ValorMonetario? valorConcedido,
        DateOnly dataDecisao)
    {
        ArgumentNullException.ThrowIfNull(criterioMunicipal);
        ArgumentNullException.ThrowIfNull(rendaPerCapita);
        ArgumentNullException.ThrowIfNull(salarioMinimoVigente);
        GarantirEmAvaliacao();

        if (Tipo != TipoBeneficio.Eventual)
        {
            throw new InvalidOperationException($"Avaliacao por criterio municipal exige beneficio eventual. Tipo atual: {Tipo}.");
        }

        var resultado = criterioMunicipal.Avaliar(rendaPerCapita, salarioMinimoVigente);
        if (resultado.Elegivel)
        {
            Conceder(valorConcedido, dataDecisao);
        }
        else
        {
            Indeferir(resultado.Motivo, dataDecisao);
        }

        return resultado;
    }

    /// <summary>Concede o beneficio (Beneficio I-6). Acionado por <see cref="AvaliarElegibilidade"/>.</summary>
    /// <param name="valor">Valor concedido (nulo em cesta basica — I-9).</param>
    /// <param name="dataDecisao">Data da concessao.</param>
    /// <exception cref="InvalidOperationException">Se o beneficio ja estiver decidido (I-5).</exception>
    public void Conceder(ValorMonetario? valor, DateOnly dataDecisao)
    {
        GarantirEmAvaliacao();

        Valor = valor;
        DataDecisao = dataDecisao;
        Situacao = SituacaoBeneficio.Concedida;
        RaiseDomainEvent(new BeneficioConcedido(Id, FamiliaId, Tipo, Competencia, valor?.Valor));
    }

    /// <summary>Indefere o beneficio com motivo fundamentado (Beneficio I-7).</summary>
    /// <param name="motivoIndeferimento">Motivo da negativa (nao vazio).</param>
    /// <param name="dataDecisao">Data do indeferimento.</param>
    /// <exception cref="ArgumentException">Se o motivo for nulo/vazio (I-7/B-5).</exception>
    /// <exception cref="InvalidOperationException">Se o beneficio ja estiver decidido (I-5).</exception>
    public void Indeferir(string motivoIndeferimento, DateOnly dataDecisao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoIndeferimento);
        GarantirEmAvaliacao();

        MotivoIndeferimento = motivoIndeferimento;
        DataDecisao = dataDecisao;
        Situacao = SituacaoBeneficio.Indeferida;
        RaiseDomainEvent(new BeneficioIndeferido(Id, FamiliaId, Tipo, motivoIndeferimento));
    }

    /// <summary>
    /// Registra a entrega de cesta basica sobre beneficio eventual concedido (Beneficio I-8).
    /// Nao altera a situacao (permanece <see cref="SituacaoBeneficio.Concedida"/>).
    /// </summary>
    /// <param name="quantidade">Quantidade de cestas (&gt;= 1).</param>
    /// <param name="dataEntrega">Data da entrega.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade for menor que 1 (B-7).</exception>
    /// <exception cref="InvalidOperationException">Se o beneficio nao estiver concedido ou nao for eventual (I-8).</exception>
    public void EntregarCestaBasica(int quantidade, DateOnly dataEntrega)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantidade, 1);

        if (Situacao != SituacaoBeneficio.Concedida)
        {
            throw new InvalidOperationException($"A entrega de cesta exige beneficio concedido. Situacao atual: {Situacao}.");
        }

        if (Tipo != TipoBeneficio.Eventual)
        {
            throw new InvalidOperationException($"A entrega de cesta so e permitida em beneficio eventual. Tipo atual: {Tipo}.");
        }

        QuantidadeCesta = quantidade;
        DataEntregaCesta = dataEntrega;
        RaiseDomainEvent(new CestaBasicaEntregue(Id, FamiliaId, quantidade, dataEntrega));
    }

    private void GarantirEmAvaliacao()
    {
        if (EstaDecidido)
        {
            throw new InvalidOperationException($"Beneficio ja decidido nao admite nova decisao. Situacao atual: {Situacao}.");
        }
    }
}
