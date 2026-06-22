using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;

/// <summary>Identificador forte do agregado <see cref="Familia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FamiliaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FamiliaId"/>.</returns>
    public static FamiliaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situacao (estado) da familia no ciclo de referenciamento do SUAS.</summary>
public enum SituacaoFamilia
{
    /// <summary>Familia referenciada a um CRAS (estado inicial).</summary>
    Referenciada = 1,

    /// <summary>Vigencia cadastral superior a 24 meses; sinalizada para atualizacao.</summary>
    AtualizacaoVencida = 2,

    /// <summary>Cadastro atualizado apos vencimento; apta a novos beneficios.</summary>
    Regularizada = 3,
}

/// <summary>
/// Raiz de agregado que referencia e organiza a familia em situacao de vulnerabilidade/risco no
/// SUAS: calcula a renda per capita, vincula a familia a um territorio/CRAS e organiza a
/// composicao familiar. A base federal (CadUnico/MDS) e autoritativa e nunca e sobrescrita por
/// este agregado (I-9). Marco: CF arts. 203-204; Lei 8.742/1993 (LOAS); PNAS/2004; NOB-SUAS/2012.
/// </summary>
public sealed class Familia : AggregateRoot<FamiliaId>, IMustHaveTenant
{
    /// <summary>Vigencia cadastral do CadUnico em meses (atualizacao obrigatoria a cada 24 meses).</summary>
    public const int MesesValidadeCadastro = 24;

    private readonly List<MembroFamiliar> _membros = [];

    private Familia()
    {
    }

    private Familia(
        FamiliaId id,
        Guid tenantId,
        Nis nis,
        Cpf cpfResponsavel,
        EnderecoTerritorializado endereco,
        Guid unidadeAtendimentoId,
        IReadOnlyCollection<MembroFamiliar> membros,
        DateOnly dataReferenciamento)
        : base(id)
    {
        TenantId = tenantId;
        Nis = nis;
        CpfResponsavel = cpfResponsavel;
        Endereco = endereco;
        UnidadeAtendimentoId = unidadeAtendimentoId;
        Territorio = endereco.Territorio;
        _membros.AddRange(membros);
        RendaPerCapita = CalcularRendaPerCapita();
        DataReferenciamento = dataReferenciamento;
        DataUltimaAtualizacaoCadastral = dataReferenciamento;
        Situacao = SituacaoFamilia.Referenciada;
        RaiseDomainEvent(new FamiliaReferenciada(id, nis.Mascarado, unidadeAtendimentoId, Territorio, dataReferenciamento));
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>NIS do responsavel familiar (chave do CadUnico) — PII redigida na trilha (LG-3).</summary>
    [CampoSensivelLgpd]
    public Nis Nis { get; private set; } = default!;

    /// <summary>CPF do responsavel familiar — PII redigida na trilha (LG-3).</summary>
    [CampoSensivelLgpd]
    public Cpf CpfResponsavel { get; private set; } = default!;

    /// <summary>Endereco com territorio/area de cobertura.</summary>
    public EnderecoTerritorializado Endereco { get; private set; } = default!;

    /// <summary>CRAS de referencia (porta de entrada PSB).</summary>
    public Guid UnidadeAtendimentoId { get; private set; }

    /// <summary>Territorio de cobertura ao qual a familia pertence.</summary>
    public string Territorio { get; private set; } = default!;

    /// <summary>Renda per capita calculada a partir dos membros.</summary>
    public RendaPerCapita RendaPerCapita { get; private set; } = default!;

    /// <summary>Data do referenciamento ao CRAS.</summary>
    public DateOnly DataReferenciamento { get; private set; }

    /// <summary>Marco da ultima atualizacao no CadUnico.</summary>
    public DateOnly DataUltimaAtualizacaoCadastral { get; private set; }

    /// <summary>Situacao atual da familia.</summary>
    public SituacaoFamilia Situacao { get; private set; }

    /// <summary>Composicao familiar (entidades-filhas).</summary>
    public IReadOnlyCollection<MembroFamiliar> Membros => _membros;

    /// <summary>
    /// Referencia uma familia a uma Unidade de Atendimento (CRAS): valida NIS/CPF e a cobertura
    /// territorial, calcula a renda per capita e emite <see cref="FamiliaReferenciada"/> (I-1, I-2, I-8, I-10, I-11).
    /// </summary>
    /// <param name="tenantId">Tenant (municipio) dono do registro.</param>
    /// <param name="nis">NIS valido do responsavel familiar.</param>
    /// <param name="cpfResponsavel">CPF valido do responsavel familiar.</param>
    /// <param name="endereco">Endereco com territorio.</param>
    /// <param name="unidadeAtendimentoId">CRAS de referencia.</param>
    /// <param name="territorioCoberturaCras">Territorio coberto pelo CRAS informado.</param>
    /// <param name="membros">Composicao familiar (ao menos um membro).</param>
    /// <param name="dataReferenciamento">Data do referenciamento.</param>
    /// <returns>Nova <see cref="Familia"/> em situacao <see cref="SituacaoFamilia.Referenciada"/>.</returns>
    /// <exception cref="ArgumentNullException">Se NIS, CPF, endereco ou membros forem nulos (I-1, I-10).</exception>
    /// <exception cref="ArgumentException">Se a composicao familiar estiver vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o endereco estiver fora do territorio do CRAS (I-2) ou houver mais de um RF (I-11).</exception>
    public static Familia Referenciar(
        Guid tenantId,
        Nis nis,
        Cpf cpfResponsavel,
        EnderecoTerritorializado endereco,
        Guid unidadeAtendimentoId,
        string territorioCoberturaCras,
        IReadOnlyCollection<MembroFamiliar> membros,
        DateOnly dataReferenciamento)
    {
        ArgumentNullException.ThrowIfNull(nis);
        ArgumentNullException.ThrowIfNull(cpfResponsavel);
        ArgumentNullException.ThrowIfNull(endereco);
        ArgumentNullException.ThrowIfNull(membros);
        ArgumentException.ThrowIfNullOrWhiteSpace(territorioCoberturaCras);
        GarantirComposicaoValida(membros);

        // I-2: o endereco deve estar dentro do territorio coberto pelo CRAS informado.
        if (!string.Equals(endereco.Territorio, territorioCoberturaCras, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Endereco fora do territorio do CRAS.");
        }

        return new Familia(
            FamiliaId.New(),
            tenantId,
            nis,
            cpfResponsavel,
            endereco,
            unidadeAtendimentoId,
            membros,
            dataReferenciamento);
    }

    /// <summary>
    /// Atualiza a composicao/renda da familia: recalcula a renda per capita, renova o marco
    /// cadastral (I-7) e, se vencida, regulariza a familia (I-12).
    /// </summary>
    /// <param name="membros">Nova composicao familiar (ao menos um membro).</param>
    /// <param name="hoje">Data corrente (renova o marco cadastral).</param>
    /// <exception cref="ArgumentNullException">Se a composicao for nula.</exception>
    /// <exception cref="InvalidOperationException">Se a composicao for vazia (I-4) ou houver mais de um RF (I-11).</exception>
    public void AtualizarRendaFamiliar(IReadOnlyCollection<MembroFamiliar> membros, DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(membros);
        GarantirComposicaoValida(membros);

        _membros.Clear();
        _membros.AddRange(membros);
        RendaPerCapita = CalcularRendaPerCapita();

        // I-7: mudanca de renda/composicao renova o marco cadastral imediatamente.
        DataUltimaAtualizacaoCadastral = hoje;

        // I-12: regularizacao alcancavel pela atualizacao cadastral.
        if (Situacao == SituacaoFamilia.AtualizacaoVencida)
        {
            Situacao = SituacaoFamilia.Regularizada;
        }
    }

    /// <summary>
    /// Processa a vigencia cadastral: se <c>hoje &gt; DataUltimaAtualizacaoCadastral + 24 meses</c>,
    /// passa a familia para <see cref="SituacaoFamilia.AtualizacaoVencida"/> (I-5/I-6). Idempotente.
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    public void ProcessarVigenciaCadastral(DateOnly hoje)
    {
        // I-6/B-6: idempotente — ja vencida nao reemite transicao.
        if (Situacao == SituacaoFamilia.AtualizacaoVencida)
        {
            return;
        }

        // I-5/B-5: estritamente maior — no marco exato a vigencia ainda e valida.
        var limite = DataUltimaAtualizacaoCadastral.AddMonths(MesesValidadeCadastro);
        if (hoje > limite)
        {
            Situacao = SituacaoFamilia.AtualizacaoVencida;
        }
    }

    /// <summary>
    /// Regulariza o cadastro vencido mediante atualizacao cadastral (I-12). So transita de
    /// <see cref="SituacaoFamilia.AtualizacaoVencida"/> para <see cref="SituacaoFamilia.Regularizada"/>.
    /// </summary>
    /// <param name="hoje">Data corrente da regularizacao (renova o marco cadastral).</param>
    /// <exception cref="InvalidOperationException">Se a familia nao estiver com cadastro vencido.</exception>
    public void RegularizarCadastro(DateOnly hoje)
    {
        if (Situacao != SituacaoFamilia.AtualizacaoVencida)
        {
            throw new InvalidOperationException($"So e possivel regularizar familia com cadastro vencido. Situacao atual: {Situacao}.");
        }

        DataUltimaAtualizacaoCadastral = hoje;
        Situacao = SituacaoFamilia.Regularizada;
    }

    /// <summary>Indica se a vigencia cadastral esta vencida na data informada (I-5).</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se vencida (estritamente apos o marco + 24 meses).</returns>
    public bool VigenciaVencida(DateOnly hoje)
        => hoje > DataUltimaAtualizacaoCadastral.AddMonths(MesesValidadeCadastro);

    /// <summary>
    /// Indica se a familia esta apta a novos beneficios: situacao em
    /// { <see cref="SituacaoFamilia.Referenciada"/>, <see cref="SituacaoFamilia.Regularizada"/> }
    /// e cadastro dentro da vigencia (I-6).
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se apta.</returns>
    public bool EstaAptaANovosBeneficios(DateOnly hoje)
        => Situacao is SituacaoFamilia.Referenciada or SituacaoFamilia.Regularizada
        && !VigenciaVencida(hoje);

    private RendaPerCapita CalcularRendaPerCapita()
    {
        // I-3/I-4: soma das rendas individuais dividida pelo numero de membros (sempre maior ou igual a 1).
        var rendaFamiliar = _membros.Aggregate(0m, static (acumulado, membro) => acumulado + membro.RendaIndividual.Valor);
        return RendaPerCapita.Calcular(rendaFamiliar, _membros.Count);
    }

    private static void GarantirComposicaoValida(IReadOnlyCollection<MembroFamiliar> membros)
    {
        // I-4/B-2: divisao por zero proibida — ao menos um membro.
        if (membros.Count == 0)
        {
            throw new InvalidOperationException("A familia deve ter ao menos um membro.");
        }

        // I-11/B-4: no maximo um Responsavel Familiar (RF) por familia.
        var responsaveis = membros.Count(membro => membro.Parentesco == Parentesco.ResponsavelFamiliar);
        if (responsaveis > 1)
        {
            throw new InvalidOperationException("A familia deve ter no maximo um Responsavel Familiar.");
        }
    }
}
