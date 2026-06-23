using System.Globalization;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;

/// <summary>
/// Servico de dominio que mapeia o snapshot <see cref="ResumoFolhaTce"/> para as LINHAS posicionais dos
/// tres arquivos da REMESSA DE FOLHA ao TCE-RS (TCE_4810/4820/4960), casando cada valor com o
/// <see cref="CampoLeiaute"/> da grade resolvida. O <see cref="EmissorRegistroSiapc"/> (reusado do M4)
/// serializa as linhas em largura fixa ISO-8859-1. Puro: sem I/O.
/// </summary>
/// <remarks>
/// REGRA [OFICIAL] §16: o casamento campo->valor segue a ESTRUTURA do MT SIAPC Vol. V §3.1, mas os nomes
/// EXATOS de campo/codigos numericos de rubrica dependem do documento oficial — marcado
/// <c>// TODO(validar-leiaute-folha-1099)</c>. Mapeia apenas campos cujo nome existe na grade resolvida;
/// campos da grade nao alimentados ficam com o default do tipo (vazio/zeros), e a obrigatoriedade e
/// checada na PRE-VALIDACAO LOCAL.
/// </remarks>
public static class MapeadorRemessaFolhaTce
{
    /// <summary>Nome do arquivo de lancamentos (TCE_4810).</summary>
    public const string ArquivoLancamentos = "TCE_4810.TXT";

    /// <summary>Nome do arquivo de cadastro de funcionarios (TCE_4820).</summary>
    public const string ArquivoFuncionarios = "TCE_4820.TXT";

    /// <summary>Nome do arquivo de rubricas (TCE_4960).</summary>
    public const string ArquivoRubricas = "TCE_4960.TXT";

    private const char Sim = 'S';
    private const char Nao = 'N';

    /// <summary>
    /// Constroi as linhas consolidadas (por arquivo) a partir do resumo, prontas para o emissor posicional.
    /// </summary>
    /// <param name="resumo">Snapshot da folha consumido do RH.</param>
    /// <param name="leiaute">Leiaute de folha resolvido (grade posicional TCE_4810/4820/4960).</param>
    /// <returns>Linhas por arquivo (nome do arquivo + valores por nome de campo).</returns>
    /// <exception cref="ArgumentNullException">Se resumo ou leiaute forem nulos.</exception>
    public static IReadOnlyList<LinhaFolhaMontada> Montar(ResumoFolhaTce resumo, LeiauteSiapc leiaute)
    {
        ArgumentNullException.ThrowIfNull(resumo);
        ArgumentNullException.ThrowIfNull(leiaute);

        var competencia = new DateOnly(resumo.Exercicio, resumo.Mes, 1);
        var linhas = new List<LinhaFolhaMontada>();

        var defFuncionarios = leiaute.RegistroPorArquivo(ArquivoFuncionarios);
        if (defFuncionarios is not null)
        {
            foreach (var servidor in resumo.Servidores)
            {
                linhas.Add(new LinhaFolhaMontada(ArquivoFuncionarios, MapearFuncionario(defFuncionarios, servidor, competencia)));
            }
        }

        var defRubricas = leiaute.RegistroPorArquivo(ArquivoRubricas);
        if (defRubricas is not null)
        {
            foreach (var rubrica in resumo.Rubricas)
            {
                linhas.Add(new LinhaFolhaMontada(ArquivoRubricas, MapearRubrica(defRubricas, rubrica, competencia)));
            }
        }

        var defLancamentos = leiaute.RegistroPorArquivo(ArquivoLancamentos);
        if (defLancamentos is not null)
        {
            // Indice de incidencia por rubrica (S/N/X) para o TCE_4810 — somado por incidencia, nunca por nome.
            var rubricaPorCodigo = resumo.Rubricas.ToDictionary(rubrica => rubrica.Codigo, StringComparer.Ordinal);
            foreach (var lancamento in resumo.Lancamentos)
            {
                rubricaPorCodigo.TryGetValue(lancamento.CodigoRubrica, out var rubrica);
                linhas.Add(new LinhaFolhaMontada(
                    ArquivoLancamentos,
                    MapearLancamento(defLancamentos, resumo, lancamento, rubrica, competencia)));
            }
        }

        return linhas;
    }

    private static Dictionary<string, ValorCampo> MapearFuncionario(
        RegistroLeiauteDef definicao,
        ServidorFolhaResumo servidor,
        DateOnly competencia)
    {
        // TODO(validar-leiaute-folha-1099): nomes/posicoes EXATOS dos campos do TCE_4820.
        var fonte = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["DataAtualizacao"] = competencia,
            ["CodigoRegistroFuncionario"] = servidor.CodigoRegistro,
            ["Cpf"] = servidor.Cpf,
            ["Nome"] = servidor.Nome,
            ["DataNascimento"] = servidor.DataNascimento,
            ["DataAdmissao"] = servidor.DataAdmissao,
            ["DataDemissao"] = servidor.DataDemissao,
            ["CodigoCargo"] = servidor.CodigoCargo,
            ["NomeCargo"] = servidor.NomeCargo,
            ["Matricula"] = servidor.Matricula,
        };

        return PreencherGrade(definicao, fonte);
    }

    private static Dictionary<string, ValorCampo> MapearRubrica(
        RegistroLeiauteDef definicao,
        RubricaFolhaResumo rubrica,
        DateOnly competencia)
    {
        // TODO(validar-leiaute-folha-1099): nomes/posicoes EXATOS dos campos do TCE_4960 + Plano de Contas da Folha.
        var fonte = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["DataAtualizacao"] = competencia,
            ["Nome"] = rubrica.Descricao,
            ["BaseLegal"] = rubrica.BaseLegal,
            ["CodigoRubrica"] = CodigoRubricaNumerico(rubrica.Codigo),
            ["ContaPlanoFolhaTce"] = rubrica.ContaPlanoFolhaTce,
            ["Operacao"] = rubrica.Operacao,
        };

        return PreencherGrade(definicao, fonte);
    }

    private static Dictionary<string, ValorCampo> MapearLancamento(
        RegistroLeiauteDef definicao,
        ResumoFolhaTce resumo,
        LancamentoFolhaResumo lancamento,
        RubricaFolhaResumo? rubrica,
        DateOnly competencia)
    {
        // Sinal do valor pela operacao: Vantagem (+) credita; Desconto (-) debita (TCE_4810 campo 6/7).
        var ehDesconto = string.Equals(lancamento.Operacao, "D", StringComparison.OrdinalIgnoreCase);
        var valorComSinal = ehDesconto ? -Math.Abs(lancamento.Valor) : Math.Abs(lancamento.Valor);

        // TODO(validar-leiaute-folha-1099): nomes/posicoes EXATOS dos 27 campos do TCE_4810.
        var fonte = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["TipoFolha"] = (long)TipoFolhaCodigo(resumo.TipoFolha),
            ["CodigoRegistroFuncionario"] = lancamento.CodigoRegistroServidor,
            ["DataCompetencia"] = competencia,
            ["DataPagamento"] = resumo.DataPagamento,
            ["ValorOperacao"] = valorComSinal,
            ["Operacao"] = lancamento.Operacao,
            ["IncidenciaIrrf"] = Indicador(rubrica?.IncideIrrf),
            ["IncidenciaRpps"] = Indicador(rubrica?.IncideRpps),
            ["IncidenciaInss"] = Indicador(rubrica?.IncideInss),
            ["CodigoRubrica"] = CodigoRubricaNumerico(lancamento.CodigoRubrica),
        };

        return PreencherGrade(definicao, fonte);
    }

    // Preenche apenas os campos da GRADE; converte o valor da fonte ao ValorCampo do tipo do campo.
    private static Dictionary<string, ValorCampo> PreencherGrade(
        RegistroLeiauteDef definicao,
        Dictionary<string, object?> fonte)
    {
        var valores = new Dictionary<string, ValorCampo>(StringComparer.Ordinal);
        foreach (var campo in definicao.Campos)
        {
            fonte.TryGetValue(campo.Nome, out var bruto);
            valores[campo.Nome] = Converter(campo, bruto);
        }

        return valores;
    }

    private static ValorCampo Converter(CampoLeiaute campo, object? bruto)
        => campo.Tipo switch
        {
            TipoCampoLeiaute.Numerico => ValorCampo.Numerico(campo.Nome, bruto is long numero ? numero : 0),
            TipoCampoLeiaute.Valor => ValorCampo.Monetario(campo.Nome, bruto is decimal valor ? valor : 0m),
            TipoCampoLeiaute.Data => bruto is DateOnly data
                ? ValorCampo.DataCampo(campo.Nome, data)
                : ValorCampo.Caractere(campo.Nome, null), // data ausente -> emissor escreve zeros.
            _ => ValorCampo.Caractere(campo.Nome, bruto?.ToString()),
        };

    // Indicador S/N/X (X=NSA quando a rubrica nao foi encontrada). MT Vol. V §3.1.1.
    private static string Indicador(bool? incide)
        => incide switch
        {
            true => Sim.ToString(),
            false => Nao.ToString(),
            null => "X",
        };

    // TipoFolha numerico do TCE_4810 (1-Normal..9-Outros). TODO(validar-leiaute-folha-1099): tabela oficial.
    private static int TipoFolhaCodigo(string tipoFolha)
        => tipoFolha switch
        {
            "Mensal" => 1,
            "DecimoTerceiro" => 2,
            "Ferias" => 3,
            "Rescisao" => 4,
            _ => 9,
        };

    // O codigo de rubrica do TCE e Numerico(5); extrai os digitos do codigo S-1010 do RH.
    // TODO(validar-leiaute-folha-1099): mapeamento oficial codigo S-1010 -> codigo de rubrica TCE.
    private static long CodigoRubricaNumerico(string codigo)
    {
        var digitos = new string([.. codigo.Where(char.IsDigit)]);
        return digitos.Length > 0 && long.TryParse(digitos, NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
            ? numero
            : 0;
    }
}

/// <summary>
/// Uma linha de um arquivo da remessa de folha ja mapeada para os campos do leiaute (valores por nome de
/// campo), pronta para o <see cref="EmissorRegistroSiapc"/>. Estrutura de transporte do dominio.
/// </summary>
/// <param name="NomeArquivo">Arquivo fisico (TCE_4810/4820/4960).</param>
/// <param name="Valores">Valores por nome de campo (casam com a grade do registro).</param>
public sealed record LinhaFolhaMontada(string NomeArquivo, IReadOnlyDictionary<string, ValorCampo> Valores);
