using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>
/// Certidao de tempo de servico/contribuicao emitida e autenticada (gancho p/ auditoria, registro no
/// dossie do servidor e eventual publicacao/validacao publica do codigo de autenticacao).
/// </summary>
/// <param name="CertidaoId">Identificador da certidao.</param>
/// <param name="ServidorId">Servidor certificado.</param>
/// <param name="Finalidade">Finalidade da certidao.</param>
/// <param name="TotalDias">Total de dias certificados (apos fatores e abatimentos).</param>
/// <param name="CodigoAutenticacao">Codigo de autenticacao para validacao publica.</param>
public sealed record CertidaoTempoServicoEmitida(
    CertidaoTempoServicoId CertidaoId,
    ServidorId ServidorId,
    FinalidadeCertidao Finalidade,
    int TotalDias,
    string CodigoAutenticacao) : IDomainEvent;

/// <summary>Certidao de tempo de servico/contribuicao anulada (tornada sem efeito).</summary>
/// <param name="CertidaoId">Identificador da certidao anulada.</param>
/// <param name="Motivo">Motivo da anulacao.</param>
public sealed record CertidaoTempoServicoAnulada(
    CertidaoTempoServicoId CertidaoId,
    string Motivo) : IDomainEvent;
