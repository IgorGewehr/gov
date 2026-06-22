using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Pré-validação LOCAL real das remessas SIAPC/PAD (espírito do e-Validador/RDI): resolve a grade do
/// leiaute e aplica críticas ESTRUTURAIS que BLOQUEIAM sobre os arquivos posicionais persistidos —
/// largura de linha vs. leiaute, presença/contagem do finalizador, e largura de campos. Produz o RDI
/// (<see cref="ResultadoValidacao"/>); a remessa só fica <c>Validada</c> sem erro.
/// </summary>
/// <remarks>
/// As críticas de CONTEÚDO específicas (códigos do PAD, percentuais da IN 8/2025) são
/// <c>// TODO(validar-leiaute-MT-2026)</c> — parametrizáveis por tenant/exercício, nunca hardcoded.
/// </remarks>
public sealed class EValidadorLocalSiapc(ILeiauteCatalogo catalogo, TimeProvider timeProvider) : IEValidadorTce
{
    private const string Finalizador = "FINALIZADOR";

    /// <inheritdoc />
    public async Task<ResultadoValidacao> ValidarAsync(RemessaTce remessa, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(remessa);

        var leiaute = await catalogo.ResolverAsync(remessa.Leiaute, cancellationToken).ConfigureAwait(false);
        var ocorrencias = new List<OcorrenciaValidacao>();

        if (remessa.Arquivos.Count == 0)
        {
            ocorrencias.Add(OcorrenciaValidacao.Criar(
                "pacote", 0, SeveridadeOcorrencia.Erro, "Pacote sem arquivos componentes."));
        }

        foreach (var arquivo in remessa.Arquivos)
        {
            ValidarArquivo(leiaute, arquivo.NomeArquivo, arquivo.Conteudo, ocorrencias);
        }

        return ResultadoValidacao.Criar(remessa.Leiaute.Versao, timeProvider.GetUtcNow(), ocorrencias);
    }

    private static void ValidarArquivo(
        LeiauteSiapc leiaute,
        string nomeArquivo,
        ReadOnlyMemory<byte> conteudo,
        List<OcorrenciaValidacao> ocorrencias)
    {
        var definicao = leiaute.RegistroPorArquivo(nomeArquivo);
        if (definicao is null)
        {
            // Crítica estrutural: arquivo fora do leiaute resolvido.
            ocorrencias.Add(OcorrenciaValidacao.Criar(
                nomeArquivo,
                0,
                SeveridadeOcorrencia.Erro,
                $"Arquivo '{nomeArquivo}' não pertence ao leiaute {leiaute.Codigo} {leiaute.Versao}."));
            return;
        }

        var texto = EmissorRegistroSiapc.EncodingSiapc.GetString(conteudo.Span);
        var linhas = texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (linhas.Length == 0)
        {
            ocorrencias.Add(OcorrenciaValidacao.Criar(
                nomeArquivo, 0, SeveridadeOcorrencia.Erro, $"Arquivo '{nomeArquivo}' vazio."));
            return;
        }

        // Última linha deve ser o FINALIZADOR; as do meio (corpo) devem ter a largura do leiaute.
        var finalizador = linhas[^1];
        var linhasCorpo = linhas[1..^1]; // remove cabeçalho (0) e finalizador (^1).

        if (!finalizador.StartsWith(Finalizador, StringComparison.Ordinal))
        {
            ocorrencias.Add(OcorrenciaValidacao.Criar(
                nomeArquivo, linhas.Length, SeveridadeOcorrencia.Erro, "Linha finalizadora ausente ou malformada."));
        }
        else
        {
            ConferirContagemFinalizador(nomeArquivo, finalizador, linhasCorpo.Length, ocorrencias);
        }

        var numero = 1;
        foreach (var linha in linhasCorpo)
        {
            numero++;
            if (linha.Length != definicao.LarguraLinha)
            {
                ocorrencias.Add(OcorrenciaValidacao.Criar(
                    nomeArquivo,
                    numero,
                    SeveridadeOcorrencia.Erro,
                    $"Largura inválida na linha {numero}: {linha.Length} (esperado {definicao.LarguraLinha})."));
            }
        }
    }

    private static void ConferirContagemFinalizador(
        string nomeArquivo,
        string finalizador,
        int registrosCorpo,
        List<OcorrenciaValidacao> ocorrencias)
    {
        var sufixo = finalizador[Finalizador.Length..];
        if (!int.TryParse(sufixo, out var declarado))
        {
            ocorrencias.Add(OcorrenciaValidacao.Criar(
                nomeArquivo, 0, SeveridadeOcorrencia.Erro, "Contagem do finalizador não numérica."));
            return;
        }

        if (declarado != registrosCorpo)
        {
            ocorrencias.Add(OcorrenciaValidacao.Criar(
                nomeArquivo,
                0,
                SeveridadeOcorrencia.Erro,
                $"Finalizador declara {declarado} registros, mas o corpo tem {registrosCorpo}."));
        }
    }
}
