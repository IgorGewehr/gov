using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

/// <summary>Identificador forte do agregado <see cref="ContaContabil"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContaContabilId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContaContabilId"/>.</returns>
    public static ContaContabilId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Conta do Plano de Contas Aplicado ao Setor Público (PCASP). A coleção de contas por tenant,
/// navegável por <see cref="ContaPaiId"/>, constitui o próprio plano (não há raiz única "PlanoDeContas").
/// Movimentação só é admitida em conta analítica (folha) e ativa.
/// </summary>
public sealed class ContaContabil : AggregateRoot<ContaContabilId>, IMustHaveTenant
{
    private ContaContabil()
    {
    }

    private ContaContabil(
        ContaContabilId id,
        Guid tenantId,
        CodigoContabil codigo,
        string titulo,
        string funcao,
        string funcionamento,
        NaturezaInformacao naturezaInformacao,
        NaturezaSaldo naturezaSaldo,
        TipoConta tipo,
        ContaContabilId? contaPaiId,
        IndicadorSuperavitFinanceiro indicadorSuperavitFinanceiro,
        bool encerramento)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Titulo = titulo;
        Funcao = funcao;
        Funcionamento = funcionamento;
        NaturezaInformacao = naturezaInformacao;
        NaturezaSaldo = naturezaSaldo;
        Tipo = tipo;
        Nivel = codigo.Nivel;
        Classe = codigo.Classe;
        ContaPaiId = contaPaiId;
        IndicadorSuperavitFinanceiro = indicadorSuperavitFinanceiro;
        Encerramento = encerramento;
        Ativa = true;
        RaiseDomainEvent(new ContaContabilCriada(id, codigo.Codigo));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Código segmentado PCASP.</summary>
    public CodigoContabil Codigo { get; private set; } = default!;

    /// <summary>Título da conta.</summary>
    public string Titulo { get; private set; } = default!;

    /// <summary>Função — o que a conta registra.</summary>
    public string Funcao { get; private set; } = default!;

    /// <summary>Funcionamento — quando debita / quando credita.</summary>
    public string Funcionamento { get; private set; } = default!;

    /// <summary>Natureza da informação (Patrimonial/Orçamentária/Controle).</summary>
    public NaturezaInformacao NaturezaInformacao { get; private set; }

    /// <summary>Natureza do saldo (Devedora/Credora/Mista).</summary>
    public NaturezaSaldo NaturezaSaldo { get; private set; }

    /// <summary>Tipo (Sintética/Analítica).</summary>
    public TipoConta Tipo { get; private set; }

    /// <summary>Nível hierárquico (1 a 7).</summary>
    public int Nivel { get; private set; }

    /// <summary>Classe (1º dígito, 1 a 8).</summary>
    public int Classe { get; private set; }

    /// <summary>Conta pai (nível imediatamente superior), ou <c>null</c> no nível 1.</summary>
    public ContaContabilId? ContaPaiId { get; private set; }

    /// <summary>Indicador de superávit financeiro (F/P) — art. 105 Lei 4.320.</summary>
    public IndicadorSuperavitFinanceiro IndicadorSuperavitFinanceiro { get; private set; }

    /// <summary>Indica se o saldo encerra (zera) no fim do exercício.</summary>
    public bool Encerramento { get; private set; }

    /// <summary>Indica se a conta está ativa (contas extintas no PCASP anual ficam inativas, nunca apagadas).</summary>
    public bool Ativa { get; private set; }

    /// <summary>
    /// Cria uma conta contábil validando coerência classe↔natureza e o indicador F/P obrigatório
    /// nas classes 1 e 2.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Código contábil PCASP.</param>
    /// <param name="titulo">Título.</param>
    /// <param name="funcao">Função (descrição).</param>
    /// <param name="funcionamento">Funcionamento (débito/crédito).</param>
    /// <param name="tipo">Tipo (Sintética/Analítica).</param>
    /// <param name="contaPaiId">Conta pai (ou <c>null</c> no nível 1).</param>
    /// <param name="indicadorSuperavitFinanceiro">Indicador F/P.</param>
    /// <param name="encerramento">Se o saldo encerra no fim do exercício.</param>
    /// <param name="naturezaSaldoOverride">Sobrepõe a natureza de saldo default (contas redutoras/mistas).</param>
    /// <returns>Nova <see cref="ContaContabil"/>.</returns>
    /// <exception cref="RoteiroContabilInvalidoException">Se o indicador F/P faltar em Ativo/Passivo.</exception>
    public static ContaContabil Criar(
        Guid tenantId,
        CodigoContabil codigo,
        string titulo,
        string funcao,
        string funcionamento,
        TipoConta tipo,
        ContaContabilId? contaPaiId,
        IndicadorSuperavitFinanceiro indicadorSuperavitFinanceiro,
        bool encerramento,
        NaturezaSaldo? naturezaSaldoOverride = null)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(funcao);
        ArgumentException.ThrowIfNullOrWhiteSpace(funcionamento);

        var naturezaInformacao = codigo.NaturezaInformacaoDefault();
        var naturezaSaldo = naturezaSaldoOverride ?? codigo.NaturezaSaldoDefault();

        if (ExigeIndicadorFP(codigo.Classe) && indicadorSuperavitFinanceiro is IndicadorSuperavitFinanceiro.NaoAplicavel)
        {
            throw new RoteiroContabilInvalidoException(
                $"Conta {codigo} (classe {codigo.Classe}) exige indicador de superavit financeiro (F/P).");
        }

        if (!ExigeIndicadorFP(codigo.Classe) && indicadorSuperavitFinanceiro is not IndicadorSuperavitFinanceiro.NaoAplicavel)
        {
            throw new RoteiroContabilInvalidoException(
                $"Conta {codigo} (classe {codigo.Classe}) nao admite indicador F/P.");
        }

        return new ContaContabil(
            ContaContabilId.New(),
            tenantId,
            codigo,
            titulo,
            funcao,
            funcionamento,
            naturezaInformacao,
            naturezaSaldo,
            tipo,
            contaPaiId,
            indicadorSuperavitFinanceiro,
            encerramento);
    }

    /// <summary>Indica se a conta pode receber lançamento (analítica e ativa).</summary>
    /// <returns><c>true</c> se for folha ativa.</returns>
    public bool PodeReceberLancamento() => Ativa && Tipo == TipoConta.Analitica;

    /// <summary>Desativa (extingue) a conta no PCASP anual. Não apaga, preserva auditoria.</summary>
    public void Desativar() => Ativa = false;

    /// <summary>Reativa uma conta previamente extinta.</summary>
    public void Reativar() => Ativa = true;

    /// <summary>Indica se a classe exige indicador F/P (Ativo=1, Passivo=2).</summary>
    /// <param name="classe">Classe contábil.</param>
    /// <returns><c>true</c> para classes 1 e 2.</returns>
    public static bool ExigeIndicadorFP(int classe) => classe is 1 or 2;
}
