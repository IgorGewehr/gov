using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Auto lavrado pela Vigilancia Sanitaria a partir de uma <see cref="Inspecao"/>: intimacao (pendencias +
/// prazo de regularizacao) ou infracao (formaliza infracao, abre prazo de defesa, pode cominar multa) —
/// rito do processo administrativo sanitario (Lei 6.437/1977). Raiz de agregado: protege a maquina de
/// estado de prazos/defesa (Lavrado → DefesaApresentada → Deferido/Indeferido, ou → Regularizado para
/// intimacao). O auto referencia a inspecao e o estabelecimento por Id; valor de multa so se aplica a auto
/// de infracao/penalidade. // TODO(M10): inscricao em divida ativa (Tributos.Contracts) quando indeferido c/ multa.
/// </summary>
public sealed class AutoVisa : AggregateRoot<AutoVisaId>, IMustHaveTenant
{
    private AutoVisa()
    {
    }

    private AutoVisa(
        AutoVisaId id,
        Guid tenantId,
        EstabelecimentoFiscalizavelId estabelecimentoId,
        InspecaoId inspecaoId,
        TipoAutoVisa tipo,
        string numero,
        string fundamentacao,
        DateOnly dataLavratura,
        DateOnly prazoFinal,
        decimal? valorMulta)
        : base(id)
    {
        TenantId = tenantId;
        EstabelecimentoFiscalizavelId = estabelecimentoId;
        InspecaoId = inspecaoId;
        Tipo = tipo;
        Numero = numero;
        Fundamentacao = fundamentacao;
        DataLavratura = dataLavratura;
        PrazoFinal = prazoFinal;
        ValorMulta = valorMulta;
        Situacao = SituacaoAutoVisa.Lavrado;
        RaiseDomainEvent(new AutoVisaLavrado(id, estabelecimentoId, tipo, prazoFinal));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Estabelecimento autuado (referencia por Id).</summary>
    public EstabelecimentoFiscalizavelId EstabelecimentoFiscalizavelId { get; private set; }

    /// <summary>Inspecao que fundamenta o auto (referencia por Id).</summary>
    public InspecaoId InspecaoId { get; private set; }

    /// <summary>Tipo do auto (intimacao/infracao/penalidade).</summary>
    public TipoAutoVisa Tipo { get; private set; }

    /// <summary>Numero do auto (controle sequencial do livro de autos da VISA).</summary>
    public string Numero { get; private set; } = string.Empty;

    /// <summary>Fundamentacao (dispositivos infringidos / pendencias intimadas).</summary>
    public string Fundamentacao { get; private set; } = string.Empty;

    /// <summary>Data de lavratura/notificacao (inicia a contagem do prazo).</summary>
    public DateOnly DataLavratura { get; private set; }

    /// <summary>Prazo final de defesa (infracao) ou de regularizacao (intimacao).</summary>
    public DateOnly PrazoFinal { get; private set; }

    /// <summary>Valor da multa (apenas auto de infracao/penalidade; nulo para intimacao).</summary>
    public decimal? ValorMulta { get; private set; }

    /// <summary>Situacao no processo administrativo (maquina de estado de prazos/defesa).</summary>
    public SituacaoAutoVisa Situacao { get; private set; }

    /// <summary>Texto da defesa apresentada (quando houver).</summary>
    public string? Defesa { get; private set; }

    /// <summary>
    /// Lava (cria) um auto a partir de uma inspecao. Auto de infracao/penalidade exige valor de multa
    /// nao negativo; intimacao nao tem multa (I-VISA-2). O prazo final deve ser posterior a lavratura.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="estabelecimentoId">Estabelecimento autuado.</param>
    /// <param name="inspecaoId">Inspecao fundante.</param>
    /// <param name="tipo">Tipo do auto.</param>
    /// <param name="numero">Numero/controle do auto.</param>
    /// <param name="fundamentacao">Fundamentacao legal/pendencias.</param>
    /// <param name="dataLavratura">Data de lavratura.</param>
    /// <param name="prazoFinal">Prazo final de defesa/regularizacao.</param>
    /// <param name="valorMulta">Valor da multa (obrigatorio para infracao/penalidade; nulo p/ intimacao).</param>
    /// <returns>Novo <see cref="AutoVisa"/> Lavrado.</returns>
    /// <exception cref="ArgumentException">Se numero/fundamentacao forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor de multa for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o prazo nao for posterior a lavratura ou a regra de multa for violada.</exception>
    public static AutoVisa Lavrar(
        Guid tenantId,
        EstabelecimentoFiscalizavelId estabelecimentoId,
        InspecaoId inspecaoId,
        TipoAutoVisa tipo,
        string numero,
        string fundamentacao,
        DateOnly dataLavratura,
        DateOnly prazoFinal,
        decimal? valorMulta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentacao);
        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento e obrigatorio.", nameof(estabelecimentoId));
        }

        if (inspecaoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Inspecao fundante e obrigatoria.", nameof(inspecaoId));
        }

        if (prazoFinal <= dataLavratura)
        {
            throw new InvalidOperationException("O prazo final deve ser posterior a data de lavratura.");
        }

        // I-VISA-2: intimacao NAO tem multa; infracao/penalidade EXIGE valor (>= 0, pode ser 0 = advertencia).
        if (tipo == TipoAutoVisa.Intimacao && valorMulta is not null)
        {
            throw new InvalidOperationException("Auto de intimacao nao comporta multa.");
        }

        if (tipo != TipoAutoVisa.Intimacao)
        {
            if (valorMulta is null)
            {
                throw new InvalidOperationException("Auto de infracao/penalidade exige valor de multa (pode ser 0).");
            }

            ArgumentOutOfRangeException.ThrowIfNegative(valorMulta.Value);
        }

        return new AutoVisa(
            AutoVisaId.New(), tenantId, estabelecimentoId, inspecaoId, tipo, numero.Trim(),
            fundamentacao.Trim(), dataLavratura, prazoFinal, valorMulta);
    }

    /// <summary>
    /// Registra a apresentacao de defesa pelo autuado (somente auto de infracao/penalidade, enquanto
    /// Lavrado e dentro do prazo). Move para DefesaApresentada (aguardando julgamento).
    /// </summary>
    /// <param name="texto">Texto/razoes da defesa.</param>
    /// <param name="hoje">Data corrente (para aferir o prazo — nunca hardcoded).</param>
    /// <exception cref="ArgumentException">Se o texto da defesa for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver Lavrado, for intimacao ou o prazo houver expirado.</exception>
    public void ApresentarDefesa(string texto, DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        if (Situacao != SituacaoAutoVisa.Lavrado)
        {
            throw new InvalidOperationException("Defesa so e admitida enquanto o auto esta Lavrado.");
        }

        if (Tipo == TipoAutoVisa.Intimacao)
        {
            throw new InvalidOperationException("Intimacao nao comporta defesa; use a regularizacao.");
        }

        if (hoje > PrazoFinal)
        {
            throw new InvalidOperationException("Prazo de defesa expirado.");
        }

        Defesa = texto.Trim();
        Situacao = SituacaoAutoVisa.DefesaApresentada;
    }

    /// <summary>
    /// Julga o auto de infracao/penalidade (apos defesa): defere (acolhe — cancela sem penalidade) ou
    /// indefere (rejeita — mantem a penalidade). Emite <see cref="AutoVisaJulgado"/>.
    /// </summary>
    /// <param name="deferir">Verdadeiro defere (cancela); falso indefere (mantem).</param>
    /// <exception cref="InvalidOperationException">Se nao houver defesa apresentada a julgar.</exception>
    public void Julgar(bool deferir)
    {
        if (Situacao != SituacaoAutoVisa.DefesaApresentada)
        {
            throw new InvalidOperationException("So e possivel julgar auto com defesa apresentada.");
        }

        Situacao = deferir ? SituacaoAutoVisa.Deferido : SituacaoAutoVisa.Indeferido;
        RaiseDomainEvent(new AutoVisaJulgado(Id, Situacao));
    }

    /// <summary>
    /// Reconhece a regularizacao das pendencias de uma INTIMACAO dentro do prazo (sana o auto sem
    /// penalidade). Move para Regularizado. Emite <see cref="AutoVisaJulgado"/>.
    /// </summary>
    /// <param name="hoje">Data corrente (para aferir o prazo de regularizacao).</param>
    /// <exception cref="InvalidOperationException">Se nao for intimacao Lavrada ou o prazo houver expirado.</exception>
    public void Regularizar(DateOnly hoje)
    {
        if (Tipo != TipoAutoVisa.Intimacao)
        {
            throw new InvalidOperationException("Regularizacao aplica-se apenas a auto de intimacao.");
        }

        if (Situacao != SituacaoAutoVisa.Lavrado)
        {
            throw new InvalidOperationException("Intimacao ja encerrada.");
        }

        if (hoje > PrazoFinal)
        {
            throw new InvalidOperationException("Prazo de regularizacao expirado.");
        }

        Situacao = SituacaoAutoVisa.Regularizado;
        RaiseDomainEvent(new AutoVisaJulgado(Id, Situacao));
    }

    /// <summary>
    /// Encerra por REVELIA o auto cujo prazo expirou sem defesa/regularizacao: infracao/penalidade vira
    /// Indeferido (penalidade mantida); intimacao Lavrada vencida tambem e Indeferida (pendencias nao
    /// sanadas — habilita penalidade). Idempotente. Emite <see cref="AutoVisaJulgado"/> na transicao.
    /// </summary>
    /// <param name="hoje">Data corrente.</param>
    public void EncerrarPorDecursoDePrazo(DateOnly hoje)
    {
        if (Situacao != SituacaoAutoVisa.Lavrado || hoje <= PrazoFinal)
        {
            return;
        }

        Situacao = SituacaoAutoVisa.Indeferido;
        RaiseDomainEvent(new AutoVisaJulgado(Id, Situacao));
    }

    /// <summary>Indica se o auto esta em aberto (prazo correndo, sem desfecho).</summary>
    /// <returns><c>true</c> se Lavrado ou com defesa apresentada.</returns>
    public bool EstaEmAberto() => Situacao is SituacaoAutoVisa.Lavrado or SituacaoAutoVisa.DefesaApresentada;

    /// <summary>Indica se o prazo esta vencido sem desfecho (alerta de busca ativa da VISA).</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se Lavrado e fora do prazo.</returns>
    public bool PrazoVencido(DateOnly hoje) => Situacao == SituacaoAutoVisa.Lavrado && hoje > PrazoFinal;
}
