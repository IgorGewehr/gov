using Tensorroot.Gov.Modules.Identidade.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

/// <summary>Identificador forte do agregado <see cref="UnidadeOrganizacional"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct UnidadeOrganizacionalId(Guid Value)
{
    /// <summary>
    /// Sentinela de ESCOPO GLOBAL/RAIZ PENDENTE (<see cref="Guid.Empty"/>): usada apenas pela ponte
    /// de compatibilidade que converte papeis planos legados em atribuicoes com escopo (MODELO
    /// §10.2). A migracao de dados de M1.x reescreve este valor para o id da UO raiz real do tenant.
    /// Nunca representa uma UO persistida.
    /// </summary>
    public static readonly UnidadeOrganizacionalId RaizPendente = new(Guid.Empty);

    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="UnidadeOrganizacionalId"/>.</returns>
    public static UnidadeOrganizacionalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Unidade Organizacional (UO): no da arvore de Secretarias/Departamentos/Setores do ente
/// (auto-relacionada por <see cref="UnidadePaiId"/>; raiz = <c>null</c>). E a dimensao
/// organizacional do RBAC+ABAC: o escopo de um papel vem da atribuicao a uma UO. Tambem alinha
/// autorizacao e prestacao de contas a mesma dimensao (UO/UG contabil — SIAPC/PAD, MSC). Nunca e
/// deletada (somente desativada) para preservar a trilha de auditoria.
/// </summary>
/// <remarks>
/// Invariante de subarvore conexa (MODELO §8 I5): a arvore e sempre conexa e sem ciclos. O dominio
/// garante localmente que uma UO nao pode ser pai dela mesma e que o reparenteamento exige um pai
/// dentro do MESMO tenant; a ausencia de ciclos na cadeia ascendente (uma UO nao pode ter como pai
/// um de seus proprios descendentes) e validada via <see cref="DefinirPai"/>, que recebe a cadeia
/// de ancestrais candidata resolvida pela aplicacao/persistencia (o agregado nao navega a arvore).
/// </remarks>
public sealed class UnidadeOrganizacional : AggregateRoot<UnidadeOrganizacionalId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do codigo estavel da UO.</summary>
    public const int ComprimentoMaximoCodigo = 40;

    /// <summary>Comprimento maximo do nome da UO.</summary>
    public const int ComprimentoMaximoNome = 200;

    private UnidadeOrganizacional()
    {
    }

    private UnidadeOrganizacional(
        UnidadeOrganizacionalId id,
        Guid tenantId,
        string codigo,
        string nome,
        TipoUnidade tipo,
        UnidadeOrganizacionalId? unidadePaiId)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Nome = nome;
        Tipo = tipo;
        UnidadePaiId = unidadePaiId;
        Ativa = true;
        RaiseDomainEvent(new UnidadeOrganizacionalCriada(id, tenantId, unidadePaiId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo estavel (ex.: <c>SMS</c>, <c>SMS.VIG</c>) — unico por tenant; usado em trilha e remessas.</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Nome de exibicao (ex.: "Secretaria Municipal de Saude").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Natureza administrativa do no.</summary>
    public TipoUnidade Tipo { get; private set; }

    /// <summary>Pai na arvore; <c>null</c> indica a UO raiz do tenant.</summary>
    public UnidadeOrganizacionalId? UnidadePaiId { get; private set; }

    /// <summary>Indica se a UO esta ativa (desativar preserva o historico — nunca deleta).</summary>
    public bool Ativa { get; private set; }

    /// <summary>Indica se esta UO e a raiz da arvore do tenant.</summary>
    public bool EhRaiz => UnidadePaiId is null;

    /// <summary>
    /// Cria a UO RAIZ do tenant (sem pai). E a UO em que, na migracao de compatibilidade, cada
    /// par (Usuario, PapelId) antigo vira uma atribuicao com <c>IncluiSubunidades=true</c>,
    /// preservando o acesso global no go-live (MODELO §10.2).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Codigo estavel da raiz.</param>
    /// <param name="nome">Nome de exibicao.</param>
    /// <param name="tipo">Natureza administrativa (tipicamente <see cref="TipoUnidade.Gabinete"/>/<see cref="TipoUnidade.Secretaria"/>).</param>
    /// <returns>Nova <see cref="UnidadeOrganizacional"/> raiz.</returns>
    public static UnidadeOrganizacional CriarRaiz(Guid tenantId, string codigo, string nome, TipoUnidade tipo)
        => new(UnidadeOrganizacionalId.New(), tenantId, NormalizarCodigo(codigo), NormalizarNome(nome), tipo, unidadePaiId: null);

    /// <summary>Cria uma UO filha de outra UO (subarvore conexa por construcao).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Codigo estavel.</param>
    /// <param name="nome">Nome de exibicao.</param>
    /// <param name="tipo">Natureza administrativa.</param>
    /// <param name="unidadePaiId">UO pai (deve pertencer ao mesmo tenant — validado na aplicacao).</param>
    /// <returns>Nova <see cref="UnidadeOrganizacional"/> filha.</returns>
    public static UnidadeOrganizacional CriarFilha(
        Guid tenantId,
        string codigo,
        string nome,
        TipoUnidade tipo,
        UnidadeOrganizacionalId unidadePaiId)
        => new(UnidadeOrganizacionalId.New(), tenantId, NormalizarCodigo(codigo), NormalizarNome(nome), tipo, unidadePaiId);

    /// <summary>Renomeia a UO e/ou altera seu tipo.</summary>
    /// <param name="nome">Novo nome.</param>
    /// <param name="tipo">Novo tipo.</param>
    public void Editar(string nome, TipoUnidade tipo)
    {
        Nome = NormalizarNome(nome);
        Tipo = tipo;
        RaiseDomainEvent(new UnidadeOrganizacionalEditada(Id));
    }

    /// <summary>
    /// Reparenteia a UO preservando a invariante de subarvore conexa e aciclica (I5). Recebe a
    /// cadeia de ancestrais do NOVO pai (do pai ate a raiz, exclusivo de si mesmo), resolvida pela
    /// aplicacao/persistencia, e rejeita o movimento se esta UO aparecer nessa cadeia (o que criaria
    /// um ciclo: pai virando descendente de si mesmo). O agregado nao navega a arvore.
    /// </summary>
    /// <param name="novoPaiId">UO que passara a ser o pai.</param>
    /// <param name="ancestraisDoNovoPai">Cadeia de ancestrais do novo pai (ids), do pai imediato ate a raiz.</param>
    /// <exception cref="InvalidOperationException">Se o novo pai for a propria UO ou um de seus descendentes (ciclo).</exception>
    public void DefinirPai(UnidadeOrganizacionalId novoPaiId, IReadOnlyCollection<UnidadeOrganizacionalId> ancestraisDoNovoPai)
    {
        ArgumentNullException.ThrowIfNull(ancestraisDoNovoPai);

        if (novoPaiId == Id)
        {
            throw new InvalidOperationException("Uma unidade organizacional nao pode ser pai dela mesma (I5: subarvore aciclica).");
        }

        // Se ESTA UO estiver entre os ancestrais do candidato a pai, o candidato e descendente dela
        // — reparentear criaria um ciclo. Negar por padrao.
        if (ancestraisDoNovoPai.Contains(Id))
        {
            throw new InvalidOperationException("Reparenteamento criaria ciclo na arvore de unidades (I5: subarvore conexa).");
        }

        if (UnidadePaiId == novoPaiId)
        {
            return;
        }

        UnidadePaiId = novoPaiId;
        RaiseDomainEvent(new UnidadeOrganizacionalReparenteada(Id, novoPaiId));
    }

    /// <summary>Ativa a UO. Idempotente.</summary>
    public void Ativar()
    {
        if (Ativa)
        {
            return;
        }

        Ativa = true;
        RaiseDomainEvent(new UnidadeOrganizacionalAtivada(Id));
    }

    /// <summary>Desativa a UO preservando o historico (nunca deleta). Idempotente.</summary>
    public void Desativar()
    {
        if (!Ativa)
        {
            return;
        }

        Ativa = false;
        RaiseDomainEvent(new UnidadeOrganizacionalDesativada(Id));
    }

    private static string NormalizarCodigo(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var limpo = codigo.Trim().ToUpperInvariant();
        if (limpo.Length > ComprimentoMaximoCodigo)
        {
            throw new ArgumentOutOfRangeException(nameof(codigo), $"Codigo da unidade excede {ComprimentoMaximoCodigo} caracteres.");
        }

        return limpo;
    }

    private static string NormalizarNome(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var limpo = nome.Trim();
        if (limpo.Length > ComprimentoMaximoNome)
        {
            throw new ArgumentOutOfRangeException(nameof(nome), $"Nome da unidade excede {ComprimentoMaximoNome} caracteres.");
        }

        return limpo;
    }
}
