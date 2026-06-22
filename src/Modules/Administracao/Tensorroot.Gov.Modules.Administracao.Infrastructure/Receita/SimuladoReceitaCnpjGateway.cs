using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Receita;

/// <summary>
/// Gateway de consulta a Receita Federal simulado (dev/demonstracao): considera ativo todo CNPJ
/// formalmente valido, permitindo exercitar o cadastro de fornecedor sem o servico real.
/// A implementacao de producao sera resiliente (Polly: retry + circuit breaker + timeout) e idempotente.
/// </summary>
public sealed class SimuladoReceitaCnpjGateway : IReceitaCnpjGateway
{
    /// <inheritdoc />
    public Task<bool> EstaAtivoAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        // CNPJ ja formalmente validado por Cnpj.Create; no simulado, considera-se ativo/regular.
        return Task.FromResult(true);
    }
}
