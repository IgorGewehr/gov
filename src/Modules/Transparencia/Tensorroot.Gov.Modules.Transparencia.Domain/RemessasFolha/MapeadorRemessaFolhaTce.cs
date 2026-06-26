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
/// REGRA [OFICIAL] §16: o casamento campo->valor segue a grade OFICIAL do MT SIAPC Vol. V v2.0 (Set/2010)
/// §3.2 (TCE_4810 §3.2.1, TCE_4820 §3.2.2, TCE_4960 §3.2.3), exatamente como semeada em
/// <c>LeiauteFolhaTceSeed</c>. Mapeia apenas campos cujo nome existe na grade resolvida; campos da grade
/// nao alimentados pelo resumo ficam com o default do tipo (vazio/zeros para Reservado/uso futuro), e a
/// obrigatoriedade e checada na PRE-VALIDACAO LOCAL. NB.: no leiaute v2.0 o unico indicador de incidencia
/// do TCE_4810 e o do IRRF (col. 51) — nao ha colunas de RPPS/INSS no 4810; e o TCE_4960 nao possui colunas
/// de "operacao" nem "plano de contas da folha" (a operacao V/D/T/O e gravada por LANCAMENTO no TCE_4810).
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
        // Grade OFICIAL TCE_4820 (MT Vol. V v2.0 §3.2.2). Campos do cadastro que o resumo NAO carrega
        // (Setor, Sexo, Situacao, RG, CBO, NIT, endereco, etc.) ficam com o default do tipo na grade.
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
            ["RegimePrevidenciario"] = RegimePrevidenciarioCodigo(servidor.Regime),
        };

        return PreencherGrade(definicao, fonte);
    }

    private static Dictionary<string, ValorCampo> MapearRubrica(
        RegistroLeiauteDef definicao,
        RubricaFolhaResumo rubrica,
        DateOnly competencia)
    {
        // Grade OFICIAL TCE_4960 (MT Vol. V v2.0 §3.2.3): apenas Data, Reservado, Nome(45), BaseLegal(150),
        // Codigo(5). NB.: o leiaute v2.0 NAO tem coluna de "operacao" nem de "plano de contas da folha" no
        // 4960 — a operacao V/D/T/O e gravada por LANCAMENTO no TCE_4810 (col. 50).
        var fonte = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["DataAtualizacao"] = competencia,
            ["Nome"] = rubrica.Descricao,
            ["BaseLegal"] = rubrica.BaseLegal,
            ["CodigoRubrica"] = CodigoRubricaNumerico(rubrica.Codigo),
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
        // Sinal do valor pela operacao: Vantagem (+) credita; Desconto (-) debita. No TCE_4810 o sinal
        // integra o campo Valor (col. 33-49, tipo Valor) e a Identificacao da Operacao vai na col. 50.
        var ehDesconto = string.Equals(lancamento.Operacao, "D", StringComparison.OrdinalIgnoreCase);
        var valorComSinal = ehDesconto ? -Math.Abs(lancamento.Valor) : Math.Abs(lancamento.Valor);

        // Grade OFICIAL TCE_4810 (MT Vol. V v2.0 §3.2.1): a operacao V/D/T/O vai na col. 50 e o UNICO
        // indicador de incidencia e o do IRRF (col. 51). Os campos bancarios (52-111) e Observacoes (112-141)
        // nao sao alimentados pelo resumo e saem com o default do tipo; Reservado (30-32) sai com zeros.
        var fonte = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["TipoFolha"] = (long)TipoFolhaCodigo(resumo.TipoFolha),
            ["CodigoRegistroFuncionario"] = CodigoRubricaNumerico(lancamento.CodigoRegistroServidor),
            ["DataCompetencia"] = competencia,
            ["DataPagamento"] = resumo.DataPagamento,
            ["ValorOperacao"] = valorComSinal,
            ["Operacao"] = OperacaoTce(lancamento.Operacao),
            ["IncidenciaIrrf"] = Indicador(rubrica?.IncideIrrf),
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
            // Numerico aceita long direto OU string com digitos (CPF, conta, codigo de registro do funcionario).
            TipoCampoLeiaute.Numerico => ValorCampo.Numerico(campo.Nome, bruto switch
            {
                long numero => numero,
                string texto => SomenteDigitos(texto),
                _ => 0,
            }),
            TipoCampoLeiaute.Valor => ValorCampo.Monetario(campo.Nome, bruto is decimal valor ? valor : 0m),
            TipoCampoLeiaute.Data => bruto is DateOnly data
                ? ValorCampo.DataCampo(campo.Nome, data)
                : ValorCampo.Caractere(campo.Nome, null), // data ausente -> emissor escreve zeros.
            _ => ValorCampo.Caractere(campo.Nome, bruto?.ToString()),
        };

    // Indicador de incidencia do IRRF S/N (TCE_4810 col. 51, MT Vol. V v2.0 §3.2.1). X=NSA quando a rubrica
    // do lancamento nao foi encontrada na tabela de rubricas do resumo (defesa em profundidade).
    private static string Indicador(bool? incide)
        => incide switch
        {
            true => Sim.ToString(),
            false => Nao.ToString(),
            null => "X",
        };

    // Operacao do TCE_4810/Identificacao da Operacao (col. 50): V=Vantagem, D=Desconto, T=Totalizador, O=Outros.
    private static string OperacaoTce(string operacao)
        => operacao?.Trim().ToUpperInvariant() switch
        {
            "V" or "VANTAGEM" => "V",
            "D" or "DESCONTO" => "D",
            "T" or "TOTALIZADOR" => "T",
            _ => "O",
        };

    // Codigo do Tipo da Folha do TCE_4810 (col. 1-1, MT Vol. V v2.0 §3.2.1): 1-Normal, 2-13o, 3-Ferias,
    // 4-Rescisao, 5-Complementar, 6-Afastamento, 9-Outros.
    private static int TipoFolhaCodigo(string tipoFolha)
        => tipoFolha switch
        {
            "Mensal" => 1,
            "DecimoTerceiro" => 2,
            "Ferias" => 3,
            "Rescisao" => 4,
            "Complementar" => 5,
            "Afastamento" => 6,
            _ => 9,
        };

    // Regime Previdenciario do TCE_4820 (col. 213-214, MT Vol. V v2.0 §3.2.2): 01-RPPS, 02-RGPS, 99-Outros.
    private static long RegimePrevidenciarioCodigo(string regime)
        => regime?.Trim().ToUpperInvariant() switch
        {
            "RPPS" => 1,
            "RGPS" or "INSS" => 2,
            _ => 99,
        };

    // O codigo de rubrica do TCE e Numerico(5); extrai os digitos do codigo S-1010 do RH.
    private static long CodigoRubricaNumerico(string codigo) => SomenteDigitos(codigo);

    private static long SomenteDigitos(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return 0;
        }

        var digitos = new string([.. valor.Where(char.IsDigit)]);
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
