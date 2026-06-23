using Tensorroot.Gov.Modules.Identidade.Domain.Events;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>Identificador forte do agregado <see cref="Usuario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct UsuarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="UsuarioId"/>.</returns>
    public static UsuarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Usuario do tenant: principal autenticavel do RBAC+ABAC. Nasce com a senha ja transformada em
/// hash (o dominio nunca ve a senha em claro) e carrega um conjunto de
/// <see cref="AtribuicaoDePapel"/> — cada uma um papel COM ESCOPO de UO — que determinam suas
/// permissoes efetivas (permissao, conjunto de UOs). Apenas usuarios ativos podem autenticar.
/// </summary>
/// <remarks>
/// COMPATIBILIDADE (MODELO §10.2): o conjunto plano <c>Papeis</c> (apenas os <see cref="PapelId"/>
/// distintos) e mantido DERIVADO das atribuicoes para nao quebrar o enforcement/persistencia
/// atuais durante a transicao. A fonte de verdade rica e <see cref="Atribuicoes"/>.
/// </remarks>
public sealed class Usuario : AggregateRoot<UsuarioId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome.</summary>
    public const int ComprimentoMaximoNome = 200;

    // Backing plano (PapelId distintos) mantido em sincronia com _atribuicoes: preserva o
    // mapeamento/coluna JSON e o enforcement RBAC atuais ate a migracao de dados de M1.x.
    private readonly HashSet<PapelId> _papeis = [];

    // Fonte de verdade rica: atribuicoes de papel com escopo de UO, vigencia e origem.
    private readonly HashSet<AtribuicaoDePapel> _atribuicoes = [];

    private Usuario()
    {
    }

    private Usuario(UsuarioId id, Guid tenantId, string nome, Email email, string senhaHash)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        Ativo = true;
        RaiseDomainEvent(new UsuarioCriado(id, tenantId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome de exibicao do usuario.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>E-mail de login (unico por tenant — invariante garantida na aplicacao/persistencia).</summary>
    public Email Email { get; private set; } = default!;

    /// <summary>Hash da senha (algoritmo + sal embutidos pela porta de hash). Nunca a senha em claro.</summary>
    public string SenhaHash { get; private set; } = default!;

    /// <summary>Indica se o usuario esta habilitado a autenticar.</summary>
    public bool Ativo { get; private set; }

    /// <summary>
    /// Papeis (perfis RBAC) atribuidos ao usuario — visao PLANA e DERIVADA (apenas os
    /// <see cref="PapelId"/> distintos, sem escopo). Mantida por compatibilidade com o
    /// enforcement/persistencia atuais; o escopo organizacional esta em <see cref="Atribuicoes"/>.
    /// </summary>
    public IReadOnlySet<PapelId> Papeis => _papeis;

    /// <summary>Atribuicoes de papel com escopo de UO (fonte de verdade do RBAC+ABAC do sujeito).</summary>
    public IReadOnlyCollection<AtribuicaoDePapel> Atribuicoes => _atribuicoes;

    /// <summary>
    /// Provisiona um novo usuario com o hash de senha ja calculado (recebido pronto da porta de hash).
    /// O usuario nasce ativo e sem papeis.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nome">Nome de exibicao.</param>
    /// <param name="email">E-mail de login (ja validado).</param>
    /// <param name="senhaHash">Hash da senha previamente calculado.</param>
    /// <param name="papeis">Papeis iniciais (opcional).</param>
    /// <returns>Novo <see cref="Usuario"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o e-mail for nulo.</exception>
    /// <exception cref="ArgumentException">Se o nome ou o hash forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o nome exceder o comprimento maximo.</exception>
    public static Usuario Criar(Guid tenantId, string nome, Email email, string senhaHash, IEnumerable<PapelId>? papeis = null)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(senhaHash);

        var usuario = new Usuario(UsuarioId.New(), tenantId, NormalizarNome(nome), email, senhaHash);
        if (papeis is not null)
        {
            usuario.DefinirPapeis(papeis);
            usuario.ClearDomainEvents();
            usuario.RaiseDomainEvent(new UsuarioCriado(usuario.Id, tenantId));
        }

        return usuario;
    }

    /// <summary>Edita os dados cadastrais do usuario (nome e e-mail de login).</summary>
    /// <param name="nome">Novo nome de exibicao.</param>
    /// <param name="email">Novo e-mail de login (ja validado).</param>
    /// <exception cref="ArgumentNullException">Se o e-mail for nulo.</exception>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o nome exceder o comprimento maximo.</exception>
    public void Editar(string nome, Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        Nome = NormalizarNome(nome);
        Email = email;
        RaiseDomainEvent(new UsuarioEditado(Id));
    }

    /// <summary>Habilita o usuario a autenticar. Idempotente.</summary>
    public void Ativar()
    {
        if (Ativo)
        {
            return;
        }

        Ativo = true;
        RaiseDomainEvent(new UsuarioAtivado(Id));
    }

    /// <summary>Impede o usuario de autenticar (desligamento). Idempotente.</summary>
    public void Desativar()
    {
        if (!Ativo)
        {
            return;
        }

        Ativo = false;
        RaiseDomainEvent(new UsuarioDesligado(Id, TenantId));
    }

    /// <summary>
    /// PONTE DE COMPATIBILIDADE (MODELO §10.2): redefine integralmente os papeis do usuario a
    /// partir de uma lista PLANA (sem escopo), criando, para cada papel distinto, uma
    /// <see cref="AtribuicaoDePapel"/> no escopo RAIZ/GLOBAL pendente
    /// (<see cref="UnidadeOrganizacionalId.RaizPendente"/>) com <c>IncluiSubunidades=true</c>,
    /// vigencia aberta e origem direta — equivalente ao acesso global atual, preservado no go-live.
    /// A API rica e <see cref="AtribuirPapel"/>/<see cref="RevogarPapel"/>.
    /// </summary>
    /// <param name="papeis">Conjunto desejado de papeis (substitui o atual). Duplicatas sao colapsadas.</param>
    /// <exception cref="ArgumentNullException">Se a colecao for nula.</exception>
    public void DefinirPapeis(IEnumerable<PapelId> papeis)
    {
        ArgumentNullException.ThrowIfNull(papeis);

        var desejados = new HashSet<PapelId>(papeis);

        _atribuicoes.Clear();
        _papeis.Clear();

        var agora = DateTimeOffset.UtcNow;
        foreach (var papelId in desejados)
        {
            _atribuicoes.Add(AtribuicaoDePapel.Criar(
                papelId,
                UnidadeOrganizacionalId.RaizPendente,
                incluiSubunidades: true,
                Vigencia.Aberta(agora),
                OrigemAtribuicao.Direta()));
            _papeis.Add(papelId);
        }

        RaiseDomainEvent(new PapeisDoUsuarioDefinidos(Id));
    }

    /// <summary>
    /// Atribui um papel ao usuario COM ESCOPO de UO (API rica do RBAC+ABAC — MODELO §2.2/§3).
    /// Idempotente por escopo: se ja existir atribuicao do mesmo papel, na mesma UO e com o mesmo
    /// alcance de subunidades, nao duplica. A validacao da regra "nao delega o que nao tem" (I4) e
    /// do escopo administrativo do concedente ocorre na aplicacao/handler (com acesso ao catalogo e
    /// a arvore); o agregado garante a coesao local.
    /// </summary>
    /// <param name="papelId">Papel a atribuir.</param>
    /// <param name="unidadeId">UO raiz do escopo.</param>
    /// <param name="incluiSubunidades">Se a atribuicao alcanca os descendentes da UO (I5).</param>
    /// <param name="vigencia">Janela de vigencia.</param>
    /// <param name="origem">Procedencia (direta/delegada + concedente).</param>
    /// <param name="profundidadeDelegacao">Profundidade na cadeia de subdelegacao (0 = direta — AA-5/D3).</param>
    /// <exception cref="ArgumentNullException">Se <paramref name="vigencia"/> ou <paramref name="origem"/> forem nulos.</exception>
    public void AtribuirPapel(
        PapelId papelId,
        UnidadeOrganizacionalId unidadeId,
        bool incluiSubunidades,
        Vigencia vigencia,
        OrigemAtribuicao origem,
        int profundidadeDelegacao = 0)
    {
        ArgumentNullException.ThrowIfNull(vigencia);
        ArgumentNullException.ThrowIfNull(origem);

        if (_atribuicoes.Any(a => a.MesmoEscopo(papelId, unidadeId, incluiSubunidades)))
        {
            return;
        }

        _atribuicoes.Add(AtribuicaoDePapel.Criar(papelId, unidadeId, incluiSubunidades, vigencia, origem, profundidadeDelegacao));
        _papeis.Add(papelId);
        RaiseDomainEvent(new PapelAtribuido(Id, papelId, unidadeId, incluiSubunidades));
    }

    /// <summary>
    /// Revoga TODAS as atribuicoes de um papel numa dada UO (qualquer alcance de subunidades). O
    /// papel so sai da visao plana <see cref="Papeis"/> quando nao restar nenhuma atribuicao dele
    /// em qualquer UO.
    /// </summary>
    /// <param name="papelId">Papel a revogar.</param>
    /// <param name="unidadeId">UO cujo escopo sera revogado.</param>
    public void RevogarPapel(PapelId papelId, UnidadeOrganizacionalId unidadeId)
    {
        var removidos = _atribuicoes.RemoveWhere(a => a.PapelId == papelId && a.UnidadeId == unidadeId);
        if (removidos == 0)
        {
            return;
        }

        if (!_atribuicoes.Any(a => a.PapelId == papelId))
        {
            _papeis.Remove(papelId);
        }

        RaiseDomainEvent(new PapelRevogado(Id, papelId, unidadeId));
    }

    /// <summary>
    /// MIGRACAO DE DADOS (MODELO §10.2): re-ancora na UO RAIZ real do tenant toda atribuicao ainda
    /// apontada para a sentinela <see cref="UnidadeOrganizacionalId.RaizPendente"/> (escopo legado
    /// global, criado pela ponte <see cref="DefinirPapeis"/>). Idempotente: se nao houver atribuicao
    /// pendente, nao altera nada. Preserva o alcance global porque a re-ancoragem mantem
    /// <c>IncluiSubunidades=true</c>. Executada no provisionamento/seed (nunca apaga, nunca duplica).
    /// </summary>
    /// <param name="raizId">Id da UO raiz REAL do tenant (ja persistida).</param>
    /// <returns><c>true</c> se alguma atribuicao foi re-ancorada; caso contrario, <c>false</c>.</returns>
    public bool ReancorarAtribuicoesPendentesNaRaiz(UnidadeOrganizacionalId raizId)
    {
        var pendentes = _atribuicoes
            .Where(atribuicao => atribuicao.UnidadeId == UnidadeOrganizacionalId.RaizPendente)
            .ToList();

        if (pendentes.Count == 0)
        {
            return false;
        }

        foreach (var pendente in pendentes)
        {
            // Idempotencia: se ja existir atribuicao do mesmo papel na raiz real (mesmo alcance),
            // descarta a pendente em vez de duplicar; senao, re-ancora a propria pendente.
            var jaExisteNaRaiz = _atribuicoes.Any(atribuicao =>
                atribuicao.UnidadeId == raizId
                && atribuicao.MesmoEscopo(pendente.PapelId, raizId, pendente.IncluiSubunidades));

            if (jaExisteNaRaiz)
            {
                _atribuicoes.Remove(pendente);
            }
            else
            {
                pendente.ReancorarNa(raizId);
            }
        }

        RaiseDomainEvent(new AtribuicoesReancoradasNaRaiz(Id, raizId));
        return true;
    }

    /// <summary>
    /// Substitui o hash de senha por um novo previamente calculado (recebido pronto da porta de hash).
    /// </summary>
    /// <param name="novoHash">Novo hash de senha.</param>
    /// <exception cref="ArgumentException">Se o hash for vazio.</exception>
    public void TrocarSenha(string novoHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(novoHash);
        SenhaHash = novoHash;
        RaiseDomainEvent(new SenhaTrocada(Id));
    }

    private static string NormalizarNome(string nome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var limpo = nome.Trim();
        if (limpo.Length > ComprimentoMaximoNome)
        {
            throw new ArgumentOutOfRangeException(nameof(nome), $"Nome excede {ComprimentoMaximoNome} caracteres.");
        }

        return limpo;
    }
}
