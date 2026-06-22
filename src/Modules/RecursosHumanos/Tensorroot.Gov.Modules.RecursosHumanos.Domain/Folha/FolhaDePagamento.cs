using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>Identificador forte do agregado <see cref="FolhaDePagamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FolhaDePagamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FolhaDePagamentoId"/>.</returns>
    public static FolhaDePagamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Folha de pagamento de um ente publico em uma competencia (<c>AAAA-MM</c>): apura proventos e
/// descontos por servidor, aplica abate-teto (CF art. 37, XI), separa remuneracao RPPS (S-1202) de
/// RGPS (S-1200) (EC 103/2019) e, ao fechar (S-1299), gera pagamentos (S-1210), totalizadores
/// (S-5001/5002/5003) e a DCTFWeb (IN RFB 2.005/2021). Raiz de agregado.
/// </summary>
public sealed class FolhaDePagamento : AggregateRoot<FolhaDePagamentoId>, IMustHaveTenant
{
    /// <summary>Codigo padrao da rubrica de abate-teto (S-1010); parametrizavel por tenant.</summary>
    public const string CodigoRubricaAbateTetoPadrao = "ABATE-TETO";

    private readonly List<EventoFolha> _eventos = [];

    private FolhaDePagamento()
    {
    }

    private FolhaDePagamento(FolhaDePagamentoId id, Guid tenantId, Competencia competencia)
        : base(id)
    {
        TenantId = tenantId;
        Competencia = competencia;
        Situacao = SituacaoFolha.Aberta;
        TotalProventos = 0m;
        TotalDescontos = 0m;
        TotalLiquido = LiquidoAPagar.Zero;
        RaiseDomainEvent(new FolhaAberta(id, competencia));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Mes/ano de referencia da folha (<c>AAAA-MM</c>).</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Situacao atual da folha (aberta/calculada/fechada/paga).</summary>
    public SituacaoFolha Situacao { get; private set; }

    /// <summary>Data do ultimo calculo (nula antes de calcular).</summary>
    public DateOnly? DataCalculo { get; private set; }

    /// <summary>Data do fechamento (nula antes de fechar).</summary>
    public DateOnly? DataFechamento { get; private set; }

    /// <summary>Data do pagamento (nula antes de pagar).</summary>
    public DateOnly? DataPagamento { get; private set; }

    /// <summary>Soma dos proventos apos calculo.</summary>
    public decimal TotalProventos { get; private set; }

    /// <summary>Soma dos descontos (inclui abate-teto) apos calculo.</summary>
    public decimal TotalDescontos { get; private set; }

    /// <summary>Total liquido (TotalProventos menos TotalDescontos), nunca negativo.</summary>
    public LiquidoAPagar TotalLiquido { get; private set; } = LiquidoAPagar.Zero;

    /// <summary>Proventos/descontos por servidor (somente leitura).</summary>
    public IReadOnlyCollection<EventoFolha> Eventos => _eventos;

    /// <summary>Abre uma folha de pagamento para uma competencia (situacao inicial <see cref="SituacaoFolha.Aberta"/> — I-14).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <returns>Nova <see cref="FolhaDePagamento"/> aberta.</returns>
    /// <exception cref="ArgumentNullException">Se a competencia for nula.</exception>
    public static FolhaDePagamento Abrir(Guid tenantId, Competencia competencia)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return new FolhaDePagamento(FolhaDePagamentoId.New(), tenantId, competencia);
    }

    /// <summary>Adiciona um evento (provento/desconto) de um servidor enquanto a folha esta aberta (I-2).</summary>
    /// <param name="servidorId">Servidor do lancamento.</param>
    /// <param name="rubrica">Rubrica (S-1010).</param>
    /// <param name="tipo">Provento ou desconto.</param>
    /// <param name="baseCalculo">Base de incidencia.</param>
    /// <param name="valor">Valor apurado (maior que zero).</param>
    /// <param name="regimePrevidenciario">Regime previdenciario herdado do servidor (I-4).</param>
    /// <returns>O <see cref="EventoFolha"/> adicionado.</returns>
    /// <exception cref="InvalidOperationException">Se a folha nao estiver aberta (I-2).</exception>
    public EventoFolha AdicionarEvento(
        Guid servidorId,
        Rubrica rubrica,
        TipoEvento tipo,
        BaseCalculo baseCalculo,
        decimal valor,
        RegimePrevidenciario regimePrevidenciario)
    {
        GarantirEditavel();
        var evento = EventoFolha.Lancar(servidorId, rubrica, tipo, baseCalculo, valor, regimePrevidenciario);
        _eventos.Add(evento);
        return evento;
    }

    /// <summary>Remove um evento da folha enquanto ela esta aberta (correcao de lancamentos — I-2).</summary>
    /// <param name="eventoId">Identificador do evento a remover.</param>
    /// <exception cref="InvalidOperationException">Se a folha nao estiver aberta ou o evento inexistir.</exception>
    public void RemoverEvento(EventoFolhaId eventoId)
    {
        GarantirEditavel();
        var evento = _eventos.Find(e => e.Id == eventoId)
            ?? throw new InvalidOperationException("Evento de folha nao encontrado.");
        _eventos.Remove(evento);
    }

    /// <summary>
    /// Remove os descontos legais (INSS/RPPS/IRRF) ja apurados de um servidor, com a folha aberta,
    /// para permitir re-apuracao idempotente pelo motor de calculo. So afeta as rubricas informadas.
    /// </summary>
    /// <param name="servidorId">Servidor cujas apuracoes legais serao removidas.</param>
    /// <param name="rubricasLegais">Codigos das rubricas de desconto legal apuradas pelo motor.</param>
    /// <exception cref="InvalidOperationException">Se a folha nao estiver aberta (I-2).</exception>
    public void RemoverDescontosLegais(Guid servidorId, IReadOnlyCollection<Rubrica> rubricasLegais)
    {
        ArgumentNullException.ThrowIfNull(rubricasLegais);
        GarantirEditavel();
        _eventos.RemoveAll(e =>
            e.ServidorId == servidorId
            && e.Tipo == TipoEvento.Desconto
            && rubricasLegais.Contains(e.Rubrica));
    }

    /// <summary>
    /// Calcula a folha: soma proventos/descontos, aplica abate-teto por servidor (I-6) e consolida o
    /// liquido (I-5). Idempotente em estado — pode ser reexecutada enquanto a folha nao estiver fechada.
    /// </summary>
    /// <param name="tetoRemuneratorio">Teto remuneratorio constitucional (CF art. 37, XI) parametrizavel.</param>
    /// <param name="hoje">Data de referencia do calculo.</param>
    /// <param name="codigoRubricaAbateTeto">Codigo da rubrica de abate-teto (parametrizavel por tenant).</param>
    /// <exception cref="InvalidOperationException">Se a folha ja estiver fechada/paga (B-7).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o teto remuneratorio nao for positivo (fail-closed — CLAUDE.md S16).</exception>
    public void Calcular(decimal tetoRemuneratorio, DateOnly hoje, string? codigoRubricaAbateTeto = null)
    {
        // FAIL-CLOSED (CLAUDE.md S16): o teto remuneratorio (CF art. 37, XI) e PARAMETRO LEGAL por
        // tenant/competencia. Um teto nao-positivo (ex.: secao de config ausente -> default 0) abateria
        // SILENCIOSAMENTE toda a remuneracao, zerando o liquido de todos os servidores — erro catastrofico
        // e auditavel pelo TCE. Recusa o calculo ate o ente configurar o teto vigente (nunca hardcoded).
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tetoRemuneratorio);
        if (Situacao is not (SituacaoFolha.Aberta or SituacaoFolha.Calculada))
        {
            throw new InvalidOperationException($"Folha nao calculavel na situacao atual: {Situacao}.");
        }

        var rubricaAbateTeto = Rubrica.De(string.IsNullOrWhiteSpace(codigoRubricaAbateTeto)
            ? CodigoRubricaAbateTetoPadrao
            : codigoRubricaAbateTeto);

        // Recalculo idempotente: remove abate-teto de calculos anteriores antes de reaplicar (B-6).
        _eventos.RemoveAll(e => e.Tipo == TipoEvento.Desconto && e.Rubrica == rubricaAbateTeto);

        // I-6 / CF art. 37, XI: lanca rubrica de abate-teto por servidor cujos proventos excedem o teto.
        foreach (var servidorId in _eventos.Select(e => e.ServidorId).Distinct().ToList())
        {
            var proventosServidor = _eventos
                .Where(e => e.ServidorId == servidorId && e.Tipo == TipoEvento.Provento)
                .Sum(e => e.Valor);

            // B-5: nenhum abate-teto quando proventos == teto (apenas quando excede).
            if (proventosServidor <= tetoRemuneratorio)
            {
                continue;
            }

            var excesso = proventosServidor - tetoRemuneratorio;
            var regime = _eventos.First(e => e.ServidorId == servidorId).RegimePrevidenciario;
            _eventos.Add(EventoFolha.Lancar(
                servidorId,
                rubricaAbateTeto,
                TipoEvento.Desconto,
                BaseCalculo.De(proventosServidor),
                excesso,
                regime));
        }

        TotalProventos = _eventos.Where(e => e.Tipo == TipoEvento.Provento).Sum(e => e.Valor);
        TotalDescontos = _eventos.Where(e => e.Tipo == TipoEvento.Desconto).Sum(e => e.Valor);

        // I-5 / B-10: liquido nao-negativo (VO valida o piso zero).
        var liquido = TotalProventos - TotalDescontos;
        TotalLiquido = LiquidoAPagar.De(liquido < 0m ? 0m : liquido);

        Situacao = SituacaoFolha.Calculada;
        DataCalculo = hoje;
        RaiseDomainEvent(new FolhaCalculada(Id, Competencia, TotalLiquido.Valor));
    }

    /// <summary>
    /// Fecha a competencia (so a partir de <see cref="SituacaoFolha.Calculada"/> — I-7), disparando
    /// S-1299/S-1210, totalizadores e a DCTFWeb (I-8 — via integracao/Outbox).
    /// </summary>
    /// <param name="hoje">Data de referencia do fechamento.</param>
    /// <exception cref="InvalidOperationException">Se a folha nao estiver calculada (I-7 / B-8).</exception>
    public void Fechar(DateOnly hoje)
    {
        if (Situacao != SituacaoFolha.Calculada)
        {
            throw new InvalidOperationException($"O fechamento exige folha Calculada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoFolha.Fechada;
        DataFechamento = hoje;
        RaiseDomainEvent(new FolhaFechada(Id, Competencia));
    }

    /// <summary>Efetua o pagamento do liquido (so a partir de <see cref="SituacaoFolha.Fechada"/> — I-9).</summary>
    /// <param name="dataPagamento">Data da liquidacao financeira.</param>
    /// <exception cref="InvalidOperationException">Se a folha nao estiver fechada (I-9 / B-9 / I-10).</exception>
    public void EfetuarPagamento(DateOnly dataPagamento)
    {
        if (Situacao != SituacaoFolha.Fechada)
        {
            throw new InvalidOperationException($"O pagamento exige folha Fechada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoFolha.Paga;
        DataPagamento = dataPagamento;
        RaiseDomainEvent(new PagamentoEfetuado(Id, Competencia, dataPagamento));
    }

    private void GarantirEditavel()
    {
        if (Situacao != SituacaoFolha.Aberta)
        {
            throw new InvalidOperationException($"Eventos so podem ser adicionados/removidos com a folha Aberta. Situacao atual: {Situacao}.");
        }
    }
}
