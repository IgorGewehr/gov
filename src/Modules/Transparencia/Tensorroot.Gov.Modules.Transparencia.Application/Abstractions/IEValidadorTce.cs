using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Porta (Anti-Corruption Layer) do e-Validador local de remessas TCE-RS. Valida o pacote e produz
/// o RDI (<see cref="ResultadoValidacao"/>), isolando o domínio do contrato externo do validador.
/// A implementação concreta (Infrastructure) mapeia explicitamente os erros do validador para
/// ocorrências de domínio.
/// </summary>
public interface IEValidadorTce
{
    /// <summary>Valida a remessa localmente e gera o RDI (erros/avisos por arquivo/registro).</summary>
    /// <param name="remessa">Remessa a validar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O RDI apurado pelo e-Validador.</returns>
    Task<ResultadoValidacao> ValidarAsync(RemessaTce remessa, CancellationToken cancellationToken);
}
