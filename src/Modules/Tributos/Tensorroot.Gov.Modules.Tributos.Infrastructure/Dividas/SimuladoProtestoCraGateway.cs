using System.Globalization;
using System.Text;
using Tensorroot.Gov.Modules.Tributos.Application.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Dividas;

/// <summary>
/// Adapter SIMULADO do protesto extrajudicial junto ao CRA (Anti-Corruption Layer). Serve a
/// dev/testes: gera um arquivo de remessa textual simplificado e interpreta um retorno simples
/// "NUMEROCDA;OCORRENCIA;PROTOCOLO" por linha. O adapter de PRODUÇÃO deve implementar o leiaute
/// oficial do CRA-RS/IEPTB-RS (CNAB 240/400, XML/WebService CRA21 ou registro 600 bytes), atrás de
/// HttpClient resiliente (Polly) e versionado por CRA.
/// // TODO(validar-oficial): leiaute, posições, códigos de ocorrência e endpoint do CRA-RS.
/// </summary>
public sealed class SimuladoProtestoCraGateway : IProtestoCraGateway
{
    /// <inheritdoc />
    public string IdentificadorCra => "CRA-RS-SIMULADO";

    /// <inheritdoc />
    public Task<ArquivoRemessaProtesto> GerarRemessaAsync(TituloProtesto titulo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titulo);

        // Leiaute simplificado (NÃO é o oficial): cabeçalho + 1 registro de título.
        var conteudo = new StringBuilder()
            .Append("HEADER;").Append(IdentificadorCra).Append(';').Append(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd", CultureInfo.InvariantCulture)).Append('\n')
            .Append("TITULO;")
            .Append(titulo.NumeroCda).Append(';')
            .Append(titulo.NomeDevedor).Append(';')
            .Append(titulo.DocumentoDevedor).Append(';')
            .Append(titulo.ValorTitulo.ToString("0.00", CultureInfo.InvariantCulture)).Append(';')
            .Append(titulo.DataInscricao.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).Append('\n')
            .Append("TRAILER;1")
            .ToString();

        return Task.FromResult(new ArquivoRemessaProtesto(IdentificadorCra, conteudo));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RetornoProtesto>> InterpretarRetornoAsync(string conteudoRetorno, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conteudoRetorno);

        var retornos = new List<RetornoProtesto>();
        foreach (var linha in conteudoRetorno.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var campos = linha.Split(';');
            if (campos.Length < 2 || !Enum.TryParse<OcorrenciaProtesto>(campos[1], ignoreCase: true, out var ocorrencia))
            {
                continue;
            }

            var protocolo = campos.Length >= 3 ? campos[2] : null;
            retornos.Add(new RetornoProtesto(campos[0], ocorrencia, protocolo));
        }

        return Task.FromResult<IReadOnlyList<RetornoProtesto>>(retornos);
    }
}
