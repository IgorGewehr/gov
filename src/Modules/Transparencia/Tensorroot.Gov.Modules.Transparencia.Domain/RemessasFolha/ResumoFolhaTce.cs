using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;

/// <summary>Identificador forte do agregado <see cref="ResumoFolhaTce"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ResumoFolhaTceId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ResumoFolhaTceId"/>.</returns>
    public static ResumoFolhaTceId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Read model materializado na Transparencia (papel CONSUMIDOR) a partir do
/// <c>FolhaResumoRemessaTceIntegrationEvent</c> publicado pelo RH quando uma folha e fechada. Guarda o
/// snapshot (servidores/rubricas/lancamentos) de uma competencia, de onde a REMESSA DE FOLHA ao TCE-RS
/// (Res. 1099 / SIAPC Vol. V) e montada — sem a Transparencia tocar no interno do RH (I-13). Idempotente
/// por <c>(TenantId, FolhaDePagamentoId)</c>: reprocessar o evento atualiza o mesmo resumo.
/// </summary>
public sealed class ResumoFolhaTce : AggregateRoot<ResumoFolhaTceId>, IMustHaveTenant
{
    private readonly List<ServidorFolhaResumo> _servidores = [];
    private readonly List<RubricaFolhaResumo> _rubricas = [];
    private readonly List<LancamentoFolhaResumo> _lancamentos = [];

    private ResumoFolhaTce()
    {
    }

    private ResumoFolhaTce(
        ResumoFolhaTceId id,
        Guid tenantId,
        Guid folhaDePagamentoId,
        int exercicio,
        int mes,
        string tipoFolha,
        DateOnly? dataPagamento)
        : base(id)
    {
        TenantId = tenantId;
        FolhaDePagamentoId = folhaDePagamentoId;
        Exercicio = exercicio;
        Mes = mes;
        TipoFolha = tipoFolha;
        DataPagamento = dataPagamento;
    }

    /// <summary>Tenant (ente publico) dono do registro. <see cref="IMustHaveTenant"/>.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Folha de pagamento de origem (RH).</summary>
    public Guid FolhaDePagamentoId { get; private set; }

    /// <summary>Ano de exercicio da competencia.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Mes (1..12) da competencia.</summary>
    public int Mes { get; private set; }

    /// <summary>Tipo da folha (Mensal/DecimoTerceiro/Ferias/Rescisao).</summary>
    public string TipoFolha { get; private set; } = "Mensal";

    /// <summary>Data de pagamento da folha (nula se ainda nao paga).</summary>
    public DateOnly? DataPagamento { get; private set; }

    /// <summary>Cadastro dos servidores (TCE_4820).</summary>
    public IReadOnlyCollection<ServidorFolhaResumo> Servidores => _servidores;

    /// <summary>Tabela de rubricas (TCE_4960).</summary>
    public IReadOnlyCollection<RubricaFolhaResumo> Rubricas => _rubricas;

    /// <summary>Lancamentos por servidor (TCE_4810).</summary>
    public IReadOnlyCollection<LancamentoFolhaResumo> Lancamentos => _lancamentos;

    /// <summary>Cria o resumo de uma folha fechada com seus componentes (snapshot consumido do RH).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="folhaDePagamentoId">Folha de origem.</param>
    /// <param name="exercicio">Ano de exercicio.</param>
    /// <param name="mes">Mes (1..12).</param>
    /// <param name="tipoFolha">Tipo da folha.</param>
    /// <param name="dataPagamento">Data de pagamento (nula se ausente).</param>
    /// <param name="servidores">Servidores (TCE_4820).</param>
    /// <param name="rubricas">Rubricas (TCE_4960).</param>
    /// <param name="lancamentos">Lancamentos (TCE_4810).</param>
    /// <returns>Novo <see cref="ResumoFolhaTce"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o mes estiver fora de 1..12.</exception>
    public static ResumoFolhaTce Criar(
        Guid tenantId,
        Guid folhaDePagamentoId,
        int exercicio,
        int mes,
        string tipoFolha,
        DateOnly? dataPagamento,
        IEnumerable<ServidorFolhaResumo> servidores,
        IEnumerable<RubricaFolhaResumo> rubricas,
        IEnumerable<LancamentoFolhaResumo> lancamentos)
    {
        ArgumentNullException.ThrowIfNull(servidores);
        ArgumentNullException.ThrowIfNull(rubricas);
        ArgumentNullException.ThrowIfNull(lancamentos);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoFolha);
        ArgumentOutOfRangeException.ThrowIfLessThan(mes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mes, 12);

        var resumo = new ResumoFolhaTce(
            ResumoFolhaTceId.New(),
            tenantId,
            folhaDePagamentoId,
            exercicio,
            mes,
            tipoFolha,
            dataPagamento);
        resumo._servidores.AddRange(servidores);
        resumo._rubricas.AddRange(rubricas);
        resumo._lancamentos.AddRange(lancamentos);
        return resumo;
    }

    /// <summary>
    /// Substitui o snapshot (idempotencia I-13): ao reprocessar o evento da mesma folha, atualiza o
    /// conteudo sem criar registro duplicado.
    /// </summary>
    /// <param name="tipoFolha">Tipo da folha.</param>
    /// <param name="dataPagamento">Data de pagamento (nula se ausente).</param>
    /// <param name="servidores">Servidores (TCE_4820).</param>
    /// <param name="rubricas">Rubricas (TCE_4960).</param>
    /// <param name="lancamentos">Lancamentos (TCE_4810).</param>
    public void Substituir(
        string tipoFolha,
        DateOnly? dataPagamento,
        IEnumerable<ServidorFolhaResumo> servidores,
        IEnumerable<RubricaFolhaResumo> rubricas,
        IEnumerable<LancamentoFolhaResumo> lancamentos)
    {
        ArgumentNullException.ThrowIfNull(servidores);
        ArgumentNullException.ThrowIfNull(rubricas);
        ArgumentNullException.ThrowIfNull(lancamentos);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoFolha);

        TipoFolha = tipoFolha;
        DataPagamento = dataPagamento;
        _servidores.Clear();
        _servidores.AddRange(servidores);
        _rubricas.Clear();
        _rubricas.AddRange(rubricas);
        _lancamentos.Clear();
        _lancamentos.AddRange(lancamentos);
    }
}

/// <summary>Identificador forte de um <see cref="ServidorFolhaResumo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ServidorFolhaResumoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo identificador.</returns>
    public static ServidorFolhaResumoId New() => new(Guid.NewGuid());
}

/// <summary>Servidor do snapshot da folha (cadastro TCE_4820). Entidade-filha exposta pela raiz.</summary>
public sealed class ServidorFolhaResumo : Entity<ServidorFolhaResumoId>
{
    private ServidorFolhaResumo()
    {
    }

    private ServidorFolhaResumo(ServidorFolhaResumoId id)
        : base(id)
    {
    }

    /// <summary>Codigo de registro do funcionario (FK p/ TCE_4810).</summary>
    public string CodigoRegistro { get; private set; } = default!;

    /// <summary>CPF (somente digitos).</summary>
    public string Cpf { get; private set; } = default!;

    /// <summary>Nome civil.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Matricula (com sufixo de vinculo).</summary>
    public string Matricula { get; private set; } = default!;

    /// <summary>Data de nascimento (nula se ausente).</summary>
    public DateOnly? DataNascimento { get; private set; }

    /// <summary>Data de admissao/nomeacao.</summary>
    public DateOnly? DataAdmissao { get; private set; }

    /// <summary>Data de demissao/desligamento (nula se ativo).</summary>
    public DateOnly? DataDemissao { get; private set; }

    /// <summary>Codigo do cargo.</summary>
    public string CodigoCargo { get; private set; } = default!;

    /// <summary>Nome do cargo.</summary>
    public string NomeCargo { get; private set; } = default!;

    /// <summary>Regime previdenciario (Rpps/Rgps).</summary>
    public string Regime { get; private set; } = default!;

    /// <summary>Cria um servidor do snapshot.</summary>
    /// <param name="codigoRegistro">Codigo de registro do funcionario.</param>
    /// <param name="cpf">CPF (somente digitos).</param>
    /// <param name="nome">Nome civil.</param>
    /// <param name="matricula">Matricula.</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <param name="dataAdmissao">Data de admissao.</param>
    /// <param name="dataDemissao">Data de demissao.</param>
    /// <param name="codigoCargo">Codigo do cargo.</param>
    /// <param name="nomeCargo">Nome do cargo.</param>
    /// <param name="regime">Regime previdenciario.</param>
    /// <returns>Novo <see cref="ServidorFolhaResumo"/>.</returns>
    public static ServidorFolhaResumo Criar(
        string codigoRegistro,
        string cpf,
        string nome,
        string matricula,
        DateOnly? dataNascimento,
        DateOnly? dataAdmissao,
        DateOnly? dataDemissao,
        string codigoCargo,
        string nomeCargo,
        string regime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoRegistro);
        return new ServidorFolhaResumo(ServidorFolhaResumoId.New())
        {
            CodigoRegistro = codigoRegistro,
            Cpf = cpf ?? string.Empty,
            Nome = nome ?? string.Empty,
            Matricula = matricula ?? string.Empty,
            DataNascimento = dataNascimento,
            DataAdmissao = dataAdmissao,
            DataDemissao = dataDemissao,
            CodigoCargo = codigoCargo ?? string.Empty,
            NomeCargo = nomeCargo ?? string.Empty,
            Regime = regime ?? string.Empty,
        };
    }
}

/// <summary>Identificador forte de um <see cref="RubricaFolhaResumo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RubricaFolhaResumoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo identificador.</returns>
    public static RubricaFolhaResumoId New() => new(Guid.NewGuid());
}

/// <summary>Rubrica do snapshot da folha (TCE_4960). Entidade-filha exposta pela raiz.</summary>
public sealed class RubricaFolhaResumo : Entity<RubricaFolhaResumoId>
{
    private RubricaFolhaResumo()
    {
    }

    private RubricaFolhaResumo(RubricaFolhaResumoId id)
        : base(id)
    {
    }

    /// <summary>Codigo da rubrica (S-1010; FK p/ TCE_4810).</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Nome/descricao.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Operacao (V/D/T/O).</summary>
    public string Operacao { get; private set; } = default!;

    /// <summary>Incidencia do IRRF.</summary>
    public bool IncideIrrf { get; private set; }

    /// <summary>Incidencia do RPPS.</summary>
    public bool IncideRpps { get; private set; }

    /// <summary>Incidencia do INSS.</summary>
    public bool IncideInss { get; private set; }

    /// <summary>Resumo da base legal (TCE_4960).</summary>
    public string BaseLegal { get; private set; } = default!;

    /// <summary>Conta do Plano de Contas da Folha (codificacao TCE).</summary>
    public string ContaPlanoFolhaTce { get; private set; } = default!;

    /// <summary>Cria uma rubrica do snapshot.</summary>
    /// <param name="codigo">Codigo da rubrica.</param>
    /// <param name="descricao">Descricao.</param>
    /// <param name="operacao">Operacao (V/D/T/O).</param>
    /// <param name="incideIrrf">Incidencia do IRRF.</param>
    /// <param name="incideRpps">Incidencia do RPPS.</param>
    /// <param name="incideInss">Incidencia do INSS.</param>
    /// <param name="baseLegal">Base legal.</param>
    /// <param name="contaPlanoFolhaTce">Conta do Plano de Contas da Folha.</param>
    /// <returns>Nova <see cref="RubricaFolhaResumo"/>.</returns>
    public static RubricaFolhaResumo Criar(
        string codigo,
        string descricao,
        string operacao,
        bool incideIrrf,
        bool incideRpps,
        bool incideInss,
        string baseLegal,
        string contaPlanoFolhaTce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        return new RubricaFolhaResumo(RubricaFolhaResumoId.New())
        {
            Codigo = codigo,
            Descricao = descricao ?? string.Empty,
            Operacao = string.IsNullOrWhiteSpace(operacao) ? "O" : operacao,
            IncideIrrf = incideIrrf,
            IncideRpps = incideRpps,
            IncideInss = incideInss,
            BaseLegal = baseLegal ?? string.Empty,
            ContaPlanoFolhaTce = contaPlanoFolhaTce ?? string.Empty,
        };
    }
}

/// <summary>Identificador forte de um <see cref="LancamentoFolhaResumo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LancamentoFolhaResumoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo identificador.</returns>
    public static LancamentoFolhaResumoId New() => new(Guid.NewGuid());
}

/// <summary>Lancamento do snapshot da folha (TCE_4810). Entidade-filha exposta pela raiz.</summary>
public sealed class LancamentoFolhaResumo : Entity<LancamentoFolhaResumoId>
{
    private LancamentoFolhaResumo()
    {
    }

    private LancamentoFolhaResumo(LancamentoFolhaResumoId id)
        : base(id)
    {
    }

    /// <summary>Codigo de registro do funcionario (FK p/ TCE_4820).</summary>
    public string CodigoRegistroServidor { get; private set; } = default!;

    /// <summary>Codigo da rubrica (FK p/ TCE_4960).</summary>
    public string CodigoRubrica { get; private set; } = default!;

    /// <summary>Operacao (V/D/T/O).</summary>
    public string Operacao { get; private set; } = default!;

    /// <summary>Valor do lancamento (sempre &gt;= 0; o sinal vem da operacao).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Cria um lancamento do snapshot.</summary>
    /// <param name="codigoRegistroServidor">Codigo de registro do funcionario.</param>
    /// <param name="codigoRubrica">Codigo da rubrica.</param>
    /// <param name="operacao">Operacao (V/D/T/O).</param>
    /// <param name="valor">Valor (&gt;= 0).</param>
    /// <returns>Novo <see cref="LancamentoFolhaResumo"/>.</returns>
    public static LancamentoFolhaResumo Criar(
        string codigoRegistroServidor,
        string codigoRubrica,
        string operacao,
        decimal valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoRegistroServidor);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoRubrica);
        return new LancamentoFolhaResumo(LancamentoFolhaResumoId.New())
        {
            CodigoRegistroServidor = codigoRegistroServidor,
            CodigoRubrica = codigoRubrica,
            Operacao = string.IsNullOrWhiteSpace(operacao) ? "O" : operacao,
            Valor = valor,
        };
    }
}
