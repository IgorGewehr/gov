using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacao EF Core do repositorio do agregado <see cref="CertidaoTempoServico"/> (CTC). Os periodos
/// (colecao owned) sao carregados sempre que o agregado e' materializado (parte do documento).
/// </summary>
public sealed class CertidaoTempoServicoRepository(RecursosHumanosDbContext context) : ICertidaoTempoServicoRepository
{
    /// <inheritdoc />
    public void Adicionar(CertidaoTempoServico certidao)
    {
        ArgumentNullException.ThrowIfNull(certidao);
        context.CertidoesTempoServico.Add(certidao);
    }

    /// <inheritdoc />
    public Task<CertidaoTempoServico?> ObterPorIdAsync(CertidaoTempoServicoId id, CancellationToken cancellationToken)
        => context.CertidoesTempoServico.FirstOrDefaultAsync(certidao => certidao.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<CertidaoTempoServico?> ObterVigentePorCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        // Entrada PUBLICA (balcao): codigo malformado nao deve lancar — apenas "nao confere" (sem resultado).
        if (!CodigoAutenticacao.TryDe(codigo, out var alvo))
        {
            return Task.FromResult<CertidaoTempoServico?>(null);
        }

        // Constroi o VO FORA da arvore de expressao: o value converter traduz a comparacao para a coluna
        // string (CodigoAutenticacao), permitindo o lookup pelo indice unico (TenantId, CodigoAutenticacao).
        return context.CertidoesTempoServico
            .FirstOrDefaultAsync(
                certidao => certidao.Situacao == SituacaoCertidao.Emitida && certidao.CodigoAutenticacao == alvo,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CertidaoTempoServico>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken)
    {
        var itens = await context.CertidoesTempoServico
            .Where(certidao => certidao.ServidorId == servidorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Mais recentes primeiro (DateOnly nao ordena de forma confiavel no SQLite; ordena em memoria — lote pequeno por servidor).
        return [.. itens
            .OrderByDescending(certidao => certidao.Exercicio)
            .ThenByDescending(certidao => certidao.Sequencial)];
    }

    /// <inheritdoc />
    public async Task<int> ProximoSequencialAsync(int exercicio, CancellationToken cancellationToken)
    {
        // Maior sequencial do exercicio no tenant (Global Query Filter aplica o TenantId) + 1.
        var maximo = await context.CertidoesTempoServico
            .Where(certidao => certidao.Exercicio == exercicio)
            .Select(certidao => (int?)certidao.Sequencial)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return (maximo ?? 0) + 1;
    }
}
