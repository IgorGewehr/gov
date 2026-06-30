using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Events;

/// <summary>Usuario criado (provisionado) no tenant.</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record UsuarioCriado(UsuarioId UsuarioId, Guid TenantId) : IDomainEvent;

/// <summary>Dados cadastrais do usuario editados.</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
public sealed record UsuarioEditado(UsuarioId UsuarioId) : IDomainEvent;

/// <summary>Usuario ativado (habilitado a autenticar).</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
public sealed record UsuarioAtivado(UsuarioId UsuarioId) : IDomainEvent;

/// <summary>Usuario desligado/desativado (impedido de autenticar) — evento sensivel para auditoria.</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record UsuarioDesligado(UsuarioId UsuarioId, Guid TenantId) : IDomainEvent;

/// <summary>
/// Conta bloqueada temporariamente por excesso de tentativas de login malsucedidas (anti
/// brute-force, P7) — evento sensivel para auditoria/seguranca. Emitido APENAS na transicao para
/// bloqueada (nao a cada falha), evitando amplificacao de Outbox sob ataque.
/// </summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="LockoutEnd">Instante ate o qual a conta permanece bloqueada.</param>
public sealed record UsuarioBloqueadoPorTentativas(
    UsuarioId UsuarioId,
    Guid TenantId,
    DateTimeOffset LockoutEnd) : IDomainEvent;

/// <summary>Papeis (perfis RBAC) do usuario redefinidos.</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
public sealed record PapeisDoUsuarioDefinidos(UsuarioId UsuarioId) : IDomainEvent;

/// <summary>Papel atribuido ao usuario COM ESCOPO de UO — evento sensivel para auditoria (I8).</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
/// <param name="PapelId">Papel atribuido.</param>
/// <param name="UnidadeId">UO raiz do escopo.</param>
/// <param name="IncluiSubunidades">Se a atribuicao alcanca os descendentes da UO.</param>
public sealed record PapelAtribuido(
    UsuarioId UsuarioId,
    PapelId PapelId,
    UnidadeOrganizacionalId UnidadeId,
    bool IncluiSubunidades) : IDomainEvent;

/// <summary>Atribuicao de papel revogada do usuario — evento sensivel para auditoria (I8).</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
/// <param name="PapelId">Papel revogado.</param>
/// <param name="UnidadeId">UO raiz do escopo revogado.</param>
public sealed record PapelRevogado(
    UsuarioId UsuarioId,
    PapelId PapelId,
    UnidadeOrganizacionalId UnidadeId) : IDomainEvent;

/// <summary>Senha do usuario trocada (apenas o fato; nunca o hash) — evento sensivel para auditoria.</summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
public sealed record SenhaTrocada(UsuarioId UsuarioId) : IDomainEvent;

/// <summary>
/// Atribuicoes legadas (escopo global pendente) re-ancoradas na UO RAIZ real do tenant (migracao de
/// dados MODELO §10.2) — evento sensivel para auditoria (I8).
/// </summary>
/// <param name="UsuarioId">Identificador do usuario.</param>
/// <param name="RaizId">UO raiz real em que as atribuicoes pendentes foram ancoradas.</param>
public sealed record AtribuicoesReancoradasNaRaiz(
    UsuarioId UsuarioId,
    UnidadeOrganizacionalId RaizId) : IDomainEvent;

/// <summary>Papel (perfil RBAC) criado no tenant.</summary>
/// <param name="PapelId">Identificador do papel.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record PapelCriado(PapelId PapelId, Guid TenantId) : IDomainEvent;

/// <summary>Permissoes de um papel redefinidas — evento sensivel para auditoria.</summary>
/// <param name="PapelId">Identificador do papel.</param>
public sealed record PermissoesDoPapelDefinidas(PapelId PapelId) : IDomainEvent;

/// <summary>Unidade Organizacional (UO) criada no tenant.</summary>
/// <param name="UnidadeId">Identificador da UO.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="UnidadePaiId">Pai na arvore; <c>null</c> se raiz.</param>
public sealed record UnidadeOrganizacionalCriada(
    UnidadeOrganizacionalId UnidadeId,
    Guid TenantId,
    UnidadeOrganizacionalId? UnidadePaiId) : IDomainEvent;

/// <summary>Dados de uma UO editados (nome/tipo).</summary>
/// <param name="UnidadeId">Identificador da UO.</param>
public sealed record UnidadeOrganizacionalEditada(UnidadeOrganizacionalId UnidadeId) : IDomainEvent;

/// <summary>UO reparenteada na arvore — evento sensivel para auditoria (afeta escopos).</summary>
/// <param name="UnidadeId">Identificador da UO movida.</param>
/// <param name="NovoPaiId">Novo pai na arvore.</param>
public sealed record UnidadeOrganizacionalReparenteada(
    UnidadeOrganizacionalId UnidadeId,
    UnidadeOrganizacionalId NovoPaiId) : IDomainEvent;

/// <summary>UO ativada.</summary>
/// <param name="UnidadeId">Identificador da UO.</param>
public sealed record UnidadeOrganizacionalAtivada(UnidadeOrganizacionalId UnidadeId) : IDomainEvent;

/// <summary>UO desativada (historico preservado) — evento sensivel para auditoria.</summary>
/// <param name="UnidadeId">Identificador da UO.</param>
public sealed record UnidadeOrganizacionalDesativada(UnidadeOrganizacionalId UnidadeId) : IDomainEvent;
