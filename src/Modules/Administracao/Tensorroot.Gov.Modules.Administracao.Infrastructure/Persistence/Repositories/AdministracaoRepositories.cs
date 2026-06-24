using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
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
