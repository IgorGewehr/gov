using CnpjVo = Tensorroot.Gov.SharedKernel.ValueObjects.Cnpj;

namespace Tensorroot.Gov.Platform.Tenancy;

/// <summary>Poder ao qual o tenant pertence.</summary>
public enum PoderTenant
{
    /// <summary>Prefeitura (Poder Executivo).</summary>
    Executivo = 1,

    /// <summary>Câmara de Vereadores (Poder Legislativo).</summary>
    Legislativo = 2,
}

/// <summary>
/// Tenant (ente público assinante). Registro da PLATAFORMA — não é isolado por tenant;
/// é o catálogo que define quem são os inquilinos e o que cada um licenciou.
/// </summary>
public sealed class Tenant
{
    private Tenant()
    {
    }

    private Tenant(Guid id, string cnpj, string nome, PoderTenant poder, string? connectionString)
    {
        Id = id;
        Cnpj = cnpj;
        Nome = nome;
        Poder = poder;
        ConnectionString = connectionString;
        Ativo = true;
    }

    /// <summary>Identificador do tenant.</summary>
    public Guid Id { get; private set; }

    /// <summary>CNPJ do ente (sem máscara).</summary>
    public string Cnpj { get; private set; } = default!;

    /// <summary>Nome do ente.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Poder (Executivo/Legislativo).</summary>
    public PoderTenant Poder { get; private set; }

    /// <summary>Indica se o tenant está ativo.</summary>
    public bool Ativo { get; private set; }

    /// <summary>Connection string do banco DEDICADO do tenant (database-per-tenant).</summary>
    public string? ConnectionString { get; private set; }

    /// <summary>Cria um tenant validando o CNPJ.</summary>
    /// <param name="cnpj">CNPJ (com ou sem máscara).</param>
    /// <param name="nome">Nome do ente.</param>
    /// <param name="poder">Poder.</param>
    /// <param name="connectionString">Conexão do banco dedicado (opcional; fallback por convenção).</param>
    /// <returns>Novo <see cref="Tenant"/>.</returns>
    public static Tenant Criar(string cnpj, string nome, PoderTenant poder, string? connectionString = null)
    {
        var documento = CnpjVo.Create(cnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new Tenant(Guid.NewGuid(), documento.Digitos, nome, poder, connectionString);
    }

    /// <summary>Define/rotaciona a conexão do banco dedicado.</summary>
    /// <param name="connectionString">Nova connection string.</param>
    public void DefinirConexao(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ConnectionString = connectionString;
    }

    /// <summary>Desativa o tenant.</summary>
    public void Desativar() => Ativo = false;
}

/// <summary>
/// Indice central (email → tenant) usado APENAS para resolver o tenant durante o login, ja que os
/// usuarios residem no banco DEDICADO de cada tenant. Vive no banco de CONTROLE da plataforma.
/// Nao guarda credenciais nem dados sensiveis — apenas o email normalizado e o tenant correspondente.
/// E SEGURANCA CRITICA: a unicidade do email e global (um email pertence a no maximo um tenant),
/// garantida pela chave primaria.
/// </summary>
public sealed class UsuarioTenantIndex
{
    private UsuarioTenantIndex()
    {
    }

    /// <summary>Cria a entrada de indice email → tenant.</summary>
    /// <param name="email">E-mail de login ja normalizado (minusculas, sem espacos).</param>
    /// <param name="tenantId">Tenant dono do usuario.</param>
    /// <exception cref="ArgumentException">Se o e-mail for vazio.</exception>
    public UsuarioTenantIndex(string email, Guid tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Email = email;
        TenantId = tenantId;
    }

    /// <summary>E-mail de login normalizado (chave global).</summary>
    public string Email { get; private set; } = default!;

    /// <summary>Tenant dono do usuario com este e-mail.</summary>
    public Guid TenantId { get; private set; }
}

/// <summary>Licenciamento de um módulo para um tenant (qualquer combinação é vendável).</summary>
public sealed class TenantModule
{
    private TenantModule()
    {
    }

    /// <summary>Cria o vínculo de licença tenant↔módulo.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="moduleName">Nome do módulo (ex.: "Saude").</param>
    public TenantModule(Guid tenantId, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        TenantId = tenantId;
        ModuleName = moduleName;
        Ativo = true;
    }

    /// <summary>Tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome do módulo licenciado.</summary>
    public string ModuleName { get; private set; } = default!;

    /// <summary>Indica se a licença está ativa.</summary>
    public bool Ativo { get; private set; }

    /// <summary>Ativa a licença do módulo.</summary>
    public void Ativar() => Ativo = true;

    /// <summary>Desativa a licença do módulo.</summary>
    public void Desativar() => Ativo = false;
}
