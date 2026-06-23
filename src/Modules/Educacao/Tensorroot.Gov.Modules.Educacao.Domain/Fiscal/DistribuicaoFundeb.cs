using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="DistribuicaoFundeb"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DistribuicaoFundebId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DistribuicaoFundebId"/>.</returns>
    public static DistribuicaoFundebId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de <see cref="ContaOrigemFundeb"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContaOrigemFundebId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContaOrigemFundebId"/>.</returns>
    public static ContaOrigemFundebId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Conta de recebimento do FUNDEB por <b>origem</b> (cota-parte, VAAF, VAAT, VAAR) dentro da
/// <see cref="DistribuicaoFundeb"/>: espelha o <b>recebido</b> e o <b>esperado</b> daquela origem,
/// permitindo a <b>conciliação</b> (o município não recalcula a cota — comprova o recebido contra o
/// esperado divulgado pelo FNDE/Estado). Espelha a <c>ContaBlocoFinanciamento</c> da Saúde.
/// <para>
/// É <b>entidade-filha</b> (owned) do agregado: NÃO implementa <c>IMustHaveTenant</c> — o isolamento por
/// tenant é herdado do dono via FK (owned types não admitem Global Query Filter próprio no EF Core).
/// </para>
/// </summary>
public sealed class ContaOrigemFundeb : Entity<ContaOrigemFundebId>
{
    private ContaOrigemFundeb()
    {
    }

    private ContaOrigemFundeb(
        ContaOrigemFundebId id,
        DistribuicaoFundebId distribuicaoId,
        OrigemRecursoFundeb origem,
        decimal valorEsperado)
        : base(id)
    {
        DistribuicaoId = distribuicaoId;
        Origem = origem;
        ValorEsperado = valorEsperado;
        TotalRecebido = 0m;
    }

    /// <summary>Distribuição à qual a conta pertence.</summary>
    public DistribuicaoFundebId DistribuicaoId { get; private set; }

    /// <summary>Origem do recurso (cota-parte / VAAF / VAAT / VAAR).</summary>
    public OrigemRecursoFundeb Origem { get; private set; }

    /// <summary>Valor esperado para a origem (divulgado pelo FNDE/Estado) — base da conciliação.</summary>
    public decimal ValorEsperado { get; private set; }

    /// <summary>Total efetivamente recebido na origem (soma das parcelas), &gt;= 0.</summary>
    public decimal TotalRecebido { get; private set; }

    /// <summary>Divergência de conciliação na origem (recebido - esperado; negativo = a receber).</summary>
    public decimal Divergencia => TotalRecebido - ValorEsperado;

    internal static ContaOrigemFundeb Criar(DistribuicaoFundebId distribuicaoId, OrigemRecursoFundeb origem, decimal valorEsperado)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valorEsperado);
        return new ContaOrigemFundeb(ContaOrigemFundebId.New(), distribuicaoId, origem, valorEsperado);
    }

    internal void DefinirEsperado(decimal valorEsperado)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valorEsperado);
        ValorEsperado = valorEsperado;
    }

    internal void Creditar(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        TotalRecebido += valor;
    }
}

/// <summary>
/// <b>E-3 — Distribuição do FUNDEB</b> (Lei 14.113/2020; EC 108/2020), por <c>(Tenant, Exercicio)</c>.
/// Espelha a <b>estrutura de recebimento/distribuição</b> do FUNDEB: parcelas creditadas por
/// <see cref="OrigemRecursoFundeb"/> (cota-parte estadual + complementações VAAF/VAAT/VAAR) e a
/// <b>conciliação</b> recebido × esperado por origem. <b>Não recalcula a cota</b> — quem rateia/complementa
/// é o ente estadual/FNDE (insumo = matrículas ponderadas pelos fatores CIF); o município <b>concilia</b> e
/// comprova. É a raiz do agregado; as <see cref="ContaOrigemFundeb"/> são entidades-filhas (fronteira de
/// consistência num único agregado). <b>Espelha o FundoMunicipalSaude da Saúde.</b>
/// <para>
/// Liga-se a E-2: a <see cref="ReceitaFundebTotal"/> (cota-parte + complementações) é a base do indicador
/// de aplicação do piso de 70% na remuneração dos profissionais.
/// </para>
/// // TODO(validar-oficial): fatores de ponderação CIF, VAAF/VAAT/VAAR-MIN (Portaria Interministerial
/// MEC/MF) e a regra de saldo (até 10% no 1º trimestre seguinte — art. 25 da Lei 14.113/2020) dependem do
/// ato/extrato oficial — a estrutura de origens/conciliação já está modelada.
/// </summary>
public sealed class DistribuicaoFundeb : AggregateRoot<DistribuicaoFundebId>, IMustHaveTenant
{
    private readonly List<ContaOrigemFundeb> _contas = [];

    private DistribuicaoFundeb()
    {
    }

    private DistribuicaoFundeb(DistribuicaoFundebId id, Guid tenantId, int exercicio)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
    }

    /// <summary>Tenant (município) dono da distribuição.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício de referência da distribuição.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Contas de recebimento por origem (conciliação por origem).</summary>
    public IReadOnlyCollection<ContaOrigemFundeb> Contas => _contas.AsReadOnly();

    /// <summary>Cria a distribuição do FUNDEB de um exercício (sem origens; abertas sob demanda).</summary>
    /// <param name="tenantId">Tenant dono da distribuição.</param>
    /// <param name="exercicio">Exercício de referência (&gt; 0).</param>
    /// <returns>Nova <see cref="DistribuicaoFundeb"/>.</returns>
    public static DistribuicaoFundeb Criar(Guid tenantId, int exercicio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exercicio);
        return new DistribuicaoFundeb(DistribuicaoFundebId.New(), tenantId, exercicio);
    }

    private ContaOrigemFundeb ObterOuAbrirConta(OrigemRecursoFundeb origem)
    {
        var conta = _contas.Find(c => c.Origem == origem);
        if (conta is null)
        {
            conta = ContaOrigemFundeb.Criar(Id, origem, 0m);
            _contas.Add(conta);
        }

        return conta;
    }

    /// <summary>Define (ou atualiza) o valor esperado divulgado pelo FNDE/Estado para uma origem.</summary>
    /// <param name="origem">Origem do recurso.</param>
    /// <param name="valorEsperado">Valor esperado (&gt;= 0).</param>
    public void DefinirEsperado(OrigemRecursoFundeb origem, decimal valorEsperado)
        => ObterOuAbrirConta(origem).DefinirEsperado(valorEsperado);

    /// <summary>Registra o recebimento de uma parcela do FUNDEB numa origem (abre a conta se preciso).</summary>
    /// <param name="origem">Origem do recurso (cota-parte / VAAF / VAAT / VAAR).</param>
    /// <param name="valor">Valor recebido (&gt; 0).</param>
    public void ReceberParcela(OrigemRecursoFundeb origem, decimal valor)
        => ObterOuAbrirConta(origem).Creditar(valor);

    /// <summary>Total recebido numa origem.</summary>
    /// <param name="origem">Origem do recurso.</param>
    /// <returns>Total recebido na origem.</returns>
    public decimal RecebidoDaOrigem(OrigemRecursoFundeb origem)
        => _contas.Where(c => c.Origem == origem).Sum(c => c.TotalRecebido);

    /// <summary>Divergência de conciliação numa origem (recebido - esperado).</summary>
    /// <param name="origem">Origem do recurso.</param>
    /// <returns>Divergência (negativa = a receber).</returns>
    public decimal DivergenciaDaOrigem(OrigemRecursoFundeb origem)
        => _contas.Where(c => c.Origem == origem).Sum(c => c.Divergencia);

    /// <summary>Receita FUNDEB total recebida no exercício (base do indicador de 70% — E-2).</summary>
    public decimal ReceitaFundebTotal => _contas.Sum(c => c.TotalRecebido);

    /// <summary>Total esperado no exercício (soma das origens) — base da conciliação consolidada.</summary>
    public decimal EsperadoTotal => _contas.Sum(c => c.ValorEsperado);
}
