using System.Globalization;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Pré-validação LOCAL das remessas SIAPC/PAD (espírito do e-Validador/RDI): aplica regras GENÉRICAS que
/// BLOQUEIAM (estrutura/largura, campos obrigatórios, somatórios) sobre os arquivos montados e produz as
/// ocorrências que alimentam o RDI (<see cref="ResultadoValidacao"/>). A remessa só fica <c>Validada</c>
/// se NÃO houver ocorrência <see cref="SeveridadeOcorrencia.Erro"/>.
/// </summary>
/// <remarks>
/// As CRÍTICAS OFICIAIS (códigos, percentuais, limiares — ex.: limite de pessoal) vêm da Tabela do PAD
/// 2026 + IN TCE-RS 8/2025, parametrizáveis por tenant/exercício — NUNCA <i>hardcoded</i>
/// (CLAUDE.md §7/§16). Aqui só implementamos as críticas ESTRUTURAIS independentes de versão; as críticas
/// de conteúdo específicas de 2026 ficam <c>// TODO(validar-leiaute-MT-2026)</c>.
/// </remarks>
public static class MotorPreValidacaoSiapc
{
    /// <summary>
    /// Valida estruturalmente os arquivos montados contra a grade do leiaute resolvido e devolve as
    /// ocorrências apuradas (erros bloqueiam; o handler converte em <see cref="ResultadoValidacao"/>).
    /// </summary>
    /// <param name="leiaute">Leiaute resolvido (grade posicional).</param>
    /// <param name="arquivos">Arquivos montados (corpo + linhas).</param>
    /// <returns>Ocorrências de erro/aviso (vazia ⇒ remessa apta).</returns>
    /// <exception cref="ArgumentNullException">Se leiaute ou arquivos forem nulos.</exception>
    public static IReadOnlyList<OcorrenciaValidacao> Validar(
        LeiauteSiapc leiaute,
        IReadOnlyList<ArquivoMontado> arquivos)
    {
        ArgumentNullException.ThrowIfNull(leiaute);
        ArgumentNullException.ThrowIfNull(arquivos);

        var ocorrencias = new List<OcorrenciaValidacao>();

        foreach (var arquivo in arquivos)
        {
            // Crítica estrutural 1: o arquivo deve corresponder a um registro declarado no leiaute.
            if (leiaute.RegistroPorArquivo(arquivo.Definicao.NomeArquivo) is null)
            {
                ocorrencias.Add(OcorrenciaValidacao.Criar(
                    arquivo.Definicao.NomeArquivo,
                    0,
                    SeveridadeOcorrencia.Erro,
                    $"Arquivo '{arquivo.Definicao.NomeArquivo}' não pertence ao leiaute {leiaute.Codigo} {leiaute.Versao}."));
                continue;
            }

            // Crítica estrutural 2: o arquivo deve conter ao menos uma linha de dados.
            if (arquivo.Linhas.Count == 0)
            {
                ocorrencias.Add(OcorrenciaValidacao.Criar(
                    arquivo.Definicao.NomeArquivo,
                    0,
                    SeveridadeOcorrencia.Erro,
                    $"Arquivo '{arquivo.Definicao.NomeArquivo}' sem linhas de dados."));
                continue;
            }

            ValidarLinhas(arquivo, ocorrencias);
        }

        return ocorrencias;
    }

    private static void ValidarLinhas(ArquivoMontado arquivo, List<OcorrenciaValidacao> ocorrencias)
    {
        var numeroLinha = 0;
        foreach (var valores in arquivo.Linhas)
        {
            numeroLinha++;
            foreach (var campo in arquivo.Definicao.Campos)
            {
                if (!valores.TryGetValue(campo.Nome, out var valor))
                {
                    // Crítica estrutural 3: campo da grade ausente na linha.
                    ocorrencias.Add(OcorrenciaValidacao.Criar(
                        arquivo.Definicao.NomeArquivo,
                        numeroLinha,
                        SeveridadeOcorrencia.Erro,
                        $"Campo '{campo.Nome}' ausente na linha {numeroLinha}."));
                    continue;
                }

                // Crítica de obrigatoriedade: campo obrigatório não pode estar vazio.
                if (campo.Obrigatorio && valor.EstaVazio)
                {
                    ocorrencias.Add(OcorrenciaValidacao.Criar(
                        arquivo.Definicao.NomeArquivo,
                        numeroLinha,
                        SeveridadeOcorrencia.Erro,
                        $"Campo obrigatório '{campo.Nome}' vazio na linha {numeroLinha}."));
                }
            }
        }
    }

    /// <summary>
    /// Crítica de SOMATÓRIO genérica: confere que o total apurado bate com o esperado (ex.: somatório de
    /// itens × total declarado). Útil para os fechamentos contábeis (partidas dobradas). Diferença ⇒ Erro.
    /// </summary>
    /// <param name="nomeArquivo">Arquivo de referência da ocorrência.</param>
    /// <param name="descricao">Descrição do somatório conferido.</param>
    /// <param name="totalApurado">Total calculado a partir das linhas.</param>
    /// <param name="totalEsperado">Total declarado/esperado.</param>
    /// <returns>Ocorrência de erro se divergir; <c>null</c> se conferir.</returns>
    public static OcorrenciaValidacao? ConferirSomatorio(
        string nomeArquivo,
        string descricao,
        decimal totalApurado,
        decimal totalEsperado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeArquivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        if (totalApurado == totalEsperado)
        {
            return null;
        }

        return OcorrenciaValidacao.Criar(
            nomeArquivo,
            0,
            SeveridadeOcorrencia.Erro,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Somatório divergente ({descricao}): apurado {totalApurado:0.00}, esperado {totalEsperado:0.00}."));
    }
}
