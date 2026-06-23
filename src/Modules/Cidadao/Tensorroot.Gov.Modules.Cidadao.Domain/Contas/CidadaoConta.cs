using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

/// <summary>Identificador forte do agregado <see cref="CidadaoConta"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CidadaoContaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CidadaoContaId"/>.</returns>
    public static CidadaoContaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Proveniencia da identidade do cidadao.</summary>
public enum OrigemConta
{
    /// <summary>Cadastro local (CPF/CNPJ + senha) — fluxo entregue agora.</summary>
    CadastroLocal = 1,

    /// <summary>Provisionada/validada pelo login unico gov.br (// TODO(M10-creds)).</summary>
    GovBr = 2,
}

/// <summary>Selo de confiabilidade do gov.br (Decreto 8.936/2016 + niveis de garantia).</summary>
public enum SeloGovBr
{
    /// <summary>Selo bronze (autodeclarado/base CPF).</summary>
    Bronze = 1,

    /// <summary>Selo prata (validacao por banco credenciado/biometria INSS).</summary>
    Prata = 2,

    /// <summary>Selo ouro (validacao por biometria TSE/CNH).</summary>
    Ouro = 3,
}

/// <summary>
/// Conta-cidadao: principal EXTERNO (ator nao-administrativo) do Portal do Cidadao. NAO pertence ao
/// RBAC organizacional do Identidade (sem papeis/UO/permissoes "perm") — autentica em um realm proprio
/// e enxerga SOMENTE o que e seu, com isolamento dado-proprio resolvido server-side pelo
/// <c>Documento</c>. E o equivalente externo do <c>Usuario</c> interno, porem enxuto.
/// <para>
/// MULTI-TENANT (CLAUDE.md §5): e <see cref="IMustHaveTenant"/> — o cidadao do municipio X jamais
/// alcanca dado do municipio Y. O par (TenantId, Documento) e UNICO (uma conta por documento por tenant).
/// </para>
/// <para>
/// SEGURANCA: o dominio NUNCA ve a senha em claro — recebe apenas o hash (mesmo padrao do Usuario). A
/// conta nasce inativa de e-mail (deny-by-default: confirmacao pendente nao impede o login local nesta
/// fase, mas o gating de atos fortes exige selo/confirmacao — parametrizavel).
/// </para>
/// </summary>
public sealed class CidadaoConta : AggregateRoot<CidadaoContaId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome.</summary>
    public const int ComprimentoMaximoNome = 200;

    private CidadaoConta()
    {
    }

    private CidadaoConta(
        CidadaoContaId id,
        Guid tenantId,
        string documento,
        string nome,
        string? email,
        string? telefone,
        string senhaHash,
        OrigemConta origem,
        SeloGovBr? seloGovBr)
        : base(id)
    {
        TenantId = tenantId;
        Documento = documento;
        Nome = nome;
        Email = email;
        Telefone = telefone;
        SenhaHash = senhaHash;
        Origem = origem;
        SeloGovBr = seloGovBr;
        Ativo = true;
        EmailConfirmado = false;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Documento civil (CPF/CNPJ) sem mascara — identidade do cidadao; unico por tenant.</summary>
    public string Documento { get; private set; } = default!;

    /// <summary>Nome ou razao social.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>E-mail de contato/recuperacao (opcional).</summary>
    public string? Email { get; private set; }

    /// <summary>Telefone de contato (opcional).</summary>
    public string? Telefone { get; private set; }

    /// <summary>Hash da senha (BCrypt). A senha em claro nunca toca o dominio nem o banco.</summary>
    public string SenhaHash { get; private set; } = default!;

    /// <summary>Proveniencia da identidade (cadastro local ou gov.br).</summary>
    public OrigemConta Origem { get; private set; }

    /// <summary>Selo gov.br (nulo no cadastro local; vem do id_token no fluxo gov.br).</summary>
    public SeloGovBr? SeloGovBr { get; private set; }

    /// <summary>Conta ativa (deny-by-default: inativa nao autentica).</summary>
    public bool Ativo { get; private set; }

    /// <summary>E-mail confirmado (gate de atos que exigem contato validado).</summary>
    public bool EmailConfirmado { get; private set; }

    /// <summary>
    /// Cria uma conta-cidadao de CADASTRO LOCAL. O documento ja deve estar VALIDADO e em digitos (VO
    /// Cpf/Cnpj no caso de uso); a senha ja deve estar EM HASH (o dominio nunca ve senha em claro).
    /// </summary>
    /// <param name="tenantId">Tenant (municipio) dono da conta.</param>
    /// <param name="documento">CPF/CNPJ em digitos, ja validado.</param>
    /// <param name="nome">Nome/razao social.</param>
    /// <param name="senhaHash">Hash da senha (BCrypt).</param>
    /// <param name="email">E-mail de contato (opcional).</param>
    /// <param name="telefone">Telefone (opcional).</param>
    /// <returns>Nova <see cref="CidadaoConta"/> com <see cref="OrigemConta.CadastroLocal"/>.</returns>
    /// <exception cref="ArgumentException">Se tenant/documento/nome/hash forem invalidos.</exception>
    public static CidadaoConta CriarLocal(
        Guid tenantId,
        string documento,
        string nome,
        string senhaHash,
        string? email = null,
        string? telefone = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant e obrigatorio.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(senhaHash);
        if (nome.Length > ComprimentoMaximoNome)
        {
            throw new ArgumentException($"Nome excede {ComprimentoMaximoNome} caracteres.", nameof(nome));
        }

        return new CidadaoConta(
            CidadaoContaId.New(),
            tenantId,
            documento.Trim(),
            nome.Trim(),
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            senhaHash,
            OrigemConta.CadastroLocal,
            seloGovBr: null);
    }

    /// <summary>Confirma o e-mail da conta (fluxo de confirmacao por link/codigo).</summary>
    public void ConfirmarEmail() => EmailConfirmado = true;

    /// <summary>Desativa a conta (deny-by-default: deixa de autenticar).</summary>
    public void Desativar() => Ativo = false;

    /// <summary>
    /// // TODO(M10-creds): aplicar o selo gov.br lido do <c>id_token</c> apos a validacao do login unico
    /// (provisionamento <see cref="OrigemConta.GovBr"/>). Atras de ACL + IOptions, quando houver creds.
    /// </summary>
    /// <param name="selo">Selo recebido do gov.br.</param>
    public void AplicarSeloGovBr(SeloGovBr selo)
    {
        SeloGovBr = selo;
        Origem = OrigemConta.GovBr;
    }
}
