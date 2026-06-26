using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.Pca;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Licitacao"/>.</summary>
public sealed class LicitacaoRepository(AdministracaoDbContext context) : ILicitacaoRepository
{
    /// <inheritdoc />
    public void Adicionar(Licitacao licitacao)
    {
        ArgumentNullException.ThrowIfNull(licitacao);
        context.Licitacoes.Add(licitacao);
    }

    /// <inheritdoc />
    public Task<Licitacao?> ObterPorIdAsync(LicitacaoId id, CancellationToken cancellationToken)
        => context.Licitacoes.FirstOrDefaultAsync(licitacao => licitacao.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Licitacao>> ListarPorSituacaoAsync(SituacaoLicitacao situacao, CancellationToken cancellationToken)
        => await context.Licitacoes
            .Where(licitacao => licitacao.Situacao == situacao)
            .OrderBy(licitacao => licitacao.Objeto)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="DispensaEletronica"/>.</summary>
public sealed class DispensaRepository(AdministracaoDbContext context) : IDispensaRepository
{
    /// <inheritdoc />
    public void Adicionar(DispensaEletronica dispensa)
    {
        ArgumentNullException.ThrowIfNull(dispensa);
        context.Dispensas.Add(dispensa);
    }

    /// <inheritdoc />
    public Task<DispensaEletronica?> ObterPorIdAsync(DispensaEletronicaId id, CancellationToken cancellationToken)
        => context.Dispensas.FirstOrDefaultAsync(dispensa => dispensa.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DispensaEletronica>> ListarPorSituacaoAsync(SituacaoDispensa situacao, CancellationToken cancellationToken)
        => await context.Dispensas
            .Where(dispensa => dispensa.Situacao == situacao)
            .OrderBy(dispensa => dispensa.Objeto)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Contrato"/>.</summary>
public sealed class ContratoRepository(AdministracaoDbContext context) : IContratoRepository
{
    /// <inheritdoc />
    public void Adicionar(Contrato contrato)
    {
        ArgumentNullException.ThrowIfNull(contrato);
        context.Contratos.Add(contrato);
    }

    /// <inheritdoc />
    public Task<Contrato?> ObterPorIdAsync(ContratoId id, CancellationToken cancellationToken)
        => context.Contratos.FirstOrDefaultAsync(contrato => contrato.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contrato>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken)
        => await context.Contratos
            .Where(contrato =>
                (contrato.Situacao == SituacaoContrato.Eficaz || contrato.Situacao == SituacaoContrato.EmExecucao)
                && contrato.VigenciaFim >= referencia)
            .OrderBy(contrato => contrato.VigenciaFim)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contrato>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken)
        => await context.Contratos
            .Where(contrato => contrato.FornecedorId == fornecedorId)
            .OrderByDescending(contrato => contrato.VigenciaInicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contrato>> ListarPendentesPublicacaoPncpAsync(CancellationToken cancellationToken)
        => await context.Contratos
            .Where(contrato =>
                !contrato.PublicadoNoPncp
                && contrato.Situacao != SituacaoContrato.Encerrado
                && contrato.Situacao != SituacaoContrato.Rescindido)
            .OrderBy(contrato => contrato.DataAssinatura)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Fornecedor"/>.</summary>
public sealed class FornecedorRepository(AdministracaoDbContext context) : IFornecedorRepository
{
    /// <inheritdoc />
    public void Adicionar(Fornecedor fornecedor)
    {
        ArgumentNullException.ThrowIfNull(fornecedor);
        context.Fornecedores.Add(fornecedor);
    }

    /// <inheritdoc />
    public Task<Fornecedor?> ObterPorIdAsync(FornecedorId id, CancellationToken cancellationToken)
        => context.Fornecedores.FirstOrDefaultAsync(fornecedor => fornecedor.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Fornecedor?> ObterPorCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        return context.Fornecedores.FirstOrDefaultAsync(fornecedor => fornecedor.Cnpj == cnpj, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistePorCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        return context.Fornecedores.AnyAsync(fornecedor => fornecedor.Cnpj == cnpj, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Fornecedor>> ListarImpedidosAsync(DateOnly referencia, CancellationToken cancellationToken)
    {
        // A vigencia impeditiva e regra de dominio (Sancao.EImpeditiva/EstaVigente); avaliada em memoria
        // a partir dos sancionados, preservando o encapsulamento do agregado.
        var sancionados = await context.Fornecedores
            .Where(fornecedor => fornecedor.Situacao == SituacaoFornecedor.Sancionado)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sancionados
            .Where(fornecedor => fornecedor.EstaImpedido(referencia))
            .OrderBy(fornecedor => fornecedor.RazaoSocial)
            .ToList();
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ItemCatalogo"/>.</summary>
public sealed class CatalogoRepository(AdministracaoDbContext context) : ICatalogoRepository
{
    /// <inheritdoc />
    public void Adicionar(ItemCatalogo item)
    {
        ArgumentNullException.ThrowIfNull(item);
        context.CatalogoItens.Add(item);
    }

    /// <inheritdoc />
    public Task<ItemCatalogo?> ObterPorIdAsync(ItemCatalogoId id, CancellationToken cancellationToken)
        => context.CatalogoItens.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistePorCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        return context.CatalogoItens.AnyAsync(item => item.Codigo == codigo, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemCatalogo>> ListarAsync(
        NaturezaItem? natureza,
        string? termo,
        bool apenasAtivos,
        CancellationToken cancellationToken)
    {
        var consulta = context.CatalogoItens.AsQueryable();
        if (natureza is { } n)
        {
            consulta = consulta.Where(item => item.Natureza == n);
        }

        if (apenasAtivos)
        {
            consulta = consulta.Where(item => item.Situacao == SituacaoItemCatalogo.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            consulta = consulta.Where(item => EF.Functions.Like(item.Codigo, $"%{t}%")
                || EF.Functions.Like(item.Descricao, $"%{t}%"));
        }

        return await consulta
            .OrderBy(item => item.Codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="Ata"/> (ARP).</summary>
public sealed class AtaRepository(AdministracaoDbContext context) : IAtaRepository
{
    /// <inheritdoc />
    public void Adicionar(Ata ata)
    {
        ArgumentNullException.ThrowIfNull(ata);
        context.Atas.Add(ata);
    }

    /// <inheritdoc />
    public Task<Ata?> ObterPorIdAsync(AtaId id, CancellationToken cancellationToken)
        => context.Atas
            .Include(ata => ata.Itens)
            .Include(ata => ata.Adesoes)
            .FirstOrDefaultAsync(ata => ata.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistePorNumeroAsync(string numero, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        return context.Atas.AnyAsync(ata => ata.Numero == numero, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ata>> ListarAsync(SituacaoAta? situacao, CancellationToken cancellationToken)
    {
        var consulta = context.Atas.Include(ata => ata.Itens).AsQueryable();
        if (situacao is { } s)
        {
            consulta = consulta.Where(ata => ata.Situacao == s);
        }

        return await consulta
            .OrderByDescending(ata => ata.VigenciaInicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="PlanoContratacoes"/> (PCA).</summary>
public sealed class PcaRepository(AdministracaoDbContext context) : IPcaRepository
{
    /// <inheritdoc />
    public void Adicionar(PlanoContratacoes plano)
    {
        ArgumentNullException.ThrowIfNull(plano);
        context.PlanosContratacoes.Add(plano);
    }

    /// <inheritdoc />
    public Task<PlanoContratacoes?> ObterPorIdAsync(PlanoContratacoesId id, CancellationToken cancellationToken)
        => context.PlanosContratacoes
            .Include(plano => plano.Itens)
            .FirstOrDefaultAsync(plano => plano.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<PlanoContratacoes?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.PlanosContratacoes
            .Include(plano => plano.Itens)
            .FirstOrDefaultAsync(plano => plano.Exercicio == exercicio, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistePorExercicioAsync(int exercicio, CancellationToken cancellationToken)
        => context.PlanosContratacoes.AnyAsync(plano => plano.Exercicio == exercicio, cancellationToken);
}
