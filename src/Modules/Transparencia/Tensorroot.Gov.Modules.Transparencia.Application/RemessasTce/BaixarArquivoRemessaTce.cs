using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Conteúdo binário de download (um .TXT específico ou o ZIP completo da remessa).</summary>
/// <param name="NomeArquivo">Nome do arquivo a baixar.</param>
/// <param name="ContentType">Tipo MIME (text/plain ou application/zip).</param>
/// <param name="Conteudo">Bytes do arquivo (ISO-8859-1 para .TXT).</param>
public sealed record ArquivoDownload(string NomeArquivo, string ContentType, ReadOnlyMemory<byte> Conteudo);

/// <summary>
/// Baixa um arquivo da remessa: um <c>.TXT</c> componente (por nome) ou o ZIP nomeado completo.
/// </summary>
/// <param name="RemessaTceId">Remessa de origem.</param>
/// <param name="NomeArquivoTxt">Nome do .TXT a baixar; <c>null</c> ⇒ baixa o ZIP completo.</param>
public sealed record BaixarArquivoRemessaTceQuery(Guid RemessaTceId, string? NomeArquivoTxt)
    : IQuery<ArquivoDownload?>;

/// <summary>Handler do download de arquivos da remessa.</summary>
public sealed class BaixarArquivoRemessaTceHandler(
    IRemessaTceRepository remessas,
    ILeiauteCatalogo leiauteCatalogo,
    IEmpacotadorRemessaSiapc empacotador)
    : IQueryHandler<BaixarArquivoRemessaTceQuery, ArquivoDownload?>
{
    private const string ContentTypeTxt = "text/plain; charset=iso-8859-1";
    private const string ContentTypeZip = "application/zip";

    /// <inheritdoc />
    public async Task<ArquivoDownload?> Handle(BaixarArquivoRemessaTceQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas
            .ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken)
            .ConfigureAwait(false);
        if (remessa is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.NomeArquivoTxt))
        {
            var txt = remessa.Arquivos.FirstOrDefault(
                arquivo => string.Equals(arquivo.NomeArquivo, request.NomeArquivoTxt, StringComparison.OrdinalIgnoreCase));
            return txt is null ? null : new ArquivoDownload(txt.NomeArquivo, ContentTypeTxt, txt.Conteudo);
        }

        var identificacao = await leiauteCatalogo
            .ObterIdentificacaoEnteAsync(remessa.Periodo, cancellationToken)
            .ConfigureAwait(false);
        var leiauteSiapc = await leiauteCatalogo
            .ResolverAsync(remessa.Leiaute, cancellationToken)
            .ConfigureAwait(false);

        var arquivos = remessa.Arquivos
            .Select(arquivo => new ArquivoMontado(
                leiauteSiapc.RegistroPorArquivo(arquivo.NomeArquivo)
                    ?? throw new InvalidOperationException(
                        $"Arquivo '{arquivo.NomeArquivo}' não pertence ao leiaute resolvido."),
                [],
                arquivo.Conteudo))
            .ToList();

        var nomeZip = remessa.NomeArquivoZip ?? NomeArquivoRemessaSiapc.Compor(
            identificacao.Cnpj,
            identificacao.DataInicioPeriodo,
            identificacao.DataFimPeriodo,
            remessa.DataGeracao,
            leiauteSiapc.TipoSetorGoverno,
            identificacao.CodigoRemessa);

        var pacote = await empacotador.EmpacotarAsync(nomeZip, arquivos, cancellationToken).ConfigureAwait(false);
        return new ArquivoDownload(pacote.NomeZip, ContentTypeZip, pacote.Conteudo);
    }
}
