using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="FundoMunicipalSaude"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FundoMunicipalSaudeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FundoMunicipalSaudeId"/>.</returns>
    public static FundoMunicipalSaudeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de <see cref="ContaBlocoFinanciamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContaBlocoFinanciamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContaBlocoFinanciamentoId"/>.</returns>
    public static ContaBlocoFinanciamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Conta-corrente por <b>bloco de financiamento</b> dentro do <see cref="FundoMunicipalSaude"/>: espelha
/// as <b>parcelas recebidas</b> do FNS e a <b>execução</b> daquele bloco, mantendo o saldo segregado.
/// Cada bloco amarra uma <b>fonte/recurso vinculado</b> (PCASP) — o recurso é executado dentro do bloco.
/// <para>
/// É <b>entidade-filha</b> (owned) do agregado FMS: NÃO implementa <c>IMustHaveTenant</c> — o isolamento
/// por tenant é herdado do dono via FK (owned types não admitem Global Query Filter próprio no EF Core).
/// </para>
/// </summary>
public sealed class ContaBlocoFinanciamento : Entity<ContaBlocoFinanciamentoId>
{
    private ContaBlocoFinanciamento()
    {
    }

    private ContaBlocoFinanciamento(
        ContaBlocoFinanciamentoId id,
        FundoMunicipalSaudeId fundoId,
        BlocoFinanciamentoSaude bloco,
        string fonteRecurso)
        : base(id)
    {
        FundoId = fundoId;
        Bloco = bloco;
        FonteRecurso = fonteRecurso;
        TotalRecebido = 0m;
        TotalExecutado = 0m;
    }

    /// <summary>Fundo ao qual a conta pertence.</summary>
    public FundoMunicipalSaudeId FundoId { get; private set; }

    /// <summary>Bloco de financiamento (Custeio/Investimento).</summary>
    public BlocoFinanciamentoSaude Bloco { get; private set; }

    /// <summary>Fonte/destinação de recurso (PCASP) vinculada ao bloco — segrega a execução.</summary>
    public string FonteRecurso { get; private set; } = default!;

    /// <summary>Total de parcelas recebidas do FNS no bloco (&gt;= 0).</summary>
    public decimal TotalRecebido { get; private set; }

    /// <summary>Total executado (empenhado/pago) no bloco (&gt;= 0).</summary>
    public decimal TotalExecutado { get; private set; }

    /// <summary>Saldo disponível no bloco (recebido - executado).</summary>
    public decimal Saldo => TotalRecebido - TotalExecutado;

    internal static ContaBlocoFinanciamento Criar(
        FundoMunicipalSaudeId fundoId,
        BlocoFinanciamentoSaude bloco,
        string fonteRecurso)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fonteRecurso);
        return new ContaBlocoFinanciamento(ContaBlocoFinanciamentoId.New(), fundoId, bloco, fonteRecurso.Trim());
    }

    internal void Creditar(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        TotalRecebido += valor;
    }

    internal void Executar(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        // Vedada a transposição livre entre blocos: não se executa além do recebido no próprio bloco.
        if (valor > Saldo)
        {
            throw new InvalidOperationException(
                $"Execução de {valor:0.00} excede o saldo do bloco {Bloco} (saldo {Saldo:0.00}). Transposição entre blocos é vedada (Port. 3.992/2017).");
        }

        TotalExecutado += valor;
    }
}

/// <summary>
/// <b>S-2 — Fundo Municipal de Saúde (FMS).</b> Unidade gestora com <b>execução segregada por bloco de
/// financiamento federal</b> (Custeio/Manutenção e Investimento/Estruturação — Port. GM/MS 3.992/2017,
/// EC pós-2017). O recurso federal entra <b>por bloco</b> e é executado <b>dentro do bloco</b> (vínculo
/// à fonte de recurso do PCASP), sem transposição livre. O fundo é a raiz do agregado; as
/// <see cref="ContaBlocoFinanciamento"/> são suas entidades-filhas, garantindo a fronteira de
/// consistência (saldo por bloco) num único agregado.
/// <para>
/// É o <b>coração contábil</b> da Saúde (pesquisa-saude §0/§2): alimenta a apuração das ASPS (S-1) e a
/// prestação de contas fundo a fundo. Não duplica o dado do PCASP (Finanças é a fonte) — espelha a
/// execução por bloco para o controle setorial.
/// </para>
/// // TODO(validar-oficial): códigos/leiaute de parcelas FNS por bloco e a taxonomia de componentes APS
/// (Port. 3.493/2024) dependem do extrato/manual oficial — o vínculo por bloco+fonte já está modelado.
/// </summary>
public sealed class FundoMunicipalSaude : AggregateRoot<FundoMunicipalSaudeId>, IMustHaveTenant
{
    private readonly List<ContaBlocoFinanciamento> _contas = [];

    private FundoMunicipalSaude()
    {
    }

    private FundoMunicipalSaude(FundoMunicipalSaudeId id, Guid tenantId, string nome, string cnpj)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Cnpj = cnpj;
    }

    /// <summary>Tenant (município) dono do fundo.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome da unidade gestora (ex.: "Fundo Municipal de Saúde de Maximiliano de Almeida").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>CNPJ da unidade gestora do FMS.</summary>
    public string Cnpj { get; private set; } = default!;

    /// <summary>Contas-corrente por bloco de financiamento (execução segregada).</summary>
    public IReadOnlyCollection<ContaBlocoFinanciamento> Contas => _contas.AsReadOnly();

    /// <summary>Cria a unidade gestora do FMS (sem blocos; os blocos são abertos sob demanda).</summary>
    /// <param name="tenantId">Tenant dono do fundo.</param>
    /// <param name="nome">Nome da unidade gestora.</param>
    /// <param name="cnpj">CNPJ da unidade gestora.</param>
    /// <returns>Novo <see cref="FundoMunicipalSaude"/>.</returns>
    public static FundoMunicipalSaude Criar(Guid tenantId, string nome, string cnpj)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpj);
        return new FundoMunicipalSaude(FundoMunicipalSaudeId.New(), tenantId, nome.Trim(), cnpj.Trim());
    }

    /// <summary>
    /// Obtém (criando se ainda não existe) a conta do bloco com a fonte de recurso informada. Garante
    /// uma única conta por (bloco, fonte) — o invariante de segregação vive no agregado.
    /// </summary>
    private ContaBlocoFinanciamento ObterOuAbrirConta(BlocoFinanciamentoSaude bloco, string fonteRecurso)
    {
        var fonte = fonteRecurso?.Trim() ?? throw new ArgumentNullException(nameof(fonteRecurso));
        var conta = _contas.Find(c => c.Bloco == bloco && string.Equals(c.FonteRecurso, fonte, StringComparison.Ordinal));
        if (conta is null)
        {
            conta = ContaBlocoFinanciamento.Criar(Id, bloco, fonte);
            _contas.Add(conta);
        }

        return conta;
    }

    /// <summary>
    /// Registra o recebimento de uma parcela do FNS no bloco/fonte informados (abre a conta se preciso).
    /// </summary>
    /// <param name="bloco">Bloco de financiamento da parcela.</param>
    /// <param name="fonteRecurso">Fonte/destinação de recurso (PCASP) vinculada.</param>
    /// <param name="valor">Valor recebido (&gt; 0).</param>
    public void ReceberParcela(BlocoFinanciamentoSaude bloco, string fonteRecurso, decimal valor)
        => ObterOuAbrirConta(bloco, fonteRecurso).Creditar(valor);

    /// <summary>
    /// Registra a execução (empenho/pagamento) de despesa no bloco/fonte informados, respeitando o saldo
    /// segregado do bloco (transposição entre blocos é vedada).
    /// </summary>
    /// <param name="bloco">Bloco de financiamento.</param>
    /// <param name="fonteRecurso">Fonte/destinação de recurso (PCASP) vinculada.</param>
    /// <param name="valor">Valor executado (&gt; 0).</param>
    public void ExecutarDespesa(BlocoFinanciamentoSaude bloco, string fonteRecurso, decimal valor)
        => ObterOuAbrirConta(bloco, fonteRecurso).Executar(valor);

    /// <summary>Saldo disponível de um bloco (soma das fontes do bloco).</summary>
    /// <param name="bloco">Bloco de financiamento.</param>
    /// <returns>Saldo do bloco (recebido - executado).</returns>
    public decimal SaldoDoBloco(BlocoFinanciamentoSaude bloco)
        => _contas.Where(c => c.Bloco == bloco).Sum(c => c.Saldo);

    /// <summary>Total recebido em um bloco (soma das fontes do bloco).</summary>
    /// <param name="bloco">Bloco de financiamento.</param>
    /// <returns>Total recebido no bloco.</returns>
    public decimal RecebidoDoBloco(BlocoFinanciamentoSaude bloco)
        => _contas.Where(c => c.Bloco == bloco).Sum(c => c.TotalRecebido);

    /// <summary>Total executado em um bloco (soma das fontes do bloco).</summary>
    /// <param name="bloco">Bloco de financiamento.</param>
    /// <returns>Total executado no bloco.</returns>
    public decimal ExecutadoDoBloco(BlocoFinanciamentoSaude bloco)
        => _contas.Where(c => c.Bloco == bloco).Sum(c => c.TotalExecutado);
}
