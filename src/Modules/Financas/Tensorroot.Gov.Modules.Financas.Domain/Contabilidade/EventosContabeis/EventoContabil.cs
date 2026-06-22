using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;

/// <summary>Identificador forte do agregado <see cref="EventoContabil"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EventoContabilId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EventoContabilId"/>.</returns>
    public static EventoContabilId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Grupo de linhas de lançamento resolvidas para uma natureza de informação, pronto para
/// <see cref="LancamentoContabil.Registrar"/>. Cada grupo vira um lançamento homogêneo.
/// </summary>
/// <param name="Natureza">Natureza da informação do grupo.</param>
/// <param name="Linhas">Partidas resolvidas.</param>
public readonly record struct GrupoLancamento(NaturezaInformacao Natureza, IReadOnlyList<LinhaLancamento> Linhas);

/// <summary>
/// Roteiro contábil parametrizável por tenant: mapeia um <see cref="FatoContabil"/> a um conjunto
/// de <see cref="LinhaRoteiro"/> (possivelmente de mais de uma natureza). Versionável por exercício.
/// </summary>
public sealed class EventoContabil : AggregateRoot<EventoContabilId>, IMustHaveTenant
{
    private readonly List<LinhaRoteiro> _linhas = [];

    private EventoContabil()
    {
    }

    private EventoContabil(
        EventoContabilId id,
        Guid tenantId,
        FatoContabil fato,
        string codigo,
        string nome,
        int exercicioVigenciaInicio,
        int? exercicioVigenciaFim,
        IEnumerable<LinhaRoteiro> linhas)
        : base(id)
    {
        TenantId = tenantId;
        Fato = fato;
        Codigo = codigo;
        Nome = nome;
        ExercicioVigenciaInicio = exercicioVigenciaInicio;
        ExercicioVigenciaFim = exercicioVigenciaFim;
        Ativo = true;
        _linhas.AddRange(linhas);
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Fato do ciclo coberto pelo roteiro.</summary>
    public FatoContabil Fato { get; private set; }

    /// <summary>Código do roteiro.</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Nome do roteiro.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Exercício inicial de vigência.</summary>
    public int ExercicioVigenciaInicio { get; private set; }

    /// <summary>Exercício final de vigência (ou <c>null</c> se vigente indefinidamente).</summary>
    public int? ExercicioVigenciaFim { get; private set; }

    /// <summary>Indica se o roteiro está ativo.</summary>
    public bool Ativo { get; private set; }

    /// <summary>Linhas do roteiro (somente leitura).</summary>
    public IReadOnlyCollection<LinhaRoteiro> Linhas => _linhas.AsReadOnly();

    /// <summary>
    /// Cria um roteiro validando que cada grupo de natureza está balanceado por construção
    /// (mesmo nº de débitos e créditos com a mesma base de valor).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="fato">Fato coberto.</param>
    /// <param name="codigo">Código.</param>
    /// <param name="nome">Nome.</param>
    /// <param name="exercicioVigenciaInicio">Exercício inicial.</param>
    /// <param name="exercicioVigenciaFim">Exercício final (ou <c>null</c>).</param>
    /// <param name="linhas">Linhas do roteiro.</param>
    /// <returns>Novo <see cref="EventoContabil"/>.</returns>
    /// <exception cref="RoteiroContabilInvalidoException">Se algum grupo de natureza for inválido.</exception>
    public static EventoContabil Criar(
        Guid tenantId,
        FatoContabil fato,
        string codigo,
        string nome,
        int exercicioVigenciaInicio,
        int? exercicioVigenciaFim,
        IReadOnlyCollection<LinhaRoteiro> linhas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentNullException.ThrowIfNull(linhas);
        if (linhas.Count == 0)
        {
            throw new RoteiroContabilInvalidoException($"Roteiro {codigo} nao possui linhas.");
        }

        if (exercicioVigenciaFim is { } fim && fim < exercicioVigenciaInicio)
        {
            throw new RoteiroContabilInvalidoException($"Roteiro {codigo}: vigencia final anterior ao inicio.");
        }

        foreach (var grupo in linhas.GroupBy(l => l.NaturezaInformacao))
        {
            // Como toda base é ValorDoFato (mesmo valor V), o balanceamento se reduz a #D == #C por natureza.
            var debitos = grupo.Count(l => l.Lado == Lancamentos.LadoPartida.Debito);
            var creditos = grupo.Count(l => l.Lado == Lancamentos.LadoPartida.Credito);
            if (debitos == 0 || creditos == 0 || debitos != creditos)
            {
                throw new RoteiroContabilInvalidoException(
                    $"Roteiro {codigo}: grupo {grupo.Key} desbalanceado ({debitos}D/{creditos}C).");
            }
        }

        return new EventoContabil(
            EventoContabilId.New(),
            tenantId,
            fato,
            codigo,
            nome,
            exercicioVigenciaInicio,
            exercicioVigenciaFim,
            linhas);
    }

    /// <summary>Indica se o roteiro está vigente no exercício informado.</summary>
    /// <param name="exercicio">Exercício de competência.</param>
    /// <returns><c>true</c> se vigente.</returns>
    public bool VigenteEm(int exercicio)
        => Ativo
        && exercicio >= ExercicioVigenciaInicio
        && (ExercicioVigenciaFim is null || exercicio <= ExercicioVigenciaFim);

    /// <summary>Desativa o roteiro (versionamento — preserva histórico).</summary>
    public void Desativar() => Ativo = false;

    /// <summary>
    /// Resolve o roteiro para um valor de fato, produzindo os grupos de partidas já agrupados por
    /// natureza — cada grupo pronto para virar um lançamento homogêneo e balanceado.
    /// </summary>
    /// <param name="valorFato">Valor do fato contábil.</param>
    /// <param name="mapa">Resolve cada conta (por papel ou código) para a <see cref="ContaContabil"/> real.</param>
    /// <returns>Grupos de lançamento por natureza.</returns>
    /// <exception cref="ContaNaoAnaliticaException">Se uma conta resolvida não for analítica.</exception>
    public IReadOnlyList<GrupoLancamento> ResolverPara(
        ValorMonetario valorFato,
        Func<LinhaRoteiro, ContaContabil> mapa)
    {
        ArgumentNullException.ThrowIfNull(valorFato);
        ArgumentNullException.ThrowIfNull(mapa);

        var grupos = new List<GrupoLancamento>();
        foreach (var grupo in _linhas.GroupBy(l => l.NaturezaInformacao))
        {
            var partidas = new List<LinhaLancamento>(grupo.Count());
            foreach (var linha in grupo)
            {
                var conta = mapa(linha);
                if (!conta.PodeReceberLancamento())
                {
                    throw new ContaNaoAnaliticaException(conta.Codigo.Codigo);
                }

                partidas.Add(new LinhaLancamento(
                    conta.Id,
                    conta.Codigo,
                    conta.NaturezaInformacao,
                    conta.Tipo,
                    linha.Lado,
                    valorFato));
            }

            grupos.Add(new GrupoLancamento(grupo.Key, partidas));
        }

        return grupos;
    }
}
