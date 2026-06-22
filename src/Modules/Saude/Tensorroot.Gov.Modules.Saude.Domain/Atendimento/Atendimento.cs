using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>Identificador forte do agregado <see cref="Atendimento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AtendimentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AtendimentoId"/>.</returns>
    public static AtendimentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro clinico de um encontro assistencial (consulta/atendimento na APS) de um Paciente em um
/// Estabelecimento, com evolucao SOAP, prescricoes e solicitacoes de exame. E append-only: evolucao
/// assinada nao se edita — apenas adendo datado/reassinado. Apos assinado (NGS2/ICP-Brasil) torna-se
/// imutavel e pode ser compartilhado na RNDS (FHIR R4) e lancado no SISAB. Trata dado sensivel (LGPD art. 11).
/// </summary>
public sealed class Atendimento : AggregateRoot<AtendimentoId>, IMustHaveTenant
{
    private readonly List<EvolucaoSOAP> _evolucoes = [];
    private readonly List<Prescricao> _prescricoes = [];
    private readonly List<SolicitacaoExame> _solicitacoesExame = [];

    private Atendimento()
    {
    }

    private Atendimento(
        AtendimentoId id,
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoId,
        ProfissionalId profissionalId,
        DateTimeOffset dataHora,
        Competencia competencia,
        ModalidadeAtendimento modalidade)
        : base(id)
    {
        TenantId = tenantId;
        PacienteId = pacienteId;
        EstabelecimentoId = estabelecimentoId;
        ProfissionalId = profissionalId;
        DataHora = dataHora;
        Competencia = competencia;
        Modalidade = modalidade;
        NivelGarantia = NivelGarantia.NGS1;
        Situacao = SituacaoAtendimento.EmAndamento;
        RaiseDomainEvent(new AtendimentoRegistrado(id, pacienteId, estabelecimentoId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Paciente atendido (referencia por Id).</summary>
    public PacienteId PacienteId { get; private set; }

    /// <summary>Estabelecimento (CNES) do atendimento (referencia por Id).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Profissional responsavel (referencia por Id).</summary>
    public ProfissionalId ProfissionalId { get; private set; }

    /// <summary>Data/hora do atendimento.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Competencia (AAAA-MM) para o SISAB.</summary>
    public Competencia Competencia { get; private set; }

    /// <summary>Modalidade (Presencial ou Teleconsulta).</summary>
    public ModalidadeAtendimento Modalidade { get; private set; }

    /// <summary>Diagnostico CID-10 (clinico), quando informado.</summary>
    public Cid? Cid { get; private set; }

    /// <summary>Diagnostico CIAP-2 (APS), quando informado.</summary>
    public Ciap? Ciap { get; private set; }

    /// <summary>Nivel de garantia (NGS1/NGS2). NGS2 e exigido para eliminar papel e compartilhar na RNDS.</summary>
    public NivelGarantia NivelGarantia { get; private set; }

    /// <summary>Assinatura ICP-Brasil (nula ate assinar).</summary>
    public AssinaturaDigital? Assinatura { get; private set; }

    /// <summary>Protocolo de aceite da RNDS (nulo ate compartilhar).</summary>
    public string? ProtocoloRnds { get; private set; }

    /// <summary>Situacao atual no ciclo clinico.</summary>
    public SituacaoAtendimento Situacao { get; private set; }

    /// <summary>Notas SOAP e adendos (append-only).</summary>
    public IReadOnlyCollection<EvolucaoSOAP> Evolucoes => _evolucoes;

    /// <summary>Prescricoes do atendimento.</summary>
    public IReadOnlyCollection<Prescricao> Prescricoes => _prescricoes;

    /// <summary>Solicitacoes de exame do atendimento.</summary>
    public IReadOnlyCollection<SolicitacaoExame> SolicitacoesExame => _solicitacoesExame;

    /// <summary>
    /// Abre o registro clinico de um encontro (situacao inicial <see cref="SituacaoAtendimento.EmAndamento"/>).
    /// As pre-condicoes de CNS confirmado, CNES/CBO ativos e CRM (teleconsulta) sao verificadas na orquestracao (handler/ACL) — I-1/I-9.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="pacienteId">Paciente atendido.</param>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="profissionalId">Profissional responsavel.</param>
    /// <param name="dataHora">Data/hora do atendimento.</param>
    /// <param name="competencia">Competencia (AAAA-MM).</param>
    /// <param name="modalidade">Modalidade (presencial/teleconsulta).</param>
    /// <returns>Novo <see cref="Atendimento"/> em andamento.</returns>
    /// <exception cref="ArgumentException">Se algum identificador de referencia for vazio.</exception>
    public static Atendimento Registrar(
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoId,
        ProfissionalId profissionalId,
        DateTimeOffset dataHora,
        Competencia competencia,
        ModalidadeAtendimento modalidade)
    {
        if (pacienteId.Value == Guid.Empty)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(pacienteId));
        }

        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento (CNES) e obrigatorio.", nameof(estabelecimentoId));
        }

        if (profissionalId.Value == Guid.Empty)
        {
            throw new ArgumentException("Profissional e obrigatorio.", nameof(profissionalId));
        }

        return new Atendimento(
            AtendimentoId.New(),
            tenantId,
            pacienteId,
            estabelecimentoId,
            profissionalId,
            dataHora,
            competencia,
            modalidade);
    }

    /// <summary>Adiciona uma nota SOAP (atendimento em andamento) — I-8.</summary>
    /// <param name="subjetivo">Componente Subjetivo.</param>
    /// <param name="objetivo">Componente Objetivo.</param>
    /// <param name="avaliacao">Componente Avaliacao.</param>
    /// <param name="plano">Componente Plano.</param>
    /// <param name="dataHora">Data/hora do registro.</param>
    /// <param name="cid">Diagnostico CID-10 (opcional).</param>
    /// <param name="ciap">Diagnostico CIAP-2 (opcional).</param>
    /// <exception cref="ArgumentException">Se todos os campos SOAP estiverem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for EmAndamento.</exception>
    public void AdicionarEvolucaoSOAP(
        string? subjetivo,
        string? objetivo,
        string? avaliacao,
        string? plano,
        DateTimeOffset dataHora,
        Cid? cid = null,
        Ciap? ciap = null)
    {
        GarantirEmAndamento();
        var evolucao = EvolucaoSOAP.Registrar(subjetivo, objetivo, avaliacao, plano, dataHora);
        _evolucoes.Add(evolucao);
        if (cid is not null)
        {
            Cid = cid;
        }

        if (ciap is not null)
        {
            Ciap = ciap;
        }
    }

    /// <summary>Adiciona uma prescricao (atendimento em andamento) — I-7.</summary>
    /// <param name="item">Item prescrito.</param>
    /// <param name="posologia">Posologia.</param>
    /// <param name="dataHora">Data/hora.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for EmAndamento.</exception>
    public void AdicionarPrescricao(string item, string posologia, DateTimeOffset dataHora)
    {
        GarantirEmAndamento();
        _prescricoes.Add(Prescricao.Registrar(item, posologia, dataHora));
    }

    /// <summary>Adiciona uma solicitacao de exame (atendimento em andamento) — I-7.</summary>
    /// <param name="procedimento">Procedimento/exame.</param>
    /// <param name="justificativa">Justificativa clinica.</param>
    /// <param name="dataHora">Data/hora.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for EmAndamento.</exception>
    public void AdicionarSolicitacaoExame(string procedimento, string justificativa, DateTimeOffset dataHora)
    {
        GarantirEmAndamento();
        _solicitacoesExame.Add(SolicitacaoExame.Registrar(procedimento, justificativa, dataHora));
    }

    /// <summary>
    /// Assina o atendimento com assinatura ICP-Brasil (NGS2): exige situacao EmAndamento e ao menos uma
    /// evolucao. Marca todas as evolucoes correntes como assinadas e torna o registro imutavel — I-3.
    /// </summary>
    /// <param name="assinatura">Assinatura digital ICP-Brasil (NGS2).</param>
    /// <exception cref="ArgumentNullException">Se a assinatura for nula.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver EmAndamento, sem evolucao, ou a assinatura nao for NGS2.</exception>
    public void Assinar(AssinaturaDigital assinatura)
    {
        ArgumentNullException.ThrowIfNull(assinatura);
        if (Situacao != SituacaoAtendimento.EmAndamento)
        {
            throw new InvalidOperationException($"Assinatura exige situacao EmAndamento. Situacao atual: {Situacao}.");
        }

        if (_evolucoes.Count == 0)
        {
            throw new InvalidOperationException("Assinatura requer ao menos uma evolucao SOAP.");
        }

        if (assinatura.Nivel != NivelGarantia.NGS2)
        {
            throw new InvalidOperationException("A assinatura do atendimento exige NGS2 (ICP-Brasil).");
        }

        Assinatura = assinatura;
        NivelGarantia = NivelGarantia.NGS2;
        foreach (var evolucao in _evolucoes)
        {
            evolucao.MarcarAssinada();
        }

        Situacao = SituacaoAtendimento.Assinado;
        RaiseDomainEvent(new AtendimentoAssinado(Id));
    }

    /// <summary>
    /// Acrescenta um adendo datado/reassinado a uma evolucao ja assinada, sem editar a original (append-only) — I-2.
    /// Permitido apenas com situacao Assinado/Compartilhado.
    /// </summary>
    /// <param name="evolucaoReferenciadaId">Evolucao assinada a complementar.</param>
    /// <param name="texto">Texto do adendo.</param>
    /// <param name="assinatura">Assinatura ICP-Brasil do adendo.</param>
    /// <param name="dataHora">Data/hora do adendo.</param>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    /// <exception cref="ArgumentNullException">Se a assinatura for nula.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver assinado/compartilhado ou a evolucao referenciada nao existir/estiver nao assinada.</exception>
    public void AdicionarAdendo(Guid evolucaoReferenciadaId, string texto, AssinaturaDigital assinatura, DateTimeOffset dataHora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        ArgumentNullException.ThrowIfNull(assinatura);
        GarantirAssinado();

        var referenciada = _evolucoes.FirstOrDefault(e => e.Id.Value == evolucaoReferenciadaId);
        if (referenciada is null || !referenciada.Assinada)
        {
            throw new InvalidOperationException("Adendo exige uma evolucao referenciada existente e assinada.");
        }

        var adendo = EvolucaoSOAP.RegistrarAdendo(evolucaoReferenciadaId, texto, dataHora);
        _evolucoes.Add(adendo);
        RaiseDomainEvent(new AdendoRegistrado(Id, evolucaoReferenciadaId));
    }

    /// <summary>
    /// Compartilha o RES na RNDS (Bundle FHIR R4 via mTLS+ICP-Brasil): exige situacao Assinado em NGS2 — I-5.
    /// </summary>
    /// <param name="protocoloRnds">Protocolo de aceite retornado pela RNDS.</param>
    /// <exception cref="ArgumentException">Se o protocolo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver Assinado ou nao for NGS2.</exception>
    public void CompartilharNaRNDS(string protocoloRnds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocoloRnds);
        if (Situacao != SituacaoAtendimento.Assinado)
        {
            throw new InvalidOperationException($"Compartilhar na RNDS exige situacao Assinado. Situacao atual: {Situacao}.");
        }

        if (NivelGarantia != NivelGarantia.NGS2)
        {
            throw new InvalidOperationException("Compartilhar na RNDS exige NGS2 (ICP-Brasil).");
        }

        ProtocoloRnds = protocoloRnds.Trim();
        Situacao = SituacaoAtendimento.Compartilhado;
        RaiseDomainEvent(new RESCompartilhadoNaRNDS(Id));
    }

    /// <summary>
    /// Lanca a producao da competencia no SISAB: exige situacao Assinado/Compartilhado. Nao altera a situacao — I-6.
    /// </summary>
    /// <param name="competencia">Competencia da producao.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver assinado/compartilhado.</exception>
    public void LancarNoSISAB(Competencia competencia)
    {
        if (Situacao is not (SituacaoAtendimento.Assinado or SituacaoAtendimento.Compartilhado))
        {
            throw new InvalidOperationException($"Lancar no SISAB exige situacao Assinado/Compartilhado. Situacao atual: {Situacao}.");
        }

        RaiseDomainEvent(new PEPLancadoNoSISAB(Id, competencia));
    }

    /// <summary>Cancela o atendimento antes da assinatura (somente EmAndamento) — I-10.</summary>
    /// <param name="motivo">Motivo do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o atendimento ja estiver assinado/compartilhado.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoAtendimento.EmAndamento)
        {
            throw new InvalidOperationException($"Cancelamento so e permitido antes da assinatura. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoAtendimento.Cancelado;
        RaiseDomainEvent(new AtendimentoCancelado(Id, motivo.Trim()));
    }

    private void GarantirEmAndamento()
    {
        if (Situacao != SituacaoAtendimento.EmAndamento)
        {
            throw new InvalidOperationException(
                $"Operacao exige atendimento EmAndamento (registro imutavel apos assinatura — somente adendo). Situacao atual: {Situacao}.");
        }
    }

    private void GarantirAssinado()
    {
        if (Situacao is not (SituacaoAtendimento.Assinado or SituacaoAtendimento.Compartilhado))
        {
            throw new InvalidOperationException(
                $"Operacao exige atendimento Assinado/Compartilhado. Situacao atual: {Situacao}.");
        }
    }
}
