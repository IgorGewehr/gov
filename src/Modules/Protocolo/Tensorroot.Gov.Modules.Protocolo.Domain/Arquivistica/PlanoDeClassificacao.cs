using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

/// <summary>Identificador forte do agregado <see cref="PlanoDeClassificacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PlanoDeClassificacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PlanoDeClassificacaoId"/>.</returns>
    public static PlanoDeClassificacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="ClasseDocumental"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ClasseDocumentalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ClasseDocumentalId"/>.</returns>
    public static ClasseDocumentalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Plano de Classificacao documental do tenant (e-ARQ v2 / CONARQ): codigo x assunto, organizado em
/// arvore de <see cref="ClasseDocumental"/>. Substitui a string solta de classificacao: a VO
/// <c>Classificacao</c> do processo passa a referenciar um codigo VALIDO no plano ativo do tenant
/// (I-T5). Multi-tenant: cada municipio tem seu plano aprovado. Raiz de agregado.
/// </summary>
public sealed class PlanoDeClassificacao : AggregateRoot<PlanoDeClassificacaoId>, IMustHaveTenant
{
    private readonly List<ClasseDocumental> _classes = [];

    private PlanoDeClassificacao()
    {
    }

    private PlanoDeClassificacao(PlanoDeClassificacaoId id, Guid tenantId, string nome, bool ativo)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Ativo = ativo;
    }

    /// <summary>Tenant (ente publico) dono do plano.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome/identificacao do plano (ex.: "Plano de Classificacao 2026").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Indica se o plano e o ATIVO do tenant (a validacao de classe usa o ativo).</summary>
    public bool Ativo { get; private set; }

    /// <summary>Classes documentais do plano (entidade interna, arvore).</summary>
    public IReadOnlyList<ClasseDocumental> Classes => _classes;

    /// <summary>Cria um novo plano de classificacao (ativo por padrao) do tenant.</summary>
    /// <param name="tenantId">Tenant dono do plano.</param>
    /// <param name="nome">Nome do plano.</param>
    /// <returns>Novo <see cref="PlanoDeClassificacao"/> ativo.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    public static PlanoDeClassificacao Criar(Guid tenantId, string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new PlanoDeClassificacao(PlanoDeClassificacaoId.New(), tenantId, nome.Trim(), ativo: true);
    }

    /// <summary>
    /// Adiciona (ou rejeita duplicata de) uma classe documental ao plano. O codigo e unico no plano.
    /// </summary>
    /// <param name="codigoClassificacao">Codigo (ex.: "040", "040.1").</param>
    /// <param name="assunto">Assunto/descricao da classe.</param>
    /// <param name="atividade">Natureza (meio/fim).</param>
    /// <param name="codigoPai">Codigo da classe-pai (arvore), quando houver.</param>
    /// <exception cref="ArgumentException">Se codigo/assunto forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se o codigo ja existir no plano.</exception>
    public void AdicionarClasse(string codigoClassificacao, string assunto, AtividadeMeioOuFim atividade, string? codigoPai = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoClassificacao);
        var codigo = codigoClassificacao.Trim();
        if (_classes.Any(classe => string.Equals(classe.CodigoClassificacao, codigo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Classe '{codigo}' ja existe no plano.");
        }

        _classes.Add(ClasseDocumental.Criar(codigo, assunto, atividade, codigoPai));
    }

    /// <summary>Indica se o codigo de classificacao existe neste plano (I-T5).</summary>
    /// <param name="codigoClassificacao">Codigo a verificar.</param>
    /// <returns><c>true</c> se houver uma classe com esse codigo.</returns>
    public bool Contem(string codigoClassificacao)
    {
        if (string.IsNullOrWhiteSpace(codigoClassificacao))
        {
            return false;
        }

        var codigo = codigoClassificacao.Trim();
        return _classes.Any(classe => string.Equals(classe.CodigoClassificacao, codigo, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Desativa o plano (ex.: ao publicar uma nova versao).</summary>
    public void Desativar() => Ativo = false;
}

/// <summary>
/// Classe documental (entidade interna do <see cref="PlanoDeClassificacao"/>, arvore): codigo x
/// assunto, com referencia opcional a classe-pai. Append-only — compoe a estrutura aprovada do plano.
/// </summary>
public sealed class ClasseDocumental : Entity<ClasseDocumentalId>
{
    /// <summary>Comprimento maximo do codigo de classificacao.</summary>
    public const int ComprimentoMaximoCodigo = 60;

    /// <summary>Comprimento maximo do assunto.</summary>
    public const int ComprimentoMaximoAssunto = 300;

    private ClasseDocumental()
    {
    }

    private ClasseDocumental(
        ClasseDocumentalId id,
        string codigoClassificacao,
        string assunto,
        AtividadeMeioOuFim atividade,
        string? codigoPai)
        : base(id)
    {
        CodigoClassificacao = codigoClassificacao;
        Assunto = assunto;
        Atividade = atividade;
        CodigoPai = codigoPai;
    }

    /// <summary>Codigo de classificacao (ex.: "040", "040.1").</summary>
    public string CodigoClassificacao { get; private set; } = default!;

    /// <summary>Assunto/descricao da classe.</summary>
    public string Assunto { get; private set; } = default!;

    /// <summary>Natureza da atividade (meio/fim).</summary>
    public AtividadeMeioOuFim Atividade { get; private set; }

    /// <summary>Codigo da classe-pai (arvore), quando houver.</summary>
    public string? CodigoPai { get; private set; }

    /// <summary>Cria uma classe documental validada.</summary>
    /// <param name="codigoClassificacao">Codigo.</param>
    /// <param name="assunto">Assunto.</param>
    /// <param name="atividade">Natureza (meio/fim).</param>
    /// <param name="codigoPai">Codigo da classe-pai (opcional).</param>
    /// <returns>Nova <see cref="ClasseDocumental"/>.</returns>
    /// <exception cref="ArgumentException">Se codigo/assunto forem vazios ou excederem limites.</exception>
    public static ClasseDocumental Criar(string codigoClassificacao, string assunto, AtividadeMeioOuFim atividade, string? codigoPai)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoClassificacao);
        ArgumentException.ThrowIfNullOrWhiteSpace(assunto);
        var codigo = codigoClassificacao.Trim();
        var assuntoNorm = assunto.Trim();
        if (codigo.Length > ComprimentoMaximoCodigo)
        {
            throw new ArgumentException($"Codigo excede {ComprimentoMaximoCodigo} caracteres.", nameof(codigoClassificacao));
        }

        if (assuntoNorm.Length > ComprimentoMaximoAssunto)
        {
            throw new ArgumentException($"Assunto excede {ComprimentoMaximoAssunto} caracteres.", nameof(assunto));
        }

        return new ClasseDocumental(ClasseDocumentalId.New(), codigo, assuntoNorm, atividade, codigoPai?.Trim());
    }
}
