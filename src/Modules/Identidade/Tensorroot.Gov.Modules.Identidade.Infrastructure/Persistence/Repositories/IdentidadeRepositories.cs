using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio de usuarios (tenant-scoped via Global Query Filter).</summary>
public sealed class UsuarioRepository(IdentidadeDbContext context) : IUsuarioRepository
{
    /// <inheritdoc />
    public void Adicionar(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        context.Usuarios.Add(usuario);
    }

    /// <inheritdoc />
    public Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken cancellationToken)
        => context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        return context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> EmailEmUsoAsync(Email email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        return context.Usuarios.AnyAsync(usuario => usuario.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken)
        => await context.Usuarios
            .OrderBy(usuario => usuario.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Usuario?> ObterParaAutenticacaoAsync(Guid tenantId, Email email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        // Autenticacao precede a resolucao do tenant via JWT. Ignoramos o Global Query Filter mas
        // aplicamos o predicado de tenant EXPLICITAMENTE (defesa em profundidade contra vazamento
        // cross-tenant). O DbContext ja esta conectado ao banco DEDICADO do tenant resolvido pelo
        // indice central.
        return await context.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                usuario => usuario.TenantId == tenantId && usuario.Email == email,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementacao EF Core do repositorio de papeis (tenant-scoped via Global Query Filter).</summary>
public sealed class PapelRepository(IdentidadeDbContext context) : IPapelRepository
{
    /// <inheritdoc />
    public void Adicionar(Papel papel)
    {
        ArgumentNullException.ThrowIfNull(papel);
        context.Papeis.Add(papel);
    }

    /// <inheritdoc />
    public Task<Papel?> ObterPorIdAsync(PapelId id, CancellationToken cancellationToken)
        => context.Papeis.FirstOrDefaultAsync(papel => papel.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Papel>> ObterPorIdsAsync(IReadOnlyCollection<PapelId> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return [];
        }

        // EF/SQLite NÃO traduz Contains() sobre chave fortemente tipada com value converter
        // (o IN(...) vem vazio). Comparamos pelos Guids brutos: materializamos os papéis do
        // tenant (filtro global aplicado — poucos por tenant) e filtramos em memória.
        var alvo = ids.Select(id => id.Value).Distinct().ToHashSet();
        var doTenant = await context.Papeis.ToListAsync(cancellationToken).ConfigureAwait(false);
        return doTenant.Where(papel => alvo.Contains(papel.Id.Value)).ToList();
    }

    /// <inheritdoc />
    public Task<bool> NomeEmUsoAsync(string nome, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return context.Papeis.AnyAsync(papel => papel.Nome == nome, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Papel>> ListarAsync(CancellationToken cancellationToken)
        => await context.Papeis
            .OrderBy(papel => papel.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio de Unidades Organizacionais (tenant-scoped via Global Query Filter).</summary>
public sealed class UnidadeRepository(IdentidadeDbContext context) : IUnidadeRepository
{
    /// <inheritdoc />
    public void Adicionar(UnidadeOrganizacional unidade)
    {
        ArgumentNullException.ThrowIfNull(unidade);
        context.Unidades.Add(unidade);
    }

    /// <inheritdoc />
    public Task<UnidadeOrganizacional?> ObterPorIdAsync(UnidadeOrganizacionalId id, CancellationToken cancellationToken)
        => context.Unidades.FirstOrDefaultAsync(unidade => unidade.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);

        // O dominio normaliza o codigo para MAIUSCULAS sem espacos; comparamos pela mesma forma.
        var normalizado = codigo.Trim().ToUpperInvariant();
        return context.Unidades.AnyAsync(unidade => unidade.Codigo == normalizado, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnidadeOrganizacional>> ListarAsync(CancellationToken cancellationToken)
        => await context.Unidades
            .OrderBy(unidade => unidade.Codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
