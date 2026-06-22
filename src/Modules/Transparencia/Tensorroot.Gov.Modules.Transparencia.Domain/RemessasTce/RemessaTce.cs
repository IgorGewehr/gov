using Tensorroot.Gov.Modules.Transparencia.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>Identificador forte do agregado <see cref="RemessaTce"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RemessaTceId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RemessaTceId"/>.</returns>
    public static RemessaTceId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Pacote de arquivos, por leiaute versionado, enviado ao TCE-RS (SIAPC/PAD) para prestação de
/// contas. Raiz de agregado do módulo Transparencia (papel CONSUMIDOR). Ciclo
/// <c>Gerada → Validada → Enviada → Homologada/Rejeitada</c>: nasce válida em <c>Gerada</c> com
/// <see cref="HashIntegridade"/> calculado; é validada localmente pelo e-Validador (RDI); só transita
/// a <c>Enviada</c> com RDI sem erro e hash íntegro (invariante central); e pode emitir alerta de
/// prazo vencido (risco de bloqueio de transferências — LRF art. 23 §3º).
/// </summary>
public sealed class RemessaTce : AggregateRoot<RemessaTceId>, IMustHaveTenant
{
    private readonly List<ArquivoRemessa> _arquivos = [];
    private bool _alertaPrazoEmitido;

    private RemessaTce()
    {
    }

    private RemessaTce(
        RemessaTceId id,
        Guid tenantId,
        Periodo periodo,
        Leiaute leiaute,
        HashIntegridade hashIntegridade,
        DateOnly dataLimite,
        DateOnly dataGeracao,
        IEnumerable<ArquivoRemessa> arquivos)
        : base(id)
    {
        TenantId = tenantId;
        Periodo = periodo;
        Leiaute = leiaute;
        HashIntegridade = hashIntegridade;
        DataLimite = dataLimite;
        DataGeracao = dataGeracao;
        Situacao = SituacaoRemessaTce.Gerada;
        _arquivos.AddRange(arquivos);
        RaiseDomainEvent(new RemessaGerada(id, periodo.ToString(), leiaute.Versao));
    }

    /// <summary>Tenant (ente público) dono do registro. <see cref="IMustHaveTenant"/>.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Competência/exercício da remessa.</summary>
    public Periodo Periodo { get; private set; } = default!;

    /// <summary>Especificação versionada do layout.</summary>
    public Leiaute Leiaute { get; private set; } = default!;

    /// <summary>Estado atual da remessa.</summary>
    public SituacaoRemessaTce Situacao { get; private set; }

    /// <summary>Hash do pacote (definido na geração; obrigatório para envio).</summary>
    public HashIntegridade? HashIntegridade { get; private set; }

    /// <summary>Prazo legal/parametrizado de envio do período.</summary>
    public DateOnly DataLimite { get; private set; }

    /// <summary>Data de geração do pacote.</summary>
    public DateOnly DataGeracao { get; private set; }

    /// <summary>Data de transmissão ao SIAPC/PAD (nula antes do envio).</summary>
    public DateOnly? DataEnvio { get; private set; }

    /// <summary>Nome do ZIP empacotado (preenchido ao marcar pronta para transmissão).</summary>
    public string? NomeArquivoZip { get; private set; }

    /// <summary>Protocolo/recibo retornado pelo PAD/e-Protocolo, registrado pelo operador (ato humano).</summary>
    public string? ProtocoloTce { get; private set; }

    /// <summary>Arquivos componentes (entidades-filhas).</summary>
    public IReadOnlyCollection<ArquivoRemessa> Arquivos => _arquivos;

    /// <summary>RDI mais recente (nulo antes de validar).</summary>
    public ResultadoValidacao? ResultadoValidacao { get; private set; }

    /// <summary>Indica se o alerta de prazo vencido já foi emitido (idempotência — I-12).</summary>
    public bool AlertaPrazoEmitido => _alertaPrazoEmitido;

    /// <summary>
    /// Consolida os itens e monta o pacote: a remessa nasce em <c>Gerada</c>, com
    /// <see cref="HashIntegridade"/> calculado sobre o pacote, e emite <c>RemessaGerada</c> (I-3).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro (I-1).</param>
    /// <param name="periodo">Competência/exercício (obrigatório — I-2).</param>
    /// <param name="leiaute">Leiaute versionado (obrigatório — I-2).</param>
    /// <param name="dataLimite">Prazo legal/parametrizado do período (I-11).</param>
    /// <param name="dataGeracao">Data de geração do pacote.</param>
    /// <param name="arquivos">Arquivos componentes do pacote.</param>
    /// <param name="conteudoPacote">Conteúdo consolidado do pacote para cálculo do hash de integridade.</param>
    /// <returns>Nova <see cref="RemessaTce"/> em <c>Gerada</c>.</returns>
    /// <exception cref="ArgumentNullException">Se período ou leiaute forem nulos.</exception>
    /// <exception cref="ArgumentException">Se não houver arquivos.</exception>
    public static RemessaTce GerarRemessa(
        Guid tenantId,
        Periodo periodo,
        Leiaute leiaute,
        DateOnly dataLimite,
        DateOnly dataGeracao,
        IEnumerable<ArquivoRemessa> arquivos,
        ReadOnlyMemory<byte> conteudoPacote)
    {
        // I-2: Periodo e Leiaute obrigatórios e não nulos na geração.
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(leiaute);
        ArgumentNullException.ThrowIfNull(arquivos);

        var lista = arquivos.ToList();
        if (lista.Count == 0)
        {
            throw new ArgumentException("A remessa deve conter ao menos um arquivo.", nameof(arquivos));
        }

        // I-3: hash calculado sobre o pacote na geração (imutabilidade/retenção — I-9).
        var hash = HashIntegridade.Calcular(conteudoPacote.Span);

        return new RemessaTce(
            RemessaTceId.New(),
            tenantId,
            periodo,
            leiaute,
            hash,
            dataLimite,
            dataGeracao,
            lista);
    }

    /// <summary>
    /// Aplica o RDI do e-Validador: sem erro transita para <c>Validada</c> (<c>RemessaValidada</c>);
    /// com erro transita para <c>Rejeitada</c> (<c>RemessaRejeitada</c>) e bloqueia o envio.
    /// </summary>
    /// <param name="resultado">RDI apurado pelo e-Validador.</param>
    /// <exception cref="ArgumentNullException">Se o RDI for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se a situação não for <c>Gerada</c> (I-4/I-10).</exception>
    public void RegistrarResultadoValidacao(ResultadoValidacao resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        // I-4/I-10: a validação só ocorre a partir de Gerada (estado Validável).
        if (Situacao != SituacaoRemessaTce.Gerada)
        {
            throw new InvalidOperationException(
                $"A validação só ocorre a partir de Gerada. Situação atual: {Situacao}.");
        }

        ResultadoValidacao = resultado;

        if (resultado.PossuiErro)
        {
            // I-5: erro no RDI ⇒ Rejeitada; envio bloqueado.
            Situacao = SituacaoRemessaTce.Rejeitada;
            RaiseDomainEvent(new RemessaRejeitada(Id, resultado.QuantidadeErros));
        }
        else
        {
            // I-6: RDI limpo ⇒ Validada (avisos não bloqueiam — CB-6).
            Situacao = SituacaoRemessaTce.Validada;
            RaiseDomainEvent(new RemessaValidada(Id));
        }
    }

    /// <summary>
    /// Marca a remessa como PRONTA PARA TRANSMISSÃO: empacotada (ZIP nomeado) e disponibilizada para
    /// download. Só transita a partir de <c>Validada</c> e com <see cref="HashIntegridade"/> presente/
    /// íntegro (I-7). NÃO é transmissão por HTTP — o TCE-RS não tem API de envio; a transmissão é MANUAL.
    /// </summary>
    /// <param name="nomeArquivoZip">Nome estruturado do ZIP empacotado.</param>
    /// <param name="conteudoPacote">Conteúdo consolidado do pacote para conferência do hash de integridade.</param>
    /// <exception cref="ArgumentException">Se o nome do ZIP for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situação não for <c>Validada</c> ou o hash for inválido/ausente.</exception>
    public void MarcarProntaParaTransmissao(string nomeArquivoZip, ReadOnlySpan<byte> conteudoPacote)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeArquivoZip);

        // I-7/I-10: só empacota a partir de Validada (terminais bloqueados).
        if (Situacao != SituacaoRemessaTce.Validada)
        {
            throw new InvalidOperationException(
                $"O empacotamento só ocorre a partir de Validada. Situação atual: {Situacao}.");
        }

        // I-7/CB-7: pacote sem hash não pode ser empacotado.
        if (HashIntegridade is null)
        {
            throw new InvalidOperationException("Pacote sem hash de integridade não pode ser empacotado.");
        }

        // I-9/Cenário 7: integridade verificada antes da transição.
        if (!HashIntegridade.Confere(conteudoPacote))
        {
            throw new InvalidOperationException("Hash de integridade do pacote inválido; empacotamento bloqueado.");
        }

        Situacao = SituacaoRemessaTce.ProntaParaTransmissao;
        NomeArquivoZip = nomeArquivoZip;
        RaiseDomainEvent(new RemessaProntaParaTransmissao(Id, nomeArquivoZip));
    }

    /// <summary>
    /// Registra o PROTOCOLO/RECIBO retornado pelo PAD/e-Protocolo (ato humano): o operador, após transmitir
    /// manualmente no portal, cola o protocolo. Transita de <c>ProntaParaTransmissao</c> a <c>Enviada</c>.
    /// Gated por <c>transparencia.remessa.transmitir</c> (SoD) no handler/endpoint.
    /// </summary>
    /// <param name="protocolo">Protocolo/recibo retornado pelo portal (não vazio).</param>
    /// <param name="dataRecibo">Data do recibo.</param>
    /// <exception cref="ArgumentException">Se o protocolo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situação não for <c>ProntaParaTransmissao</c> (I-10).</exception>
    public void RegistrarProtocolo(string protocolo, DateOnly dataRecibo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocolo);

        if (Situacao != SituacaoRemessaTce.ProntaParaTransmissao)
        {
            throw new InvalidOperationException(
                $"O protocolo só pode ser registrado a partir de ProntaParaTransmissao. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoRemessaTce.Enviada;
        DataEnvio = dataRecibo;
        ProtocoloTce = protocolo;
        RaiseDomainEvent(new RemessaEnviadaTce(Id, dataRecibo, protocolo));
    }

    /// <summary>Registra a homologação do TCE (passa a <c>Homologada</c>).</summary>
    /// <exception cref="InvalidOperationException">Se a situação não for <c>Enviada</c> (I-8/I-10).</exception>
    public void Homologar()
    {
        // I-8/I-10: homologação só a partir de Enviada (terminais bloqueados).
        if (Situacao != SituacaoRemessaTce.Enviada)
        {
            throw new InvalidOperationException(
                $"A homologação só ocorre a partir de Enviada. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoRemessaTce.Homologada;
        RaiseDomainEvent(new RemessaHomologada(Id));
    }

    /// <summary>
    /// Sinaliza o vencimento do prazo legal sem envio (alerta de risco de bloqueio de transferências —
    /// LRF art. 23 §3º). Não altera a <see cref="Situacao"/>; idempotente (não duplica o alerta — I-12).
    /// </summary>
    /// <param name="hoje">Data de referência da verificação.</param>
    /// <exception cref="InvalidOperationException">Se já enviada/homologada, se o prazo não venceu, ou se o alerta já foi emitido.</exception>
    public void VencerPrazo(DateOnly hoje)
    {
        // I-12/CB-11: só se aplica a remessa não enviada.
        if (Situacao is SituacaoRemessaTce.Enviada or SituacaoRemessaTce.Homologada)
        {
            throw new InvalidOperationException(
                $"Não há risco de bloqueio para remessa {Situacao}.");
        }

        // I-12/CB-9: exige hoje > DataLimite (igualdade não dispara).
        if (hoje <= DataLimite)
        {
            throw new InvalidOperationException("O prazo da remessa ainda não venceu.");
        }

        // I-12/CB-10: idempotência — emite o alerta uma única vez.
        if (_alertaPrazoEmitido)
        {
            throw new InvalidOperationException("O alerta de prazo vencido já foi emitido para esta remessa.");
        }

        _alertaPrazoEmitido = true;
        RaiseDomainEvent(new PrazoRemessaVencido(Id, DataLimite));
    }
}
