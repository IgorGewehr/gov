using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>
/// Concern de SUSPENSÃO (CTN art. 151) e PRESCRIÇÃO INTERCORRENTE (LEF art. 40) da Dívida Ativa
/// (R5 + CTN151) — [revisao-humana-juridica]. Partial do agregado <see cref="DividaAtiva"/>.
/// </summary>
public sealed partial class DividaAtiva
{
    /// <summary>
    /// Data da CIÊNCIA da Fazenda sobre a não-localização do devedor/inexistência de bens (LEF art. 40 +
    /// REsp 1.340.553/RS): marco do início do ano de suspensão da execução. Nulo se não suspensa.
    /// [revisao-humana-juridica]
    /// </summary>
    public DateOnly? DataSuspensaoExecucao { get; private set; }
    /// <summary>Data do despacho de arquivamento da execução (LEF art. 40 §2º). Escritural — não afeta o cálculo.</summary>
    public DateOnly? DataArquivamentoExecucao { get; private set; }
    /// <summary>Causa de suspensão da EXIGIBILIDADE em vigor (CTN art. 151), se houver. [revisao-humana-juridica]</summary>
    public CausaSuspensaoExigibilidade? CausaSuspensao { get; private set; }
    /// <summary>Data em que a exigibilidade foi suspensa (CTN art. 151).</summary>
    public DateOnly? DataSuspensaoExigibilidade { get; private set; }
    /// <summary>
    /// Dias acumulados em que a prescrição ficou PAUSADA por suspensão da exigibilidade (CTN art. 151):
    /// a suspensão pausa o prazo (que retoma de onde parou) — diferente da interrupção, que o reinicia.
    /// </summary>
    public int DiasPrescricaoSuspensos { get; private set; }
    /// <summary>Indica se a exigibilidade está suspensa por alguma causa do CTN art. 151.</summary>
    public bool ExigibilidadeSuspensa => CausaSuspensao is not null;
    /// <summary>
    /// Início da prescrição INTERCORRENTE (LEF art. 40 §4º): 1 ano após a ciência da não-localização.
    /// Nulo se a execução não foi suspensa. [revisao-humana-juridica]
    /// </summary>
    public DateOnly? DataInicioPrescricaoIntercorrente => DataSuspensaoExecucao?.AddYears(1);
    /// <summary>Data-limite da prescrição INTERCORRENTE = início + prazo parametrizado (Súmula 314/STJ).</summary>
    public DateOnly? DataPrescricaoIntercorrente => DataInicioPrescricaoIntercorrente?.AddYears(AnosPrescricaoParametrizado);
    /// <summary>
    /// Suspende a execução fiscal por 1 ano (LEF art. 40, caput/§1º) quando o devedor não é localizado ou
    /// não há bens penhoráveis. Marca a CIÊNCIA da Fazenda (REsp 1.340.553/RS): dela corre o ano de
    /// suspensão e, findo ele, a prescrição intercorrente quinquenal. [revisao-humana-juridica]
    /// </summary>
    /// <param name="dataCienciaNaoLocalizacao">Data da ciência da não-localização/inexistência de bens.</param>
    /// <exception cref="InvalidOperationException">Se a dívida não estiver em execução fiscal.</exception>
    public void SuspenderExecucao(DateOnly dataCienciaNaoLocalizacao)
    {
        if (Situacao != SituacaoDividaAtiva.EmExecucaoFiscal)
        {
            throw new InvalidOperationException($"Só se suspende (art. 40) execução fiscal em curso. Situação atual: {Situacao}.");
        }

        DataSuspensaoExecucao = dataCienciaNaoLocalizacao;
        Situacao = SituacaoDividaAtiva.ExecucaoSuspensa;
        RaiseDomainEvent(new ExecucaoFiscalSuspensa(Id, TenantId, dataCienciaNaoLocalizacao));
    }
    /// <summary>
    /// Arquiva a execução fiscal (LEF art. 40, §2º) findo o ano de suspensão. Escritural: a prescrição
    /// intercorrente já corre automaticamente desde o fim do ano de suspensão. [revisao-humana-juridica]
    /// </summary>
    /// <param name="dataArquivamento">Data do despacho de arquivamento (≥ início do trilho intercorrente).</param>
    /// <exception cref="InvalidOperationException">Se a execução não estiver suspensa.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a data anteceder o fim do ano de suspensão.</exception>
    public void ArquivarExecucao(DateOnly dataArquivamento)
    {
        if (Situacao != SituacaoDividaAtiva.ExecucaoSuspensa)
        {
            throw new InvalidOperationException($"Só se arquiva (art. 40 §2º) execução suspensa. Situação atual: {Situacao}.");
        }

        if (DataInicioPrescricaoIntercorrente is { } inicio && dataArquivamento < inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(dataArquivamento), "O arquivamento não pode anteceder o fim do ano de suspensão.");
        }

        DataArquivamentoExecucao = dataArquivamento;
        Situacao = SituacaoDividaAtiva.ExecucaoArquivada;
        RaiseDomainEvent(new ExecucaoFiscalArquivada(Id, TenantId, dataArquivamento));
    }
    /// <summary>
    /// Registra CONSTRIÇÃO patrimonial efetiva ou CITAÇÃO (marco interruptivo do trilho intercorrente —
    /// REsp 1.340.553/RS; mero peticionamento não basta): zera o trilho intercorrente, devolve a dívida à
    /// execução fiscal e interrompe a prescrição (novo termo). [revisao-humana-juridica]
    /// </summary>
    /// <param name="data">Data da constrição/citação efetiva.</param>
    /// <exception cref="InvalidOperationException">Se a execução não estiver suspensa/arquivada.</exception>
    /// <exception cref="DividaAtivaPrescritaException">Se a intercorrente já se consumou na data.</exception>
    public void RegistrarConstricaoOuCitacao(DateOnly data)
    {
        if (Situacao is not (SituacaoDividaAtiva.ExecucaoSuspensa or SituacaoDividaAtiva.ExecucaoArquivada))
        {
            throw new InvalidOperationException($"Constrição/citação intercorrente só no curso suspenso/arquivado. Situação atual: {Situacao}.");
        }

        if (EstaPrescrita(data))
        {
            throw new DividaAtivaPrescritaException(Id, DataPrescricaoIntercorrente ?? DataPrescricao, data);
        }

        DataSuspensaoExecucao = null;
        DataArquivamentoExecucao = null;
        DataUltimaInterrupcaoPrescricao = data;
        Situacao = SituacaoDividaAtiva.EmExecucaoFiscal;
        RaiseDomainEvent(new ConstricaoOuCitacaoRegistrada(Id, TenantId, data));
    }
    /// <summary>
    /// Suspende a EXIGIBILIDADE do crédito (CTN art. 151, I–V): moratória, depósito do montante integral,
    /// recurso administrativo, liminar em MS ou tutela. Pausa a prescrição e habilita CPEN (não CND). O
    /// parcelamento (inc. VI) é firmado por <see cref="FirmarParcelamento"/>. [revisao-humana-juridica]
    /// </summary>
    /// <param name="causa">Causa do art. 151 (exceto parcelamento).</param>
    /// <param name="dataSuspensao">Data em que a exigibilidade passou a estar suspensa.</param>
    /// <exception cref="InvalidOperationException">Se a dívida estiver extinta ou já suspensa.</exception>
    /// <exception cref="DividaAtivaPrescritaException">Se a dívida já estiver prescrita na data.</exception>
    public void SuspenderExigibilidade(CausaSuspensaoExigibilidade causa, DateOnly dataSuspensao)
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada)
        {
            throw new InvalidOperationException($"Dívida encerrada não comporta suspensão de exigibilidade. Situação atual: {Situacao}.");
        }

        if (ExigibilidadeSuspensa)
        {
            throw new InvalidOperationException($"A exigibilidade já está suspensa ({CausaSuspensao}).");
        }

        if (EstaPrescrita(dataSuspensao))
        {
            throw new DividaAtivaPrescritaException(Id, DataPrescricao, dataSuspensao);
        }

        CausaSuspensao = causa;
        DataSuspensaoExigibilidade = dataSuspensao;
        RaiseDomainEvent(new ExigibilidadeDividaSuspensa(Id, TenantId, causa, dataSuspensao));
    }
    /// <summary>
    /// Cessa a suspensão da exigibilidade (CTN art. 151): acumula os dias pausados (a prescrição RETOMA de
    /// onde parou, deslocada para a frente) e restabelece a exigibilidade. [revisao-humana-juridica]
    /// </summary>
    /// <param name="dataCessacao">Data em que a causa de suspensão cessou.</param>
    /// <exception cref="InvalidOperationException">Se não houver suspensão de exigibilidade vigente.</exception>
    public void CessarSuspensao(DateOnly dataCessacao)
    {
        if (!ExigibilidadeSuspensa)
        {
            throw new InvalidOperationException("Não há suspensão de exigibilidade a cessar.");
        }

        var diasPausados = dataCessacao.DayNumber - DataSuspensaoExigibilidade!.Value.DayNumber;
        if (diasPausados > 0)
        {
            DiasPrescricaoSuspensos += diasPausados;
        }

        CausaSuspensao = null;
        DataSuspensaoExigibilidade = null;
        RaiseDomainEvent(new ExigibilidadeDividaRestabelecida(Id, TenantId, dataCessacao));
    }
    /// <summary>
    /// Indica se a dívida está prescrita na data informada (CTN art. 174), pelo termo inicial efetivo
    /// (constituição definitiva ou última interrupção). Dívida suspensa/quitada/cancelada não prescreve.
    /// </summary>
    /// <param name="referencia">Data de referência (informada — sem relógio no domínio).</param>
    /// <returns><c>true</c> se prescrita.</returns>
    public bool EstaPrescrita(DateOnly referencia)
    {
        // Não corre: dívida extinta (quitada/cancelada) ou com exigibilidade SUSPENSA (CTN art. 151,
        // incluindo parcelamento). Mantém-se o teste explícito de Parcelada por retrocompatibilidade com
        // linhas legadas sem o overlay de causa.
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada or SituacaoDividaAtiva.Parcelada)
        {
            return false;
        }

        if (ExigibilidadeSuspensa)
        {
            return false;
        }

        // Trilho INTERCORRENTE (LEF art. 40 §4º + Súmula 314/STJ): suspensa/arquivada a execução por
        // não-localização, prescreve 5 anos após o fim do ano de suspensão.
        if (DataInicioPrescricaoIntercorrente is { } inicioIntercorrente)
        {
            return referencia > inicioIntercorrente.AddYears(AnosPrescricaoParametrizado);
        }

        // Trilho ORDINÁRIO (CTN art. 174), já deslocado pelos dias de suspensão da exigibilidade.
        return referencia > DataPrescricao;
    }
    /// <summary>
    /// Invariante de exigibilidade do crédito inscrito (fail-closed). Recusa a cobrança quando a dívida
    /// está suspensa/encerrada OU quando está PRESCRITA na data de referência (CTN art. 174): a prescrição
    /// extingue o próprio crédito tributário (CTN art. 156, V), de modo que protesto/execução/CDA de
    /// dívida prescrita são vedados. A data de referência é informada (data do fato — sem relógio no
    /// domínio, CLAUDE.md §16).
    /// </summary>
    /// <param name="referencia">Data de referência do ato de cobrança (data do fato).</param>
    /// <exception cref="DividaAtivaPrescritaException">Se a dívida estiver prescrita.</exception>
    /// <exception cref="InvalidOperationException">Se a dívida não estiver exigível por situação.</exception>
    private void GarantirExigivel(DateOnly referencia)
    {
        if (Situacao is SituacaoDividaAtiva.Quitada or SituacaoDividaAtiva.Cancelada or SituacaoDividaAtiva.Parcelada)
        {
            throw new InvalidOperationException($"A dívida não está exigível. Situação atual: {Situacao}.");
        }

        // CTN art. 151: enquanto suspensa a exigibilidade (moratória/depósito/recurso/liminar), são
        // vedados ajuizamento/protesto/prosseguimento da cobrança.
        if (ExigibilidadeSuspensa)
        {
            throw new InvalidOperationException($"A exigibilidade está suspensa (CTN art. 151, {CausaSuspensao}).");
        }

        if (EstaPrescrita(referencia))
        {
            throw new DividaAtivaPrescritaException(Id, DataPrescricao, referencia);
        }
    }
}
