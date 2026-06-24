using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>Identificador forte de um <see cref="Dependente"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DependenteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DependenteId"/>.</returns>
    public static DependenteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Dependente do servidor para fins de Imposto de Renda e beneficios — entidade-filha do
/// agregado <see cref="Servidor"/>, exposta somente atraves da raiz. Dados pessoais
/// sensiveis (LGPD).
/// </summary>
/// <remarks>
/// Nem todo dependente registrado e dedutivel do IRRF. A elegibilidade fiscal segue o rol
/// FECHADO da Lei 9.250/1995 art. 35 (regulamentado no RIR/2018 art. 90/91): conjuge/companheiro,
/// filho/enteado ate 21 anos (ou ate 24 se cursando ensino superior/tecnico), filho/enteado
/// incapaz em qualquer idade, irmao/neto/bisneto sob guarda judicial nas mesmas faixas etarias,
/// pais/avos/bisavos com rendimento dentro do limite legal, absolutamente incapaz tutelado/curatelado.
/// Dependente registrado apenas para beneficios (ex.: sogra, filho maior 24 nao-estudante, pessoa
/// com renda propria acima do limite) NAO gera deducao de IRRF. A elegibilidade e capturada
/// explicitamente em <see cref="ElegivelIrrf"/> — decisao do cadastro, nunca presumida do
/// <c>Parentesco</c> ou inferida em runtime — e somente os elegiveis compoem a deducao na base.
/// </remarks>
public sealed class Dependente : Entity<DependenteId>
{
    private Dependente()
    {
    }

    private Dependente(DependenteId id, string nome, string parentesco, DateOnly dataNascimento, bool elegivelIrrf)
        : base(id)
    {
        Nome = nome;
        Parentesco = parentesco;
        DataNascimento = dataNascimento;
        ElegivelIrrf = elegivelIrrf;
    }

    /// <summary>Nome civil do dependente.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Grau de parentesco com o servidor.</summary>
    public string Parentesco { get; private set; } = default!;

    /// <summary>Data de nascimento do dependente.</summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>
    /// Indica se o dependente e ELEGIVEL a deducao de IRRF (rol fechado da Lei 9.250/1995 art. 35).
    /// Apenas dependentes com esta flag compoem a deducao por dependente na base do imposto;
    /// dependentes registrados somente para beneficios permanecem <c>false</c> e nao reduzem a base
    /// (evita sub-recolhimento de IRRF retido na fonte).
    /// </summary>
    public bool ElegivelIrrf { get; private set; }

    /// <summary>Registra um novo dependente.</summary>
    /// <param name="nome">Nome civil (nao vazio).</param>
    /// <param name="parentesco">Grau de parentesco (nao vazio).</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <param name="elegivelIrrf">
    /// <c>true</c> se enquadrado no rol fechado da Lei 9.250/1995 art. 35 (dedutivel do IRRF);
    /// <c>false</c> se registrado apenas para beneficios. Padrao <c>false</c> (fail-closed fiscal:
    /// na ausencia de decisao explicita de elegibilidade, nao se deduz, evitando sub-recolhimento).
    /// </param>
    /// <returns>Novo <see cref="Dependente"/>.</returns>
    /// <exception cref="ArgumentException">Se nome/parentesco for vazio.</exception>
    public static Dependente Registrar(string nome, string parentesco, DateOnly dataNascimento, bool elegivelIrrf = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentesco);
        return new Dependente(DependenteId.New(), nome.Trim(), parentesco.Trim(), dataNascimento, elegivelIrrf);
    }

    /// <summary>
    /// Ajusta a elegibilidade fiscal do dependente para deducao de IRRF (Lei 9.250/1995 art. 35),
    /// refletindo mudanca de condicao (ex.: filho que perde a faixa etaria ou conclui o ensino
    /// superior deixa de ser elegivel; reconhecimento posterior de dependencia o torna elegivel).
    /// </summary>
    /// <param name="elegivel"><c>true</c> se passa a ser dedutivel do IRRF; <c>false</c> caso contrario.</param>
    public void DefinirElegibilidadeIrrf(bool elegivel) => ElegivelIrrf = elegivel;
}
