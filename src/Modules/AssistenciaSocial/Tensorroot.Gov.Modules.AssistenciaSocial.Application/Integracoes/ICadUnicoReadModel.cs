namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Integracoes;

/// <summary>
/// Folha resumo do CadUnico (MDS) projetada para leitura por NIS, isolada por <c>TenantId</c>.
/// Somente leitura; o CadUnico federal nunca trafega entre tenants (I-9, README secao 6).
/// Definido localmente como ACL do modulo enquanto a integracao concreta nao e implementada
/// na camada de Infraestrutura.
/// </summary>
public interface ICadUnicoReadModel
{
    /// <summary>Projeta a folha resumo federal de um NIS no contexto do tenant atual.</summary>
    /// <param name="nis">NIS (somente digitos) a projetar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resultado projetado; <see cref="ResultadoCadUnico.NisLocalizado"/> falso quando ausente.</returns>
    Task<ResultadoCadUnico> ProjetarResumoAsync(string nis, CancellationToken cancellationToken);
}
