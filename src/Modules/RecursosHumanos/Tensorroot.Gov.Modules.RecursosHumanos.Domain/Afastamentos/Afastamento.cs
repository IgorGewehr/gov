using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;

/// <summary>
/// Afastamento/licenca TIPADO de um servidor (referencia <see cref="ServidorId"/> por Id — mesmo modulo),
/// com ciclo de vida proprio (Vigente -> Encerrado/Cancelado) e efeito DETERMINISTICO na folha derivado
/// da <see cref="RegraAfastamento"/> do tipo (parametrizada por tenant): o usuario escolhe o
/// <see cref="TipoAfastamento"/>; o percentual mantido pelo ente, a suspensao de proventos e a contagem
/// de tempo VEM DA REGRA (nunca digitados livremente — CLAUDE.md S7/S16). O <c>Servidor.Situacao=Afastado</c>
/// continua como espelho de "tem afastamento vigente". Raiz de agregado.
/// </summary>
public sealed class Afastamento : AggregateRoot<AfastamentoId>, IMustHaveTenant
{
    private Afastamento()
    {
    }

    private Afastamento(
        AfastamentoId id,
        Guid tenantId,
        ServidorId servidorId,
        TipoAfastamento tipo,
        DateOnly inicio,
        DateOnly? fimPrevisto,
        string? documento,
        bool suspendeProventos,
        decimal percentualRemuneracao,
        int diasPagosPeloEnte,
        bool contaTempo,
        string codigoEventoESocial)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        Tipo = tipo;
        Inicio = inicio;
        FimPrevisto = fimPrevisto;
        Documento = documento;
        SuspendeProventos = suspendeProventos;
        PercentualRemuneracao = percentualRemuneracao;
        DiasPagosPeloEnte = diasPagosPeloEnte;
        ContaTempo = contaTempo;
        CodigoEventoESocial = codigoEventoESocial;
        Situacao = SituacaoAfastamento.Vigente;
        RaiseDomainEvent(new AfastamentoRegistrado(servidorId, tipo, inicio, fimPrevisto, contaTempo));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor afastado (referencia por Id ao agregado <see cref="Servidor"/>).</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Tipo legal do afastamento (define o efeito na folha via regra).</summary>
    public TipoAfastamento Tipo { get; private set; }

    /// <summary>Data de inicio do afastamento.</summary>
    public DateOnly Inicio { get; private set; }

    /// <summary>Fim previsto (nulo quando indeterminado, ex.: auxilio-doenca).</summary>
    public DateOnly? FimPrevisto { get; private set; }

    /// <summary>Fim efetivo (gravado no encerramento/retorno).</summary>
    public DateOnly? FimEfetivo { get; private set; }

    /// <summary>Referencia do documento (atestado/portaria/laudo).</summary>
    public string? Documento { get; private set; }

    /// <summary>Snapshot da regra: se o ente suspende o provento (apos os dias pagos pelo ente).</summary>
    public bool SuspendeProventos { get; private set; }

    /// <summary>Snapshot da regra: percentual (0..100) do provento mantido pelo ente.</summary>
    public decimal PercentualRemuneracao { get; private set; }

    /// <summary>Snapshot da regra: dias iniciais pagos integral pelo ente (ex.: doenca 15d).</summary>
    public int DiasPagosPeloEnte { get; private set; }

    /// <summary>Snapshot da regra: se o periodo conta tempo de servico (estabilidade/aposentadoria).</summary>
    public bool ContaTempo { get; private set; }

    /// <summary>Snapshot da regra: codigo do evento eSocial (S-2230, etc.).</summary>
    public string CodigoEventoESocial { get; private set; } = RegraAfastamento.CodigoEventoESocialPadrao;

    /// <summary>Situacao no ciclo de vida do afastamento.</summary>
    public SituacaoAfastamento Situacao { get; private set; }

    /// <summary>
    /// Abre um afastamento tipado para o servidor, congelando o efeito da <see cref="RegraAfastamento"/>
    /// vigente do tipo (snapshot — a regra pode mudar no futuro sem reescrever o historico).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor afastado.</param>
    /// <param name="tipo">Tipo legal do afastamento.</param>
    /// <param name="inicio">Data de inicio.</param>
    /// <param name="fimPrevisto">Fim previsto (opcional; nao anterior ao inicio).</param>
    /// <param name="documento">Referencia do documento de respaldo (opcional).</param>
    /// <param name="regra">Regra vigente do tipo (parametrizada por tenant) — fonte do efeito na folha.</param>
    /// <returns>Novo <see cref="Afastamento"/> vigente.</returns>
    /// <exception cref="ArgumentNullException">Se a regra for nula.</exception>
    /// <exception cref="ArgumentException">Se a regra nao corresponder ao tipo informado.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o fim previsto for anterior ao inicio.</exception>
    public static Afastamento Abrir(
        Guid tenantId,
        ServidorId servidorId,
        TipoAfastamento tipo,
        DateOnly inicio,
        DateOnly? fimPrevisto,
        string? documento,
        RegraAfastamento regra)
    {
        ArgumentNullException.ThrowIfNull(regra);
        if (regra.Tipo != tipo)
        {
            throw new ArgumentException($"Regra de afastamento ({regra.Tipo}) nao corresponde ao tipo informado ({tipo}).", nameof(regra));
        }

        if (fimPrevisto is { } fim && fim < inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fimPrevisto), "Fim previsto nao pode ser anterior ao inicio.");
        }

        return new Afastamento(
            AfastamentoId.New(),
            tenantId,
            servidorId,
            tipo,
            inicio,
            fimPrevisto,
            string.IsNullOrWhiteSpace(documento) ? null : documento.Trim(),
            regra.SuspendeProventos,
            regra.PercentualRemuneracao,
            regra.DiasPagosPeloEnte,
            regra.ContaTempo,
            regra.CodigoEventoESocial);
    }

    /// <summary>Encerra o afastamento vigente (retorno do servidor); grava o fim efetivo.</summary>
    /// <param name="fimEfetivo">Data efetiva de retorno (nao anterior ao inicio).</param>
    /// <exception cref="InvalidOperationException">Se o afastamento nao estiver vigente.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o fim efetivo for anterior ao inicio.</exception>
    public void Encerrar(DateOnly fimEfetivo)
    {
        if (Situacao != SituacaoAfastamento.Vigente)
        {
            throw new InvalidOperationException($"So afastamento Vigente pode ser encerrado. Situacao atual: {Situacao}.");
        }

        if (fimEfetivo < Inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fimEfetivo), "Fim efetivo nao pode ser anterior ao inicio.");
        }

        FimEfetivo = fimEfetivo;
        Situacao = SituacaoAfastamento.Encerrado;
        RaiseDomainEvent(new AfastamentoEncerrado(ServidorId, Tipo, Inicio, fimEfetivo));
    }

    /// <summary>Cancela o afastamento (lancado por engano/revogado); deixa de ter efeito na folha.</summary>
    /// <param name="motivo">Motivo do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o afastamento ja estiver encerrado/cancelado.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoAfastamento.Vigente)
        {
            throw new InvalidOperationException($"So afastamento Vigente pode ser cancelado. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoAfastamento.Cancelado;
    }

    /// <summary>
    /// Indica se o afastamento esta ativo (vigente ou encerrado) e sobrepoe QUALQUER dia do intervalo
    /// informado. Afastamento cancelado nunca incide. Usa o fim efetivo quando ha; senao o previsto;
    /// senao considera indeterminado (em aberto a partir do inicio).
    /// </summary>
    /// <param name="periodoInicio">Inicio do intervalo de competencia.</param>
    /// <param name="periodoFim">Fim do intervalo de competencia.</param>
    /// <returns><c>true</c> se ha sobreposicao.</returns>
    public bool VigenteEm(DateOnly periodoInicio, DateOnly periodoFim)
    {
        if (Situacao == SituacaoAfastamento.Cancelado)
        {
            return false;
        }

        var fim = FimEfetivo ?? FimPrevisto;
        // Sobreposicao de [Inicio, fim?] com [periodoInicio, periodoFim].
        return Inicio <= periodoFim && (fim is null || fim >= periodoInicio);
    }

    /// <summary>
    /// Projeta o <see cref="EfeitoFolhaAfastamento"/> deste afastamento sobre a competencia: conta os dias
    /// de sobreposicao com o mes e expoe os parametros de efeito (do snapshot da regra). Afastamento que
    /// nao incide na competencia retorna efeito NEUTRO.
    /// </summary>
    /// <param name="competencia">Competencia da folha.</param>
    /// <returns>Efeito a aplicar ao provento-base do mes.</returns>
    public EfeitoFolhaAfastamento EfeitoNa(Competencia competencia)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var primeiroDia = new DateOnly(competencia.Ano, competencia.Mes, 1);
        var diasNoMes = DateTime.DaysInMonth(competencia.Ano, competencia.Mes);
        var ultimoDia = new DateOnly(competencia.Ano, competencia.Mes, diasNoMes);

        if (!VigenteEm(primeiroDia, ultimoDia))
        {
            return EfeitoFolhaAfastamento.Neutro(diasNoMes);
        }

        var fim = FimEfetivo ?? FimPrevisto ?? ultimoDia;
        var inicioEfetivo = Inicio > primeiroDia ? Inicio : primeiroDia;
        var fimEfetivo = fim < ultimoDia ? fim : ultimoDia;
        var diasAfastados = fimEfetivo.DayNumber - inicioEfetivo.DayNumber + 1;
        if (diasAfastados < 0)
        {
            diasAfastados = 0;
        }

        // Os "dias pagos pelo ente" (ex.: doenca 15d) sao contados do INICIO do afastamento; em meses
        // posteriores ao inicio eles ja foram consumidos. So vale a parte do credito que cai NESTE mes.
        var diasDecorridosAntesDoMes = Math.Max(0, primeiroDia.DayNumber - Inicio.DayNumber);
        var creditoEnteRestante = Math.Max(0, DiasPagosPeloEnte - diasDecorridosAntesDoMes);
        var diasPagosPeloEnteNoMes = Math.Min(creditoEnteRestante, diasAfastados);

        return new EfeitoFolhaAfastamento(
            diasAfastados,
            diasNoMes,
            diasPagosPeloEnteNoMes,
            SuspendeProventos,
            PercentualRemuneracao);
    }
}
