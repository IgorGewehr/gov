using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

/// <summary>Identificador forte do agregado <see cref="TabelaTemporalidade"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaTemporalidadeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaTemporalidadeId"/>.</returns>
    public static TabelaTemporalidadeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="RegraTemporalidade"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegraTemporalidadeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegraTemporalidadeId"/>.</returns>
    public static RegraTemporalidadeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tabela de Temporalidade e Destinacao (TTD) do tenant (e-ARQ v2 / Res. CONARQ): uma
/// <see cref="RegraTemporalidade"/> por codigo de classificacao, com os prazos de guarda e a
/// destinacao final. Parametrizavel por tenant (cada municipio tem sua TTD aprovada). Raiz de
/// agregado. I-T6: prazos/destinacao vem SEMPRE daqui — zero hardcode no calculo.
/// </summary>
public sealed class TabelaTemporalidade : AggregateRoot<TabelaTemporalidadeId>, IMustHaveTenant
{
    private readonly List<RegraTemporalidade> _regras = [];

    private TabelaTemporalidade()
    {
    }

    private TabelaTemporalidade(TabelaTemporalidadeId id, Guid tenantId, string nome, bool ativa)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Ativa = ativa;
    }

    /// <summary>Tenant (ente publico) dono da TTD.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome/identificacao da TTD.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Indica se e a TTD ATIVA do tenant (o motor resolve a regra a partir da ativa).</summary>
    public bool Ativa { get; private set; }

    /// <summary>Regras de temporalidade (entidade interna, uma por codigo de classificacao).</summary>
    public IReadOnlyList<RegraTemporalidade> Regras => _regras;

    /// <summary>Cria uma nova TTD (ativa por padrao) do tenant.</summary>
    /// <param name="tenantId">Tenant dono da TTD.</param>
    /// <param name="nome">Nome da TTD.</param>
    /// <returns>Nova <see cref="TabelaTemporalidade"/> ativa.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    public static TabelaTemporalidade Criar(Guid tenantId, string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new TabelaTemporalidade(TabelaTemporalidadeId.New(), tenantId, nome.Trim(), ativa: true);
    }

    /// <summary>
    /// Adiciona uma regra de temporalidade (uma por codigo de classificacao). Rejeita duplicata.
    /// </summary>
    /// <param name="codigoClassificacao">Codigo da classe a que a regra se aplica.</param>
    /// <param name="prazoGuardaCorrenteAnos">Prazo da fase corrente (anos, &gt;= 0).</param>
    /// <param name="prazoGuardaIntermediariaAnos">Prazo da fase intermediaria (anos, &gt;= 0).</param>
    /// <param name="destinacaoFinal">Destinacao final (eliminacao/guarda permanente).</param>
    /// <param name="eventoContagem">Evento base da contagem.</param>
    /// <param name="observacao">Norma-fonte (auditabilidade ao TCE — I-T6).</param>
    /// <exception cref="InvalidOperationException">Se o codigo ja tiver regra na TTD.</exception>
    public void AdicionarRegra(
        string codigoClassificacao,
        int prazoGuardaCorrenteAnos,
        int prazoGuardaIntermediariaAnos,
        Destinacao destinacaoFinal,
        EventoContagem eventoContagem,
        string? observacao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoClassificacao);
        var codigo = codigoClassificacao.Trim();
        if (_regras.Any(regra => string.Equals(regra.CodigoClassificacao, codigo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Ja existe regra de temporalidade para a classe '{codigo}'.");
        }

        _regras.Add(RegraTemporalidade.Criar(
            codigo, prazoGuardaCorrenteAnos, prazoGuardaIntermediariaAnos, destinacaoFinal, eventoContagem, observacao));
    }

    /// <summary>
    /// Resolve a regra de temporalidade para um codigo de classificacao, ou nulo se ausente.
    /// </summary>
    /// <param name="codigoClassificacao">Codigo da classe.</param>
    /// <returns>A regra correspondente, ou <c>null</c>.</returns>
    public RegraTemporalidade? ResolverRegra(string codigoClassificacao)
    {
        if (string.IsNullOrWhiteSpace(codigoClassificacao))
        {
            return null;
        }

        var codigo = codigoClassificacao.Trim();
        return _regras.FirstOrDefault(regra => string.Equals(regra.CodigoClassificacao, codigo, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Desativa a TTD (ex.: ao publicar nova versao).</summary>
    public void Desativar() => Ativa = false;
}

/// <summary>
/// Regra de temporalidade (entidade interna da <see cref="TabelaTemporalidade"/>): os prazos de guarda
/// CORRENTE e INTERMEDIARIA, a destinacao final e o evento base da contagem para uma classe documental.
/// Carrega a <see cref="Observacao"/> = norma-fonte (auditabilidade ao TCE — I-T6).
/// </summary>
public sealed class RegraTemporalidade : Entity<RegraTemporalidadeId>
{
    /// <summary>Comprimento maximo da observacao (norma-fonte).</summary>
    public const int ComprimentoMaximoObservacao = 500;

    private RegraTemporalidade()
    {
    }

    private RegraTemporalidade(
        RegraTemporalidadeId id,
        string codigoClassificacao,
        int prazoGuardaCorrenteAnos,
        int prazoGuardaIntermediariaAnos,
        Destinacao destinacaoFinal,
        EventoContagem eventoContagem,
        string? observacao)
        : base(id)
    {
        CodigoClassificacao = codigoClassificacao;
        PrazoGuardaCorrenteAnos = prazoGuardaCorrenteAnos;
        PrazoGuardaIntermediariaAnos = prazoGuardaIntermediariaAnos;
        DestinacaoFinal = destinacaoFinal;
        EventoContagem = eventoContagem;
        Observacao = observacao;
    }

    /// <summary>Codigo de classificacao a que a regra se aplica.</summary>
    public string CodigoClassificacao { get; private set; } = default!;

    /// <summary>Prazo de guarda na fase corrente (anos).</summary>
    public int PrazoGuardaCorrenteAnos { get; private set; }

    /// <summary>Prazo de guarda na fase intermediaria (anos).</summary>
    public int PrazoGuardaIntermediariaAnos { get; private set; }

    /// <summary>Destinacao final (eliminacao/guarda permanente).</summary>
    public Destinacao DestinacaoFinal { get; private set; }

    /// <summary>Evento base da contagem dos prazos.</summary>
    public EventoContagem EventoContagem { get; private set; }

    /// <summary>Norma-fonte da regra (auditabilidade ao TCE).</summary>
    public string? Observacao { get; private set; }

    /// <summary>Cria uma regra de temporalidade validada.</summary>
    /// <param name="codigoClassificacao">Codigo da classe.</param>
    /// <param name="prazoGuardaCorrenteAnos">Prazo corrente (anos, &gt;= 0).</param>
    /// <param name="prazoGuardaIntermediariaAnos">Prazo intermediaria (anos, &gt;= 0).</param>
    /// <param name="destinacaoFinal">Destinacao final.</param>
    /// <param name="eventoContagem">Evento base.</param>
    /// <param name="observacao">Norma-fonte (opcional).</param>
    /// <returns>Nova <see cref="RegraTemporalidade"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum prazo for negativo.</exception>
    /// <exception cref="ArgumentException">Se a observacao exceder o limite.</exception>
    public static RegraTemporalidade Criar(
        string codigoClassificacao,
        int prazoGuardaCorrenteAnos,
        int prazoGuardaIntermediariaAnos,
        Destinacao destinacaoFinal,
        EventoContagem eventoContagem,
        string? observacao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoClassificacao);
        ArgumentOutOfRangeException.ThrowIfNegative(prazoGuardaCorrenteAnos);
        ArgumentOutOfRangeException.ThrowIfNegative(prazoGuardaIntermediariaAnos);
        var observacaoNorm = observacao?.Trim();
        if (observacaoNorm is not null && observacaoNorm.Length > ComprimentoMaximoObservacao)
        {
            throw new ArgumentException($"Observacao excede {ComprimentoMaximoObservacao} caracteres.", nameof(observacao));
        }

        return new RegraTemporalidade(
            RegraTemporalidadeId.New(),
            codigoClassificacao.Trim(),
            prazoGuardaCorrenteAnos,
            prazoGuardaIntermediariaAnos,
            destinacaoFinal,
            eventoContagem,
            observacaoNorm);
    }
}
