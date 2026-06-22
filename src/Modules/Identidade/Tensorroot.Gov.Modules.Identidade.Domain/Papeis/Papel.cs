using Tensorroot.Gov.Modules.Identidade.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Papeis;

/// <summary>Identificador forte do agregado <see cref="Papel"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PapelId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PapelId"/>.</returns>
    public static PapelId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Papel (perfil RBAC) do tenant: agrupa um conjunto de permissoes do catalogo canonico
/// (<see cref="DomainPermissoes"/>) atribuiveis a usuarios. Negar por padrao: so admite
/// permissoes conhecidas.
/// </summary>
public sealed class Papel : AggregateRoot<PapelId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome do papel.</summary>
    public const int ComprimentoMaximoNome = 80;

    private readonly HashSet<string> _permissoes = new(StringComparer.Ordinal);

    private Papel()
    {
    }

    private Papel(PapelId id, Guid tenantId, string nome)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        RaiseDomainEvent(new PapelCriado(id, tenantId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome do papel (unico por tenant — invariante garantida na aplicacao/persistencia).</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Permissoes (escopos do catalogo canonico) concedidas por este papel.</summary>
    public IReadOnlySet<string> Permissoes => _permissoes;

    /// <summary>Cria um papel para o tenant.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nome">Nome do papel.</param>
    /// <param name="permissoes">Permissoes iniciais (opcional); todas devem ser conhecidas.</param>
    /// <returns>Novo <see cref="Papel"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o nome exceder o comprimento maximo.</exception>
    public static Papel Criar(Guid tenantId, string nome, IEnumerable<string>? permissoes = null)
    {
        var papel = new Papel(PapelId.New(), tenantId, NormalizarNome(nome));
        if (permissoes is not null)
        {
            papel.DefinirPermissoes(permissoes);
        }

        return papel;
    }

    /// <summary>Renomeia o papel.</summary>
    /// <param name="nome">Novo nome.</param>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o nome exceder o comprimento maximo.</exception>
    public void Renomear(string nome) => Nome = NormalizarNome(nome);

    /// <summary>
    /// Redefine integralmente o conjunto de permissoes do papel. Rejeita qualquer escopo fora do
    /// catalogo canonico (negar por padrao). Duplicatas sao colapsadas.
    /// </summary>
    /// <param name="permissoes">Conjunto desejado de permissoes.</param>
    /// <exception cref="ArgumentNullException">Se a colecao for nula.</exception>
    /// <exception cref="ArgumentException">Se alguma permissao nao pertencer ao catalogo.</exception>
    public void DefinirPermissoes(IEnumerable<string> permissoes)
    {
        ArgumentNullException.ThrowIfNull(permissoes);

        var desejadas = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permissao in permissoes)
        {
            if (!DomainPermissoes.EhConhecida(permissao))
            {
                throw new ArgumentException($"Permissao desconhecida: '{permissao}'.", nameof(permissoes));
            }

            desejadas.Add(permissao);
        }

        _permissoes.Clear();
        _permissoes.UnionWith(desejadas);
        RaiseDomainEvent(new PermissoesDoPapelDefinidas(Id));
    }

    private static string NormalizarNome(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var limpo = nome.Trim();
        if (limpo.Length > ComprimentoMaximoNome)
        {
            throw new ArgumentOutOfRangeException(nameof(nome), $"Nome do papel excede {ComprimentoMaximoNome} caracteres.");
        }

        return limpo;
    }
}
