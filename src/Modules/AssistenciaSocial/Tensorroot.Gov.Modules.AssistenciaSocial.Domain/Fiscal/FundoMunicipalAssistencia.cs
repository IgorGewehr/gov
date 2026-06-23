using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="FundoMunicipalAssistencia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FundoMunicipalAssistenciaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FundoMunicipalAssistenciaId"/>.</returns>
    public static FundoMunicipalAssistenciaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de <see cref="ContaCofinanciamentoSuas"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContaCofinanciamentoSuasId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContaCofinanciamentoSuasId"/>.</returns>
    public static ContaCofinanciamentoSuasId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Conta-corrente por <b>(bloco, piso, fonte)</b> dentro do <see cref="FundoMunicipalAssistencia"/>:
/// espelha as <b>parcelas recebidas</b> do FNAS (SUASWeb/AgilizaSUAS) e a <b>execucao</b> daquele
/// piso, mantendo o saldo segregado. Cada conta amarra uma <b>fonte/recurso vinculado</b> (PCASP).
/// <para>
/// Entidade-filha (owned) do agregado FMAS: NAO implementa <c>IMustHaveTenant</c> — o isolamento por
/// tenant e herdado do dono via FK (owned types nao admitem Global Query Filter proprio no EF Core).
/// Espelha <c>ContaBlocoFinanciamento</c> da Saude, com piso adicional (granularidade do SUAS).
/// </para>
/// </summary>
public sealed class ContaCofinanciamentoSuas : Entity<ContaCofinanciamentoSuasId>
{
    private ContaCofinanciamentoSuas()
    {
    }

    private ContaCofinanciamentoSuas(
        ContaCofinanciamentoSuasId id,
        FundoMunicipalAssistenciaId fundoId,
        BlocoFinanciamentoAssistencia bloco,
        PisoAssistencia piso,
        string fonteRecurso)
        : base(id)
    {
        FundoId = fundoId;
        Bloco = bloco;
        Piso = piso;
        FonteRecurso = fonteRecurso;
        TotalRecebido = 0m;
        TotalExecutado = 0m;
    }

    /// <summary>Fundo ao qual a conta pertence.</summary>
    public FundoMunicipalAssistenciaId FundoId { get; private set; }

    /// <summary>Bloco de cofinanciamento (PSB/PSE-MC/PSE-AC/Gestao-IGD).</summary>
    public BlocoFinanciamentoAssistencia Bloco { get; private set; }

    /// <summary>Piso de cofinanciamento dentro do bloco (servico tipificado).</summary>
    public PisoAssistencia Piso { get; private set; }

    /// <summary>Fonte/destinacao de recurso (PCASP) vinculada — segrega a execucao.</summary>
    public string FonteRecurso { get; private set; } = default!;

    /// <summary>Total de parcelas recebidas do FNAS na conta (&gt;= 0).</summary>
    public decimal TotalRecebido { get; private set; }

    /// <summary>Total executado (empenhado/pago) na conta (&gt;= 0).</summary>
    public decimal TotalExecutado { get; private set; }

    /// <summary>Saldo disponivel na conta (recebido - executado).</summary>
    public decimal Saldo => TotalRecebido - TotalExecutado;

    internal static ContaCofinanciamentoSuas Criar(
        FundoMunicipalAssistenciaId fundoId,
        BlocoFinanciamentoAssistencia bloco,
        PisoAssistencia piso,
        string fonteRecurso)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fonteRecurso);
        return new ContaCofinanciamentoSuas(ContaCofinanciamentoSuasId.New(), fundoId, bloco, piso, fonteRecurso.Trim());
    }

    internal void Creditar(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        TotalRecebido += valor;
    }

    internal void Executar(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        // Vedada a transposicao livre entre blocos/pisos: nao se executa alem do recebido na conta.
        if (valor > Saldo)
        {
            throw new InvalidOperationException(
                $"Execucao de {valor:0.00} excede o saldo da conta {Bloco}/{Piso} (saldo {Saldo:0.00}). " +
                "Transposicao livre entre blocos/pisos do SUAS e vedada (Port. 1.043/2024).");
        }

        TotalExecutado += valor;
    }
}

/// <summary>
/// <b>A-1 — Fundo Municipal de Assistencia Social (FMAS).</b> Unidade gestora com <b>execucao segregada
/// por bloco de cofinanciamento federal do SUAS</b> (Protecao Social Basica; Especial de Media
/// Complexidade; Especial de Alta Complexidade; Gestao do SUAS/IGD — Portaria MDS nº 1.043/2024) e por
/// <b>piso</b> (Tipificacao Nacional, Res. CNAS 109/2009). O recurso federal entra <b>por bloco/piso</b>
/// e e executado <b>dentro da conta</b> (vinculo a fonte de recurso do PCASP), sem transposicao livre.
/// O fundo e a raiz do agregado; as <see cref="ContaCofinanciamentoSuas"/> sao suas entidades-filhas,
/// garantindo a fronteira de consistencia (saldo por bloco/piso) num unico agregado.
/// <para>
/// Espelha o <c>FundoMunicipalSaude</c> (FMS) da Saude — mesmo padrao de fundo por bloco + fonte, com a
/// granularidade adicional de piso exigida pelo SUAS. Nao duplica o dado do PCASP (Financas e a fonte) —
/// espelha a execucao por bloco/piso para o controle setorial e a prestacao de contas (AgilizaSUAS).
/// </para>
/// // TODO(validar-oficial): reprogramacao de saldo (facilitada pela Port. 1.043/2024) e valores de
/// pisos (tabela NOB-SUAS) dependem do extrato/manual oficial — o vinculo bloco+piso+fonte ja esta modelado.
/// </summary>
public sealed class FundoMunicipalAssistencia : AggregateRoot<FundoMunicipalAssistenciaId>, IMustHaveTenant
{
    private readonly List<ContaCofinanciamentoSuas> _contas = [];

    private FundoMunicipalAssistencia()
    {
    }

    private FundoMunicipalAssistencia(FundoMunicipalAssistenciaId id, Guid tenantId, string nome, string cnpj)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Cnpj = cnpj;
    }

    /// <summary>Tenant (municipio) dono do fundo.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome da unidade gestora (ex.: "Fundo Municipal de Assistencia Social de Maximiliano de Almeida").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>CNPJ da unidade gestora do FMAS.</summary>
    public string Cnpj { get; private set; } = default!;

    /// <summary>Contas-corrente por (bloco, piso, fonte) — execucao segregada.</summary>
    public IReadOnlyCollection<ContaCofinanciamentoSuas> Contas => _contas.AsReadOnly();

    /// <summary>Cria a unidade gestora do FMAS (sem contas; as contas sao abertas sob demanda).</summary>
    /// <param name="tenantId">Tenant dono do fundo.</param>
    /// <param name="nome">Nome da unidade gestora.</param>
    /// <param name="cnpj">CNPJ da unidade gestora.</param>
    /// <returns>Novo <see cref="FundoMunicipalAssistencia"/>.</returns>
    public static FundoMunicipalAssistencia Criar(Guid tenantId, string nome, string cnpj)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpj);
        return new FundoMunicipalAssistencia(FundoMunicipalAssistenciaId.New(), tenantId, nome.Trim(), cnpj.Trim());
    }

    private ContaCofinanciamentoSuas ObterOuAbrirConta(BlocoFinanciamentoAssistencia bloco, PisoAssistencia piso, string fonteRecurso)
    {
        var fonte = fonteRecurso?.Trim() ?? throw new ArgumentNullException(nameof(fonteRecurso));
        var conta = _contas.Find(c => c.Bloco == bloco && c.Piso == piso && string.Equals(c.FonteRecurso, fonte, StringComparison.Ordinal));
        if (conta is null)
        {
            conta = ContaCofinanciamentoSuas.Criar(Id, bloco, piso, fonte);
            _contas.Add(conta);
        }

        return conta;
    }

    /// <summary>Registra o recebimento de uma parcela do FNAS no bloco/piso/fonte (abre a conta se preciso).</summary>
    /// <param name="bloco">Bloco de cofinanciamento da parcela.</param>
    /// <param name="piso">Piso de cofinanciamento (servico tipificado).</param>
    /// <param name="fonteRecurso">Fonte/destinacao de recurso (PCASP) vinculada.</param>
    /// <param name="valor">Valor recebido (&gt; 0).</param>
    public void ReceberParcela(BlocoFinanciamentoAssistencia bloco, PisoAssistencia piso, string fonteRecurso, decimal valor)
        => ObterOuAbrirConta(bloco, piso, fonteRecurso).Creditar(valor);

    /// <summary>
    /// Registra a execucao (empenho/pagamento) de despesa no bloco/piso/fonte, respeitando o saldo
    /// segregado da conta (transposicao livre entre blocos/pisos e vedada).
    /// </summary>
    /// <param name="bloco">Bloco de cofinanciamento.</param>
    /// <param name="piso">Piso de cofinanciamento.</param>
    /// <param name="fonteRecurso">Fonte/destinacao de recurso (PCASP) vinculada.</param>
    /// <param name="valor">Valor executado (&gt; 0).</param>
    public void ExecutarDespesa(BlocoFinanciamentoAssistencia bloco, PisoAssistencia piso, string fonteRecurso, decimal valor)
        => ObterOuAbrirConta(bloco, piso, fonteRecurso).Executar(valor);

    /// <summary>Saldo disponivel de um bloco (soma de todos os pisos/fontes do bloco).</summary>
    /// <param name="bloco">Bloco de cofinanciamento.</param>
    /// <returns>Saldo do bloco (recebido - executado).</returns>
    public decimal SaldoDoBloco(BlocoFinanciamentoAssistencia bloco)
        => _contas.Where(c => c.Bloco == bloco).Sum(c => c.Saldo);

    /// <summary>Total recebido em um bloco (soma de todos os pisos/fontes do bloco).</summary>
    /// <param name="bloco">Bloco de cofinanciamento.</param>
    /// <returns>Total recebido no bloco.</returns>
    public decimal RecebidoDoBloco(BlocoFinanciamentoAssistencia bloco)
        => _contas.Where(c => c.Bloco == bloco).Sum(c => c.TotalRecebido);

    /// <summary>Total executado em um bloco (soma de todos os pisos/fontes do bloco).</summary>
    /// <param name="bloco">Bloco de cofinanciamento.</param>
    /// <returns>Total executado no bloco.</returns>
    public decimal ExecutadoDoBloco(BlocoFinanciamentoAssistencia bloco)
        => _contas.Where(c => c.Bloco == bloco).Sum(c => c.TotalExecutado);

    /// <summary>Saldo disponivel de um piso (soma das fontes do piso).</summary>
    /// <param name="piso">Piso de cofinanciamento.</param>
    /// <returns>Saldo do piso (recebido - executado).</returns>
    public decimal SaldoDoPiso(PisoAssistencia piso)
        => _contas.Where(c => c.Piso == piso).Sum(c => c.Saldo);

    /// <summary>Total recebido em um piso (soma das fontes do piso).</summary>
    /// <param name="piso">Piso de cofinanciamento.</param>
    /// <returns>Total recebido no piso.</returns>
    public decimal RecebidoDoPiso(PisoAssistencia piso)
        => _contas.Where(c => c.Piso == piso).Sum(c => c.TotalRecebido);

    /// <summary>Total executado em um piso (soma das fontes do piso).</summary>
    /// <param name="piso">Piso de cofinanciamento.</param>
    /// <returns>Total executado no piso.</returns>
    public decimal ExecutadoDoPiso(PisoAssistencia piso)
        => _contas.Where(c => c.Piso == piso).Sum(c => c.TotalExecutado);
}
