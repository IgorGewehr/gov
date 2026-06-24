using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

/// <summary>Identificador forte do agregado <see cref="DestinacaoProcesso"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DestinacaoProcessoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DestinacaoProcessoId"/>.</returns>
    public static DestinacaoProcessoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Ficha de destinacao de um processo arquivado (e-ARQ v2 / CONARQ): a DECISAO calculada e registrada
/// das fases de guarda (corrente/intermediaria) e da destinacao final. Protege o IRREVERSIVEL pelas
/// invariantes I-T1..I-T7: nao elimina antes do prazo; guarda permanente nunca elimina; eliminacao so
/// de processo arquivado, com ato humano + termo assinado/carimbado sob WORM; estado terminal nao
/// retrocede. Raiz de agregado, multi-tenant.
/// </summary>
public sealed class DestinacaoProcesso : AggregateRoot<DestinacaoProcessoId>, IMustHaveTenant
{
    private DestinacaoProcesso()
    {
    }

    private DestinacaoProcesso(
        DestinacaoProcessoId id,
        Guid tenantId,
        Guid processoId,
        string codigoClassificacao,
        DateOnly fimGuardaCorrente,
        DateOnly fimGuardaIntermediaria,
        Destinacao destinacao,
        DateOnly? dataAptidaoEliminacao,
        EstadoDestinacao estado)
        : base(id)
    {
        TenantId = tenantId;
        ProcessoId = processoId;
        CodigoClassificacao = codigoClassificacao;
        FimGuardaCorrente = fimGuardaCorrente;
        FimGuardaIntermediaria = fimGuardaIntermediaria;
        DestinacaoFinal = destinacao;
        DataAptidaoEliminacao = dataAptidaoEliminacao;
        Estado = estado;
    }

    /// <summary>Tenant (ente publico) dono da ficha.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Processo arquivado a que a destinacao se refere.</summary>
    public Guid ProcessoId { get; private set; }

    /// <summary>Codigo de classificacao que resolveu a regra de temporalidade.</summary>
    public string CodigoClassificacao { get; private set; } = default!;

    /// <summary>Fim da fase de guarda corrente.</summary>
    public DateOnly FimGuardaCorrente { get; private set; }

    /// <summary>Fim da fase de guarda intermediaria.</summary>
    public DateOnly FimGuardaIntermediaria { get; private set; }

    /// <summary>Destinacao final (eliminacao/guarda permanente).</summary>
    public Destinacao DestinacaoFinal { get; private set; }

    /// <summary>Data a partir da qual a eliminacao e permitida (nula em guarda permanente).</summary>
    public DateOnly? DataAptidaoEliminacao { get; private set; }

    /// <summary>Estado da ficha no ciclo de vida da destinacao.</summary>
    public EstadoDestinacao Estado { get; private set; }

    /// <summary>Quem autorizou a eliminacao (RBAC); nulo enquanto nao autorizada.</summary>
    public Guid? AutorizadoPor { get; private set; }

    /// <summary>Hash do termo de eliminacao (assinado/carimbado); nulo enquanto nao eliminado (I-T4).</summary>
    public string? TermoEliminacaoHash { get; private set; }

    /// <summary>Referencia ao edital de eliminacao (Res. CONARQ 40/2014); nula enquanto nao eliminado.</summary>
    public string? EditalEliminacaoRef { get; private set; }

    /// <summary>Data efetiva da eliminacao; nula enquanto nao eliminado.</summary>
    public DateOnly? DataEliminacao { get; private set; }

    /// <summary>
    /// Cria a ficha de destinacao a partir do plano calculado pelo motor de temporalidade.
    /// I-T3: o processo deve estar arquivado (garantido pelo gatilho de <c>Processo.Arquivar</c>).
    /// O estado inicial e <see cref="EstadoDestinacao.Permanente"/> em guarda permanente, ou
    /// <see cref="EstadoDestinacao.AguardandoPrazo"/> em eliminacao.
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="processoId">Processo arquivado.</param>
    /// <param name="codigoClassificacao">Codigo da classe.</param>
    /// <param name="fimGuardaCorrente">Fim da fase corrente.</param>
    /// <param name="fimGuardaIntermediaria">Fim da fase intermediaria.</param>
    /// <param name="destinacao">Destinacao final.</param>
    /// <param name="dataAptidaoEliminacao">Data de aptidao (nula em guarda permanente).</param>
    /// <returns>Nova <see cref="DestinacaoProcesso"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o processo nao for informado.</exception>
    /// <exception cref="ArgumentException">Se o codigo de classificacao for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se guarda permanente tiver data de aptidao (incoerente).</exception>
    public static DestinacaoProcesso Criar(
        Guid tenantId,
        Guid processoId,
        string codigoClassificacao,
        DateOnly fimGuardaCorrente,
        DateOnly fimGuardaIntermediaria,
        Destinacao destinacao,
        DateOnly? dataAptidaoEliminacao)
    {
        if (processoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(processoId), "Processo e obrigatorio.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(codigoClassificacao);

        // I-T2 (coerencia): guarda permanente nunca tem aptidao a eliminacao.
        if (destinacao == Destinacao.GuardaPermanente && dataAptidaoEliminacao is not null)
        {
            throw new InvalidOperationException("Guarda permanente nao pode ter data de aptidao a eliminacao.");
        }

        if (destinacao == Destinacao.Eliminacao && dataAptidaoEliminacao is null)
        {
            throw new InvalidOperationException("Destinacao de eliminacao exige data de aptidao.");
        }

        var estado = destinacao == Destinacao.GuardaPermanente
            ? EstadoDestinacao.Permanente
            : EstadoDestinacao.AguardandoPrazo;

        return new DestinacaoProcesso(
            DestinacaoProcessoId.New(),
            tenantId,
            processoId,
            codigoClassificacao.Trim(),
            fimGuardaCorrente,
            fimGuardaIntermediaria,
            destinacao,
            dataAptidaoEliminacao,
            estado);
    }

    /// <summary>
    /// Promove a ficha de <see cref="EstadoDestinacao.AguardandoPrazo"/> para
    /// <see cref="EstadoDestinacao.AptoEliminar"/> quando o prazo de guarda ja decorreu (hoje &gt;=
    /// aptidao). Idempotente: se ja estiver apto, nao faz nada. I-T2: guarda permanente nunca promove.
    /// </summary>
    /// <param name="hoje">Data corrente (injetada — determinismo).</param>
    /// <returns><c>true</c> se promoveu para apto nesta chamada.</returns>
    public bool AvaliarAptidao(DateOnly hoje)
    {
        if (DestinacaoFinal == Destinacao.GuardaPermanente)
        {
            return false; // I-T2: permanente jamais transita para eliminacao.
        }

        if (Estado != EstadoDestinacao.AguardandoPrazo)
        {
            return false;
        }

        if (DataAptidaoEliminacao is { } aptidao && hoje >= aptidao)
        {
            Estado = EstadoDestinacao.AptoEliminar;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Autoriza a eliminacao por ato humano (RBAC). I-T1: rejeita se o prazo nao decorreu. I-T2: guarda
    /// permanente sempre rejeita. So a partir de <see cref="EstadoDestinacao.AptoEliminar"/>.
    /// </summary>
    /// <param name="autorizadoPor">Sujeito autorizador (RBAC).</param>
    /// <param name="hoje">Data corrente (injetada).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o autorizador nao for informado.</exception>
    /// <exception cref="InvalidOperationException">Se permanente (I-T2), prazo nao decorrido (I-T1) ou estado invalido.</exception>
    public void AutorizarEliminacao(Guid autorizadoPor, DateOnly hoje)
    {
        if (autorizadoPor == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(autorizadoPor), "Autorizador e obrigatorio.");
        }

        // I-T2: guarda permanente nunca elimina.
        if (DestinacaoFinal == Destinacao.GuardaPermanente)
        {
            throw new InvalidOperationException("Guarda permanente nunca elimina (I-T2).");
        }

        // I-T1: nao eliminar antes do prazo.
        if (DataAptidaoEliminacao is not { } aptidao || hoje < aptidao)
        {
            throw new InvalidOperationException("Prazo de guarda ainda nao decorrido — eliminacao vedada (I-T1).");
        }

        if (Estado is not (EstadoDestinacao.AptoEliminar or EstadoDestinacao.AguardandoPrazo))
        {
            throw new InvalidOperationException($"Autorizacao exige ficha apta/aguardando. Estado atual: {Estado}.");
        }

        AutorizadoPor = autorizadoPor;
        Estado = EstadoDestinacao.EliminacaoAutorizada;
    }

    /// <summary>
    /// Registra a eliminacao efetiva (terminal). I-T4: exige termo assinado/carimbado (hash) + edital
    /// sob WORM — a prova oponivel ao TCE. I-T2: guarda permanente sempre rejeita. Exige autorizacao previa.
    /// </summary>
    /// <param name="termoEliminacaoHash">Hash do termo de eliminacao (assinado/carimbado).</param>
    /// <param name="editalEliminacaoRef">Referencia do edital (Res. CONARQ 40/2014).</param>
    /// <param name="dataEliminacao">Data da eliminacao.</param>
    /// <exception cref="ArgumentNullException">Se o hash do termo nao for informado (I-T4).</exception>
    /// <exception cref="ArgumentException">Se o edital nao for informado (I-T4).</exception>
    /// <exception cref="InvalidOperationException">Se permanente (I-T2) ou sem autorizacao previa.</exception>
    public void MarcarEliminado(Hash termoEliminacaoHash, string editalEliminacaoRef, DateOnly dataEliminacao)
    {
        // I-T2: guarda permanente nunca elimina (transicao inalcancavel).
        if (DestinacaoFinal == Destinacao.GuardaPermanente)
        {
            throw new InvalidOperationException("Guarda permanente nunca elimina (I-T2).");
        }

        // I-T4: rastreabilidade do irreversivel.
        ArgumentNullException.ThrowIfNull(termoEliminacaoHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(editalEliminacaoRef);

        if (Estado != EstadoDestinacao.EliminacaoAutorizada)
        {
            throw new InvalidOperationException(
                $"Eliminacao exige autorizacao previa (EliminacaoAutorizada). Estado atual: {Estado}.");
        }

        TermoEliminacaoHash = termoEliminacaoHash.Valor;
        EditalEliminacaoRef = editalEliminacaoRef.Trim();
        DataEliminacao = dataEliminacao;
        Estado = EstadoDestinacao.Eliminado; // I-T7: terminal, nao retrocede.
    }
}
