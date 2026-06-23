using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

/// <summary>
/// Responsavel pelo aluno (entidade-filha do agregado <see cref="Aluno"/>). Para o aluno menor de
/// idade (LGPD art. 14 — melhor interesse do menor) e exigido ao menos um responsavel. O CPF e
/// dado pessoal sensivel e e redigido na trilha de auditoria. Nasce valido via <see cref="Criar"/>.
/// </summary>
public sealed class Responsavel : Entity<ResponsavelId>
{
    private Responsavel()
    {
    }

    private Responsavel(
        ResponsavelId id,
        string nome,
        Cpf? cpf,
        Parentesco parentesco,
        string? telefone,
        bool responsavelFinanceiro,
        bool autorizadoBuscar)
        : base(id)
    {
        Nome = nome;
        Cpf = cpf;
        Parentesco = parentesco;
        Telefone = telefone;
        ResponsavelFinanceiro = responsavelFinanceiro;
        AutorizadoBuscar = autorizadoBuscar;
    }

    /// <summary>Nome do responsavel.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>CPF do responsavel, quando informado — PII redigida na trilha (LGPD art. 14).</summary>
    [CampoSensivelLgpd]
    public Cpf? Cpf { get; private set; }

    /// <summary>Grau de parentesco/vinculo com o aluno.</summary>
    public Parentesco Parentesco { get; private set; }

    /// <summary>Telefone de contato, quando informado.</summary>
    public string? Telefone { get; private set; }

    /// <summary>Indica se e o responsavel financeiro (mensalidade/contribuicoes, quando aplicavel).</summary>
    public bool ResponsavelFinanceiro { get; private set; }

    /// <summary>Indica se esta autorizado a buscar o aluno na escola.</summary>
    public bool AutorizadoBuscar { get; private set; }

    /// <summary>Cria um responsavel valido.</summary>
    /// <param name="nome">Nome do responsavel (obrigatorio).</param>
    /// <param name="cpf">CPF (opcional).</param>
    /// <param name="parentesco">Grau de parentesco/vinculo.</param>
    /// <param name="telefone">Telefone de contato (opcional).</param>
    /// <param name="responsavelFinanceiro">Indica se e o responsavel financeiro.</param>
    /// <param name="autorizadoBuscar">Indica se esta autorizado a buscar o aluno.</param>
    /// <returns>Novo <see cref="Responsavel"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o parentesco for invalido.</exception>
    public static Responsavel Criar(
        string nome,
        Cpf? cpf,
        Parentesco parentesco,
        string? telefone,
        bool responsavelFinanceiro,
        bool autorizadoBuscar)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        if (!Enum.IsDefined(parentesco))
        {
            throw new ArgumentOutOfRangeException(nameof(parentesco), "Parentesco invalido.");
        }

        return new Responsavel(
            ResponsavelId.New(),
            nome.Trim(),
            cpf,
            parentesco,
            string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            responsavelFinanceiro,
            autorizadoBuscar);
    }
}
