using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>Identificador forte do agregado <see cref="VinculoServidorUsuario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VinculoServidorUsuarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VinculoServidorUsuarioId"/>.</returns>
    public static VinculoServidorUsuarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Vinculo entre um <c>Usuario</c> (modulo Identidade) e um <see cref="Servidor"/> (modulo
/// RecursosHumanos): registra que o usuario autenticado <see cref="UsuarioId"/> E o servidor
/// <see cref="ServidorId"/> no mesmo tenant. E a ANCORA do autosservico ("Minha Folha"): cada
/// endpoint proprio resolve o ServidorId do PROPRIO usuario por este vinculo (nunca por um id vindo
/// do cliente) — base do ABAC dado-proprio a prova de bala.
/// <para>
/// ISOLAMENTO DE MODULO (CLAUDE.md §1.2): o <see cref="UsuarioId"/> e apenas um <see cref="Guid"/>
/// opaco (o "sub"/subject do JWT). O RecursosHumanos NAO referencia o tipo interno de Usuario do
/// Identidade — guarda so a chave estrangeira logica. A criacao do vinculo entra por um Contract.
/// </para>
/// <para>
/// MULTI-TENANT: e <see cref="IMustHaveTenant"/> (Global Query Filter + carimbo no insert). Um par
/// (TenantId, UsuarioId) e UNICO (um usuario mapeia para no maximo um servidor por tenant) e um par
/// (TenantId, ServidorId) tambem (um servidor para no maximo um usuario) — invariantes de banco.
/// </para>
/// </summary>
public sealed class VinculoServidorUsuario : AggregateRoot<VinculoServidorUsuarioId>, IMustHaveTenant
{
    private VinculoServidorUsuario()
    {
    }

    private VinculoServidorUsuario(
        VinculoServidorUsuarioId id,
        Guid tenantId,
        Guid usuarioId,
        ServidorId servidorId)
        : base(id)
    {
        TenantId = tenantId;
        UsuarioId = usuarioId;
        ServidorId = servidorId;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificador opaco do usuario do Identidade (o "sub"/subject do JWT).</summary>
    public Guid UsuarioId { get; private set; }

    /// <summary>Servidor (vinculo de pessoal) ao qual o usuario corresponde.</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Cria um vinculo usuario&#8596;servidor no tenant informado.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="usuarioId">Usuario (subject do JWT) a vincular.</param>
    /// <param name="servidorId">Servidor correspondente.</param>
    /// <returns>Novo vinculo.</returns>
    /// <exception cref="ArgumentException">Se <paramref name="tenantId"/> ou <paramref name="usuarioId"/> forem vazios.</exception>
    public static VinculoServidorUsuario Criar(Guid tenantId, Guid usuarioId, ServidorId servidorId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant e obrigatorio.", nameof(tenantId));
        }

        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("Usuario e obrigatorio.", nameof(usuarioId));
        }

        if (servidorId.Value == Guid.Empty)
        {
            throw new ArgumentException("Servidor e obrigatorio.", nameof(servidorId));
        }

        return new VinculoServidorUsuario(VinculoServidorUsuarioId.New(), tenantId, usuarioId, servidorId);
    }
}
