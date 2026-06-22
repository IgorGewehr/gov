using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;

/// <summary>Identificador forte do agregado <see cref="ProcessoArbitramentoItbi"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProcessoArbitramentoItbiId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProcessoArbitramentoItbiId"/>.</returns>
    public static ProcessoArbitramentoItbiId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Processo administrativo de arbitramento da base de cálculo do ITBI (CTN art. 148), modelado como
/// agregado com máquina de estados. Sob o Tema 1.113/STJ, a base é o valor declarado (presunção de
/// veracidade); o município SÓ pode afastá-la por este processo regular, individualizado e com
/// contraditório — nunca de ofício por valor de referência. O desfecho (<see cref="Concluir"/>) é a
/// ÚNICA via para elevar a base no motor, via <see cref="ResultadoArbitramento"/>.
/// <para>Estados: Instaurado → AguardandoContraditorio → EmAnalise → Concluido | Cancelado.</para>
/// </summary>
public sealed class ProcessoArbitramentoItbi : AggregateRoot<ProcessoArbitramentoItbiId>, IMustHaveTenant
{
    private ProcessoArbitramentoItbi()
    {
    }

    private ProcessoArbitramentoItbi(
        ProcessoArbitramentoItbiId id,
        Guid tenantId,
        TransmissaoImobiliariaId transmissaoImobiliariaId,
        string numeroProcesso,
        string motivoInstauracao,
        ValorMonetario valorPropostoFisco,
        string fundamentacaoFisco,
        Guid responsavelId,
        DateOnly dataInstauracao)
        : base(id)
    {
        TenantId = tenantId;
        TransmissaoImobiliariaId = transmissaoImobiliariaId;
        NumeroProcesso = numeroProcesso;
        MotivoInstauracao = motivoInstauracao;
        ValorPropostoFisco = valorPropostoFisco;
        FundamentacaoFisco = fundamentacaoFisco;
        ResponsavelId = responsavelId;
        DataInstauracao = dataInstauracao;
        Estado = EstadoArbitramentoItbi.Instaurado;
        RaiseDomainEvent(new ProcessoArbitramentoItbiInstaurado(id, tenantId, transmissaoImobiliariaId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Transmissão imobiliária cuja base de cálculo está sob arbitramento.</summary>
    public TransmissaoImobiliariaId TransmissaoImobiliariaId { get; private set; }

    /// <summary>Número/protocolo do processo administrativo.</summary>
    public string NumeroProcesso { get; private set; } = default!;

    /// <summary>
    /// Motivo individualizado da instauração (por que a declaração "não merece fé": omissa, falsa
    /// ou não fidedigna — CTN art. 148). Não basta "menor que a pauta".
    /// </summary>
    public string MotivoInstauracao { get; private set; } = default!;

    /// <summary>Valor proposto pelo fisco como base (R$) — sujeito ao contraditório.</summary>
    public ValorMonetario ValorPropostoFisco { get; private set; } = default!;

    /// <summary>Fundamentação técnica/legal do fisco para a proposta (ônus da prova do fisco).</summary>
    public string FundamentacaoFisco { get; private set; } = default!;

    /// <summary>Justificativa/avaliação contraditória do contribuinte (art. 148, parte final), quando houver.</summary>
    public string? JustificativaContribuinte { get; private set; }

    /// <summary>Valor arbitrado na decisão final (R$) — definido apenas na conclusão.</summary>
    public ValorMonetario? ValorArbitradoFinal { get; private set; }

    /// <summary>Estado atual da máquina de arbitramento.</summary>
    public EstadoArbitramentoItbi Estado { get; private set; }

    /// <summary>Servidor responsável pela instauração.</summary>
    public Guid ResponsavelId { get; private set; }

    /// <summary>Data de instauração do processo.</summary>
    public DateOnly DataInstauracao { get; private set; }

    /// <summary>Data de abertura do contraditório (notificação do contribuinte).</summary>
    public DateOnly? DataAberturaContraditorio { get; private set; }

    /// <summary>Data em que a defesa foi apresentada (ou o prazo se esgotou).</summary>
    public DateOnly? DataApresentacaoContraditorio { get; private set; }

    /// <summary>Data do desfecho (conclusão ou cancelamento).</summary>
    public DateOnly? DataDesfecho { get; private set; }

    /// <summary>Indica se o processo está em estado terminal (concluído ou cancelado).</summary>
    public bool EstaEncerrado => Estado is EstadoArbitramentoItbi.Concluido or EstadoArbitramentoItbi.Cancelado;

    /// <summary>
    /// Instaura o processo de arbitramento da base do ITBI de uma transmissão (CTN art. 148).
    /// Exige motivo individualizado e fundamentação do fisco — não basta divergência de pauta.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="transmissaoImobiliariaId">Transmissão sob arbitramento.</param>
    /// <param name="numeroProcesso">Número/protocolo do processo administrativo.</param>
    /// <param name="motivoInstauracao">Motivo individualizado (por que a declaração não merece fé).</param>
    /// <param name="valorPropostoFisco">Valor proposto pelo fisco como base (R$).</param>
    /// <param name="fundamentacaoFisco">Fundamentação técnica/legal do fisco.</param>
    /// <param name="responsavelId">Servidor responsável.</param>
    /// <param name="dataInstauracao">Data de instauração.</param>
    /// <returns>Novo <see cref="ProcessoArbitramentoItbi"/> no estado Instaurado.</returns>
    /// <exception cref="ArgumentException">Se número, motivo ou fundamentação estiverem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o responsável for vazio.</exception>
    public static ProcessoArbitramentoItbi Instaurar(
        Guid tenantId,
        TransmissaoImobiliariaId transmissaoImobiliariaId,
        string numeroProcesso,
        string motivoInstauracao,
        ValorMonetario valorPropostoFisco,
        string fundamentacaoFisco,
        Guid responsavelId,
        DateOnly dataInstauracao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroProcesso);
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoInstauracao);
        ArgumentNullException.ThrowIfNull(valorPropostoFisco);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentacaoFisco);
        if (responsavelId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(responsavelId), "O responsável pela instauração do arbitramento é obrigatório.");
        }

        return new ProcessoArbitramentoItbi(
            ProcessoArbitramentoItbiId.New(),
            tenantId,
            transmissaoImobiliariaId,
            numeroProcesso.Trim(),
            motivoInstauracao.Trim(),
            valorPropostoFisco,
            fundamentacaoFisco.Trim(),
            responsavelId,
            dataInstauracao);
    }

    /// <summary>Notifica o contribuinte e abre o contraditório (passo OBRIGATÓRIO — não pode ser pulado).</summary>
    /// <param name="dataNotificacao">Data da notificação.</param>
    /// <exception cref="InvalidOperationException">Se o processo não estiver recém-instaurado.</exception>
    public void AbrirContraditorio(DateOnly dataNotificacao)
    {
        if (Estado != EstadoArbitramentoItbi.Instaurado)
        {
            throw new InvalidOperationException($"O contraditório só pode ser aberto a partir do estado Instaurado. Estado atual: {Estado}.");
        }

        DataAberturaContraditorio = dataNotificacao;
        Estado = EstadoArbitramentoItbi.AguardandoContraditorio;
        RaiseDomainEvent(new ContraditorioArbitramentoItbiAberto(Id, TenantId));
    }

    /// <summary>
    /// Registra a defesa/avaliação contraditória do contribuinte e encaminha o processo à análise do fisco.
    /// </summary>
    /// <param name="justificativaContribuinte">Texto da defesa/avaliação contraditória.</param>
    /// <param name="dataApresentacao">Data de apresentação da defesa.</param>
    /// <exception cref="InvalidOperationException">Se não estiver aguardando o contraditório.</exception>
    /// <exception cref="ArgumentException">Se a justificativa estiver vazia.</exception>
    public void RegistrarContraditorio(string justificativaContribuinte, DateOnly dataApresentacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativaContribuinte);
        if (Estado != EstadoArbitramentoItbi.AguardandoContraditorio)
        {
            throw new InvalidOperationException($"A defesa só pode ser registrada enquanto se aguarda o contraditório. Estado atual: {Estado}.");
        }

        JustificativaContribuinte = justificativaContribuinte.Trim();
        DataApresentacaoContraditorio = dataApresentacao;
        Estado = EstadoArbitramentoItbi.EmAnalise;
        RaiseDomainEvent(new ContraditorioArbitramentoItbiApresentado(Id, TenantId));
    }

    /// <summary>
    /// Conclui o processo com decisão final fundamentada e o valor arbitrado, gerando o
    /// <see cref="ResultadoArbitramento"/> que habilita o lançamento de ofício. Exige que o
    /// contraditório já tenha ocorrido (estado EmAnalise).
    /// </summary>
    /// <param name="valorArbitradoFinal">Valor arbitrado pela decisão (nova base de cálculo).</param>
    /// <param name="dataDecisao">Data da decisão.</param>
    /// <returns>O resultado do arbitramento (porta de entrada do recálculo no motor).</returns>
    /// <exception cref="InvalidOperationException">Se o processo não estiver em análise (contraditório não cumprido).</exception>
    /// <exception cref="ArgumentNullException">Se o valor arbitrado for nulo.</exception>
    public ResultadoArbitramento Concluir(ValorMonetario valorArbitradoFinal, DateOnly dataDecisao)
    {
        ArgumentNullException.ThrowIfNull(valorArbitradoFinal);
        if (Estado != EstadoArbitramentoItbi.EmAnalise)
        {
            throw new InvalidOperationException($"O arbitramento só pode ser concluído após a análise do contraditório. Estado atual: {Estado}.");
        }

        ValorArbitradoFinal = valorArbitradoFinal;
        DataDesfecho = dataDecisao;
        Estado = EstadoArbitramentoItbi.Concluido;
        RaiseDomainEvent(new ProcessoArbitramentoItbiConcluido(Id, TenantId, TransmissaoImobiliariaId, valorArbitradoFinal.Valor));

        return new ResultadoArbitramento(Id.Value, valorArbitradoFinal, ProcessoConcluido: true);
    }

    /// <summary>
    /// Cancela o processo (a declaração do contribuinte prevaleceu). Permitido enquanto não estiver
    /// em estado terminal.
    /// </summary>
    /// <param name="dataCancelamento">Data do cancelamento.</param>
    /// <exception cref="InvalidOperationException">Se o processo já estiver encerrado.</exception>
    public void Cancelar(DateOnly dataCancelamento)
    {
        if (EstaEncerrado)
        {
            throw new InvalidOperationException($"O processo já está encerrado. Estado atual: {Estado}.");
        }

        DataDesfecho = dataCancelamento;
        Estado = EstadoArbitramentoItbi.Cancelado;
        RaiseDomainEvent(new ProcessoArbitramentoItbiCancelado(Id, TenantId));
    }

    /// <summary>
    /// Reconstrói o <see cref="ResultadoArbitramento"/> de um processo já concluído (para recálculos
    /// posteriores). Falha se o processo não estiver concluído — preserva "só após processo".
    /// </summary>
    /// <returns>O resultado do arbitramento concluído.</returns>
    /// <exception cref="InvalidOperationException">Se o processo não estiver concluído.</exception>
    public ResultadoArbitramento ObterResultado()
    {
        if (Estado != EstadoArbitramentoItbi.Concluido || ValorArbitradoFinal is null)
        {
            throw new InvalidOperationException("O resultado do arbitramento só existe quando o processo está Concluido.");
        }

        return new ResultadoArbitramento(Id.Value, ValorArbitradoFinal, ProcessoConcluido: true);
    }
}
