using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

/// <summary>Identificador forte do agregado <see cref="ProntuarioSuas"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProntuarioSuasId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProntuarioSuasId"/>.</returns>
    public static ProntuarioSuasId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro <b>sigiloso</b> do acompanhamento familiar no SUAS (PAIF em CRAS / PAEFI em CREAS):
/// reune atendimentos, plano de acompanhamento e violacoes de direitos, sob sigilo profissional
/// e trilha de acesso imutavel (quem leu, quando, por que). Marco: CF arts. 203-204; Lei
/// 8.742/1993 (LOAS); Tipificacao Nacional (Res. CNAS 109/2009); NOB-SUAS/2012; LGPD art. 11.
/// </summary>
public sealed class ProntuarioSuas : AggregateRoot<ProntuarioSuasId>, IMustHaveTenant
{
    private readonly List<RegistroAcompanhamento> _registros = [];
    private readonly List<ViolacaoDireito> _violacoes = [];
    private readonly List<AcessoProntuario> _acessos = [];

    private ProntuarioSuas()
    {
    }

    private ProntuarioSuas(
        ProntuarioSuasId id,
        Guid tenantId,
        Guid familiaId,
        Guid unidadeAtendimentoId,
        DateOnly dataAbertura)
        : base(id)
    {
        TenantId = tenantId;
        FamiliaId = familiaId;
        UnidadeAtendimentoId = unidadeAtendimentoId;
        DataAbertura = dataAbertura;
        Situacao = SituacaoProntuario.Aberto;
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Familia acompanhada.</summary>
    public Guid FamiliaId { get; private set; }

    /// <summary>CRAS/CREAS responsavel (define os servicos permitidos).</summary>
    public Guid UnidadeAtendimentoId { get; private set; }

    /// <summary>Situacao atual do prontuario no ciclo de acompanhamento.</summary>
    public SituacaoProntuario Situacao { get; private set; }

    /// <summary>Data de abertura do acompanhamento.</summary>
    public DateOnly DataAbertura { get; private set; }

    /// <summary>Motivo do encerramento (preenchido quando <see cref="SituacaoProntuario.Encerrado"/>).</summary>
    public string? MotivoEncerramento { get; private set; }

    /// <summary>Data do encerramento.</summary>
    public DateOnly? DataEncerramento { get; private set; }

    /// <summary>Atendimentos/evolucoes do acompanhamento (entidades-filhas; conteudo sigiloso).</summary>
    public IReadOnlyCollection<RegistroAcompanhamento> Registros => _registros;

    /// <summary>Plano de acompanhamento familiar (entidade-filha; conteudo sigiloso).</summary>
    public PlanoAcompanhamentoFamiliar? Plano { get; private set; }

    /// <summary>Violacoes de direitos registradas (entidades-filhas; dado sensivel — art. 11 LGPD).</summary>
    public IReadOnlyCollection<ViolacaoDireito> Violacoes => _violacoes;

    /// <summary>Trilha imutavel (append-only) de acessos ao prontuario (quem/quando/por que).</summary>
    public IReadOnlyCollection<AcessoProntuario> Acessos => _acessos;

    /// <summary>
    /// Abre o prontuario ao iniciar o acompanhamento de uma familia. Nasce em situacao
    /// <see cref="SituacaoProntuario.Aberto"/> (I-1, I-2; sem evento de dominio nesta versao).
    /// </summary>
    /// <param name="tenantId">Tenant (municipio) dono do registro.</param>
    /// <param name="familiaId">Familia acompanhada (obrigatorio).</param>
    /// <param name="unidadeAtendimentoId">CRAS/CREAS responsavel (obrigatorio).</param>
    /// <param name="dataAbertura">Data de abertura do acompanhamento.</param>
    /// <returns>Novo <see cref="ProntuarioSuas"/> em situacao <see cref="SituacaoProntuario.Aberto"/>.</returns>
    /// <exception cref="ArgumentException">Se a familia ou a unidade nao forem informadas (I-1).</exception>
    public static ProntuarioSuas Abrir(
        Guid tenantId,
        Guid familiaId,
        Guid unidadeAtendimentoId,
        DateOnly dataAbertura)
    {
        // I-1: familia e unidade sao obrigatorias na abertura.
        if (familiaId == Guid.Empty)
        {
            throw new ArgumentException("Familia e obrigatoria na abertura do prontuario.", nameof(familiaId));
        }

        if (unidadeAtendimentoId == Guid.Empty)
        {
            throw new ArgumentException("Unidade de atendimento e obrigatoria na abertura do prontuario.", nameof(unidadeAtendimentoId));
        }

        return new ProntuarioSuas(
            ProntuarioSuasId.New(),
            tenantId,
            familiaId,
            unidadeAtendimentoId,
            dataAbertura);
    }

    /// <summary>
    /// Registra um atendimento (PAIF/PAEFI/SCFV) no prontuario, mantendo a situacao <see cref="SituacaoProntuario.Aberto"/>;
    /// emite <see cref="AtendimentoRegistrado"/> (I-5). Exige compatibilidade servico↔unidade (I-4).
    /// </summary>
    /// <param name="servico">Servico socioassistencial.</param>
    /// <param name="dataAtendimento">Data do atendimento.</param>
    /// <param name="descricao">Descricao sigilosa do atendimento.</param>
    /// <param name="profissionalId">Profissional autor.</param>
    /// <param name="tipoUnidade">Tipo da unidade do prontuario (CRAS/CREAS), para validar a oferta.</param>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o prontuario estiver encerrado (I-3) ou o servico for incompativel com a unidade (I-4).</exception>
    public void RegistrarAtendimento(
        TipoServico servico,
        DateOnly dataAtendimento,
        string descricao,
        Guid profissionalId,
        TipoUnidadeAtendimento tipoUnidade)
    {
        GarantirAberto(); // I-3
        GarantirServicoCompativel(servico, tipoUnidade); // I-4

        var registro = RegistroAcompanhamento.Registrar(servico, dataAtendimento, descricao, profissionalId);
        _registros.Add(registro);
        RaiseDomainEvent(new AtendimentoRegistrado(Id, UnidadeAtendimentoId, servico, dataAtendimento));
    }

    /// <summary>Define (pactua) o plano de acompanhamento familiar; exige situacao <see cref="SituacaoProntuario.Aberto"/> (I-12).</summary>
    /// <param name="objetivos">Objetivos do plano.</param>
    /// <param name="dataPactuacao">Data de pactuacao.</param>
    /// <param name="compromissos">Compromissos pactuados (opcional).</param>
    /// <exception cref="ArgumentException">Se os objetivos forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se o prontuario estiver encerrado (I-12).</exception>
    public void DefinirPlano(string objetivos, DateOnly dataPactuacao, IEnumerable<string>? compromissos = null)
    {
        GarantirAberto(); // I-12
        Plano = PlanoAcompanhamentoFamiliar.Pactuar(objetivos, dataPactuacao, compromissos);
    }

    /// <summary>Registra uma violacao de direito; exige situacao <see cref="SituacaoProntuario.Aberto"/> (I-12, I-10).</summary>
    /// <param name="tipoViolacao">Tipo da violacao.</param>
    /// <param name="envolveCriancaAdolescente">Se envolve crianca/adolescente (dado sensivel reforcado — art. 11 LGPD).</param>
    /// <param name="dataIdentificacao">Data de identificacao.</param>
    /// <exception cref="InvalidOperationException">Se o prontuario estiver encerrado (I-12).</exception>
    public void RegistrarViolacao(TipoViolacaoDireito tipoViolacao, bool envolveCriancaAdolescente, DateOnly dataIdentificacao)
    {
        GarantirAberto(); // I-12
        _violacoes.Add(ViolacaoDireito.Registrar(tipoViolacao, envolveCriancaAdolescente, dataIdentificacao));
    }

    /// <summary>
    /// Encerra o acompanhamento, com motivo; passa a <see cref="SituacaoProntuario.Encerrado"/>,
    /// grava a data e emite <see cref="AcompanhamentoEncerrado"/> (I-6, I-11).
    /// </summary>
    /// <param name="motivoEncerramento">Motivo do encerramento (obrigatorio).</param>
    /// <param name="dataEncerramento">Data do encerramento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio (I-6).</exception>
    /// <exception cref="InvalidOperationException">Se o prontuario ja estiver encerrado (I-11).</exception>
    public void EncerrarAcompanhamento(string motivoEncerramento, DateOnly dataEncerramento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoEncerramento); // I-6
        GarantirAberto(); // I-11

        Situacao = SituacaoProntuario.Encerrado;
        MotivoEncerramento = motivoEncerramento;
        DataEncerramento = dataEncerramento;
        RaiseDomainEvent(new AcompanhamentoEncerrado(Id, motivoEncerramento));
    }

    /// <summary>
    /// Registra (append-only) um acesso ao conteudo sigiloso na trilha imutavel (I-7, I-8).
    /// Nao altera a situacao de acompanhamento e nunca remove/atualiza acessos anteriores.
    /// </summary>
    /// <param name="usuarioId">Usuario que acessou.</param>
    /// <param name="motivoAcesso">Justificativa obrigatoria do acesso.</param>
    /// <param name="dataHoraAcessoUtc">Momento (UTC) do acesso.</param>
    /// <exception cref="ArgumentException">Se o motivo de acesso for vazio (I-7).</exception>
    public void RegistrarAcesso(Guid usuarioId, string motivoAcesso, DateTime dataHoraAcessoUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoAcesso); // I-7
        _acessos.Add(AcessoProntuario.Registrar(usuarioId, motivoAcesso, dataHoraAcessoUtc));
    }

    private void GarantirAberto()
    {
        if (Situacao != SituacaoProntuario.Aberto)
        {
            throw new InvalidOperationException($"Operacao exige prontuario Aberto. Situacao atual: {Situacao}.");
        }
    }

    private static void GarantirServicoCompativel(TipoServico servico, TipoUnidadeAtendimento tipoUnidade)
    {
        // I-4: PAIF so em CRAS; PAEFI so em CREAS. SCFV segue a oferta da unidade.
        switch (servico)
        {
            case TipoServico.Paif when tipoUnidade != TipoUnidadeAtendimento.Cras:
                throw new InvalidOperationException("PAIF e ofertado somente em unidade CRAS.");
            case TipoServico.Paefi when tipoUnidade != TipoUnidadeAtendimento.Creas:
                throw new InvalidOperationException("PAEFI e ofertado somente em unidade CREAS.");
            default:
                break;
        }
    }
}
