namespace Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

/// <summary>
/// Dados civis do aluno (owned type): nome civil, nome social, data de nascimento, sexo, nome da mae
/// (obrigatorio — exigencia EducaCenso) e nome do pai (opcional). Objeto de valor imutavel; o
/// construtor protege as invariantes I-A1 (nome/data) e I-A3 (nome da mae). Contem PII de menor
/// (LGPD art. 14) — o agregado marca o campo com <c>[CampoSensivelLgpd]</c> para redacao na trilha.
/// </summary>
public readonly record struct DadosCivis
{
    /// <summary>Comprimento maximo dos campos de nome.</summary>
    public const int ComprimentoNome = 120;

    /// <summary>Cria os dados civis do aluno, validando nome, data de nascimento e nome da mae.</summary>
    /// <param name="nome">Nome civil (obrigatorio).</param>
    /// <param name="dataNascimento">Data de nascimento (nao futura).</param>
    /// <param name="sexo">Sexo.</param>
    /// <param name="nomeMae">Nome da mae (obrigatorio — EducaCenso, I-A3).</param>
    /// <param name="nomePai">Nome do pai (opcional).</param>
    /// <param name="nomeSocial">Nome social (opcional).</param>
    /// <param name="hoje">Data corrente para a guarda de nascimento nao futuro.</param>
    /// <exception cref="ArgumentException">Se o nome ou o nome da mae forem vazios ou excederem o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a data de nascimento for futura.</exception>
    public DadosCivis(
        string nome,
        DateOnly dataNascimento,
        Sexo sexo,
        string nomeMae,
        string? nomePai,
        string? nomeSocial,
        DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeMae);

        var nomeNormalizado = nome.Trim();
        var nomeMaeNormalizado = nomeMae.Trim();
        if (nomeNormalizado.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Nome do aluno excede {ComprimentoNome} caracteres.", nameof(nome));
        }

        if (nomeMaeNormalizado.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Nome da mae excede {ComprimentoNome} caracteres.", nameof(nomeMae));
        }

        if (dataNascimento > hoje)
        {
            throw new ArgumentOutOfRangeException(nameof(dataNascimento), "Data de nascimento nao pode ser futura.");
        }

        Nome = nomeNormalizado;
        DataNascimento = dataNascimento;
        Sexo = sexo;
        NomeMae = nomeMaeNormalizado;
        NomePai = string.IsNullOrWhiteSpace(nomePai) ? null : nomePai.Trim();
        NomeSocial = string.IsNullOrWhiteSpace(nomeSocial) ? null : nomeSocial.Trim();
    }

    // Construtor de materializacao (EF Core): vincula 1:1 as propriedades persistidas, sem o
    // parametro de guarda 'hoje'. Os dados ja foram validados na criacao via o construtor publico.
    private DadosCivis(string nome, DateOnly dataNascimento, Sexo sexo, string nomeMae, string? nomePai, string? nomeSocial)
    {
        Nome = nome;
        DataNascimento = dataNascimento;
        Sexo = sexo;
        NomeMae = nomeMae;
        NomePai = nomePai;
        NomeSocial = nomeSocial;
    }

    /// <summary>Nome civil do aluno.</summary>
    public string Nome { get; }

    /// <summary>Nome social, quando informado.</summary>
    public string? NomeSocial { get; }

    /// <summary>Data de nascimento.</summary>
    public DateOnly DataNascimento { get; }

    /// <summary>Sexo.</summary>
    public Sexo Sexo { get; }

    /// <summary>Nome da mae (obrigatorio — EducaCenso).</summary>
    public string NomeMae { get; }

    /// <summary>Nome do pai, quando informado.</summary>
    public string? NomePai { get; }

    /// <summary>Idade do aluno (anos completos) na data de referencia informada.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns>Idade em anos completos.</returns>
    public int IdadeEm(DateOnly hoje)
    {
        var idade = hoje.Year - DataNascimento.Year;
        if (DataNascimento > hoje.AddYears(-idade))
        {
            idade--;
        }

        return idade;
    }

    /// <summary>Indica se o aluno e menor de idade (menos de 18 anos completos) na data informada.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se menor de 18 anos.</returns>
    public bool EhMenorEm(DateOnly hoje) => IdadeEm(hoje) < MaioridadeCivil;

    /// <summary>Idade da maioridade civil (CC art. 5º).</summary>
    public const int MaioridadeCivil = 18;
}
