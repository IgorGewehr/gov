using System.IO.Compression;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Empacotador real da remessa SIAPC/PAD: monta o ZIP nomeado com cada <c>.TXT</c> (bytes ISO-8859-1)
/// como entrada. NÃO transmite — produz o artefato para download e transmissão MANUAL (PAD/e-Protocolo).
/// Determinístico: o mesmo conjunto de arquivos produz o mesmo conteúdo lógico (datas de entrada fixas).
/// </summary>
public sealed class EmpacotadorRemessaSiapc : IEmpacotadorRemessaSiapc
{
    // Data fixa nas entradas do ZIP para empacotamento determinístico (idempotência por conteúdo).
    private static readonly DateTimeOffset DataEntradaFixa = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public Task<PacoteRemessaSiapc> EmpacotarAsync(
        string nomeZip,
        IReadOnlyList<ArquivoMontado> arquivos,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeZip);
        ArgumentNullException.ThrowIfNull(arquivos);

        if (arquivos.Count == 0)
        {
            throw new InvalidOperationException("Não há arquivos para empacotar.");
        }

        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var arquivo in arquivos.OrderBy(item => item.Definicao.NomeArquivo, StringComparer.Ordinal))
            {
                var entrada = zip.CreateEntry(arquivo.Definicao.NomeArquivo, CompressionLevel.Optimal);
                entrada.LastWriteTime = DataEntradaFixa;
                using var fluxo = entrada.Open();
                fluxo.Write(arquivo.Conteudo.Span);
            }
        }

        return Task.FromResult(new PacoteRemessaSiapc(nomeZip, memoria.ToArray()));
    }
}
