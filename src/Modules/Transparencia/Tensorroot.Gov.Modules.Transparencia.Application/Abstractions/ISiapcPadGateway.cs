using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Pacote ZIP nomeado da remessa SIAPC/PAD, pronto para transmissão MANUAL (PAD desktop).</summary>
/// <param name="NomeZip">Nome estruturado do ZIP (convenção do TCE-RS).</param>
/// <param name="Conteudo">Bytes do ZIP (cada .TXT em ISO-8859-1).</param>
public sealed record PacoteRemessaSiapc(string NomeZip, ReadOnlyMemory<byte> Conteudo);

/// <summary>
/// Empacotador da remessa SIAPC/PAD: monta o ZIP NOMEADO a partir dos arquivos posicionais já emitidos.
/// </summary>
/// <remarks>
/// IMPORTANTE: o TCE-RS NÃO tem API de ENVIO — a transmissão é MANUAL (PAD desktop + e-Protocolo + cert
/// A3 pessoal). Por isso esta porta NÃO transmite por HTTP: ela apenas EMPACOTA e deixa o artefato PRONTO
/// para download/transmissão humana. O ato humano (protocolo/recibo) é registrado por
/// <see cref="IRemessaTceRepository"/>/handler dedicado. Idempotente: o mesmo conjunto de arquivos produz
/// o mesmo ZIP.
/// </remarks>
public interface IEmpacotadorRemessaSiapc
{
    /// <summary>Empacota os arquivos posicionais em um ZIP nomeado conforme a convenção do TCE-RS.</summary>
    /// <param name="nomeZip">Nome estruturado do ZIP (ver <c>NomeArquivoRemessaSiapc</c>).</param>
    /// <param name="arquivos">Arquivos montados (bytes ISO-8859-1 de cada .TXT).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O pacote ZIP pronto para transmissão manual.</returns>
    Task<PacoteRemessaSiapc> EmpacotarAsync(
        string nomeZip,
        IReadOnlyList<ArquivoMontado> arquivos,
        CancellationToken cancellationToken);
}
