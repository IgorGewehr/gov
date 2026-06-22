using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

/// <summary>
/// Solicitacao de regulacao de procedimento (consulta especializada, exame ou leito) para um
/// paciente, classificada por <see cref="Procedimento"/> SIGTAP, <see cref="Prioridade"/> e consumo
/// de <see cref="Cota"/>. Ao ser autorizada, reserva a vaga no SISREG e consome cota. Materializa o
/// acesso regulado a media/alta complexidade. Trata dado pessoal sensivel (LGPD art. 11). Raiz de
/// agregado e fronteira de consistencia transacional.
/// </summary>
public sealed class SolicitacaoRegulacao : AggregateRoot<SolicitacaoRegulacaoId>, IMustHaveTenant
{
    private SolicitacaoRegulacao()
    {
    }

    private SolicitacaoRegulacao(
        SolicitacaoRegulacaoId id,
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoSolicitanteId,
        ProfissionalId profissionalSolicitanteId,
        Procedimento procedimento,
        Prioridade prioridade,
        Cota cota,
        string justificativa,
        DateOnly dataSolicitacao)
        : base(id)
    {
        TenantId = tenantId;
        PacienteId = pacienteId;
        EstabelecimentoSolicitanteId = estabelecimentoSolicitanteId;
        ProfissionalSolicitanteId = profissionalSolicitanteId;
        Procedimento = procedimento;
        Prioridade = prioridade;
        Cota = cota;
        Justificativa = justificativa;
        DataSolicitacao = dataSolicitacao;
        Situacao = SituacaoSolicitacaoRegulacao.Solicitada;
        RaiseDomainEvent(new RegulacaoSolicitada(id, pacienteId, procedimento));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Paciente da solicitacao (referencia por Id).</summary>
    public PacienteId PacienteId { get; private set; }

    /// <summary>Unidade solicitante (CNES).</summary>
    public EstabelecimentoId EstabelecimentoSolicitanteId { get; private set; }

    /// <summary>Profissional solicitante.</summary>
    public ProfissionalId ProfissionalSolicitanteId { get; private set; }

    /// <summary>Procedimento SIGTAP (codigo + descricao).</summary>
    public Procedimento Procedimento { get; private set; }

    /// <summary>Classificacao de risco/urgencia.</summary>
    public Prioridade Prioridade { get; private set; }

    /// <summary>Cota/limite de vagas aplicavel (snapshot do consumo).</summary>
    public Cota Cota { get; private set; }

    /// <summary>Justificativa clinica do pedido.</summary>
    public string Justificativa { get; private set; } = default!;

    /// <summary>Data de abertura da solicitacao.</summary>
    public DateOnly DataSolicitacao { get; private set; }

    /// <summary>Data da autorizacao (nula ate autorizar).</summary>
    public DateOnly? DataAutorizacao { get; private set; }

    /// <summary>Protocolo da reserva no SISREG (nulo ate autorizar).</summary>
    public string? ProtocoloSisreg { get; private set; }

    /// <summary>Situacao atual no fluxo de regulacao.</summary>
    public SituacaoSolicitacaoRegulacao Situacao { get; private set; }

    /// <summary>
    /// Abre uma nova solicitacao de regulacao (estado inicial <c>Solicitada</c>), emitindo
    /// <see cref="RegulacaoSolicitada"/>. A confirmacao do CNS do paciente (I-2) e validada no handler.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="pacienteId">Paciente da solicitacao.</param>
    /// <param name="estabelecimentoSolicitanteId">Unidade solicitante (CNES).</param>
    /// <param name="profissionalSolicitanteId">Profissional solicitante.</param>
    /// <param name="procedimento">Procedimento SIGTAP.</param>
    /// <param name="prioridade">Classificacao de risco/urgencia.</param>
    /// <param name="cota">Cota aplicavel ao procedimento.</param>
    /// <param name="justificativa">Justificativa clinica.</param>
    /// <param name="dataSolicitacao">Data de abertura.</param>
    /// <returns>Nova <see cref="SolicitacaoRegulacao"/>.</returns>
    /// <exception cref="ArgumentException">Se procedimento/justificativa forem invalidos (I-1) ou a prioridade for invalida (I-11).</exception>
    public static SolicitacaoRegulacao Solicitar(
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoSolicitanteId,
        ProfissionalId profissionalSolicitanteId,
        Procedimento procedimento,
        Prioridade prioridade,
        Cota cota,
        string justificativa,
        DateOnly dataSolicitacao)
    {
        // I-1: procedimento SIGTAP valido (codigo nao vazio) e justificativa nao vazia.
        ArgumentException.ThrowIfNullOrWhiteSpace(procedimento.CodigoSigtap);
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);

        // I-11: prioridade obrigatoria e valida.
        if (!Enum.IsDefined(prioridade))
        {
            throw new ArgumentException($"Prioridade invalida: '{prioridade}'.", nameof(prioridade));
        }

        return new SolicitacaoRegulacao(
            SolicitacaoRegulacaoId.New(),
            tenantId,
            pacienteId,
            estabelecimentoSolicitanteId,
            profissionalSolicitanteId,
            procedimento,
            prioridade,
            cota,
            justificativa.Trim(),
            dataSolicitacao);
    }

    /// <summary>
    /// Autoriza a solicitacao: exige situacao em analise e cota com disponibilidade (I-3); consome a
    /// cota, grava a data e o protocolo da reserva no SISREG e emite <see cref="SolicitacaoAutorizada"/> (I-4).
    /// </summary>
    /// <param name="protocoloSisreg">Protocolo da reserva retornado pelo SISREG.</param>
    /// <param name="dataAutorizacao">Data da autorizacao.</param>
    /// <exception cref="ArgumentException">Se o protocolo do SISREG for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao estiver em analise ou a cota estiver esgotada (I-3).</exception>
    public void Autorizar(string protocoloSisreg, DateOnly dataAutorizacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocoloSisreg);
        GarantirEmAnalise();

        // I-3: autorizacao exige disponibilidade de cota.
        if (!Cota.TemDisponibilidade())
        {
            throw new InvalidOperationException("Cota esgotada: nao ha vaga disponivel para autorizar.");
        }

        Cota = Cota.ConsumirVaga();
        DataAutorizacao = dataAutorizacao;
        ProtocoloSisreg = protocoloSisreg.Trim();
        Situacao = SituacaoSolicitacaoRegulacao.Autorizada;
        RaiseDomainEvent(new SolicitacaoAutorizada(Id, ProtocoloSisreg));
    }

    /// <summary>Nega (indefere) a solicitacao em analise; passa a <c>Negada</c> (terminal) (I-5).</summary>
    /// <param name="motivo">Motivo da negativa.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao estiver em analise.</exception>
    public void Negar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirEmAnalise();

        Situacao = SituacaoSolicitacaoRegulacao.Negada;
        RaiseDomainEvent(new SolicitacaoNegada(Id, motivo.Trim()));
    }

    /// <summary>Devolve a solicitacao <c>Solicitada</c> ao solicitante para complementacao (I-6).</summary>
    /// <param name="motivo">Motivo da devolucao.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <c>Solicitada</c>.</exception>
    public void Devolver(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoSolicitacaoRegulacao.Solicitada)
        {
            throw new InvalidOperationException($"A devolucao so e permitida para solicitacao 'Solicitada'. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoSolicitacaoRegulacao.Devolvida;
        RaiseDomainEvent(new SolicitacaoDevolvida(Id, motivo.Trim()));
    }

    /// <summary>Marca como executado o procedimento autorizado; passa a <c>Executada</c> (terminal) (I-8).</summary>
    /// <param name="dataExecucao">Data de execucao (registro).</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <c>Autorizada</c>.</exception>
    public void Executar(DateOnly dataExecucao)
    {
        _ = dataExecucao;
        if (Situacao != SituacaoSolicitacaoRegulacao.Autorizada)
        {
            throw new InvalidOperationException($"A execucao exige solicitacao 'Autorizada'. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoSolicitacaoRegulacao.Executada;
        RaiseDomainEvent(new ProcedimentoExecutado(Id));
    }

    /// <summary>
    /// Cancela a solicitacao enquanto cancelavel ({<c>Solicitada</c>,<c>Devolvida</c>,<c>Autorizada</c>}),
    /// antes de executada (I-7/I-9). Ao cancelar uma <c>Autorizada</c>, a cota reservada e devolvida.
    /// </summary>
    /// <param name="motivo">Motivo do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a solicitacao estiver encerrada/executada.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is not (SituacaoSolicitacaoRegulacao.Solicitada
            or SituacaoSolicitacaoRegulacao.Devolvida
            or SituacaoSolicitacaoRegulacao.Autorizada))
        {
            throw new InvalidOperationException($"A solicitacao nao pode ser cancelada. Situacao atual: {Situacao}.");
        }

        // I-7: cancelar uma autorizada devolve a cota reservada.
        if (Situacao == SituacaoSolicitacaoRegulacao.Autorizada)
        {
            Cota = Cota.DevolverVaga();
        }

        Situacao = SituacaoSolicitacaoRegulacao.Cancelada;
        RaiseDomainEvent(new SolicitacaoCancelada(Id, motivo.Trim()));
    }

    private void GarantirEmAnalise()
    {
        if (Situacao is not (SituacaoSolicitacaoRegulacao.Solicitada or SituacaoSolicitacaoRegulacao.Devolvida))
        {
            throw new InvalidOperationException($"A solicitacao nao esta em analise. Situacao atual: {Situacao}.");
        }
    }
}
