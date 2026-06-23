using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes.Leiautes;

/// <summary>
/// Modelo (dirigido por DADOS) da grade posicional da REMESSA DE FOLHA ao TCE-RS (Resolucao 1099/2018 +
/// Manual Tecnico SIAPC Volume V, secao 3.1): os tres arquivos texto de largura fixa, ISO-8859-1 —
/// <c>TCE_4810.TXT</c> (lancamentos), <c>TCE_4820.TXT</c> (cadastro de funcionarios) e <c>TCE_4960.TXT</c>
/// (rubricas/base legal). REUSA o <see cref="EmissorRegistroSiapc"/> e o modelo de leiaute do M4
/// (<see cref="LeiauteSiapcModelo"/>), exatamente como a remessa SIAPC contabil.
/// </summary>
/// <remarks>
/// REGRA [OFICIAL] §16: o leiaute de folha-TCE da Res. 1099 e OFICIAL. A ESTRUTURA abaixo e fiel ao MT
/// Vol. V (tipos/larguras/ordem dos campos capturados na pesquisa), mas as posicoes/tamanhos EXATOS de cada
/// campo devem ser conferidos no documento oficial antes de producao — marcados
/// <c>// TODO(validar-leiaute-folha-1099)</c>. Em producao a grade vem da configuracao versionada por
/// exercicio (JSON), nunca constante em C# (CLAUDE.md §7/§16).
/// </remarks>
public static class LeiauteFolhaTceSeed
{
    /// <summary>Codigo do leiaute de folha ao TCE-RS (distinto do SIAPC contabil).</summary>
    public const string Codigo = "FOLHA-TCE";

    /// <summary>Versao/exercicio default (Res. 1099/2018; reconfirmar vigencia no exercicio).</summary>
    public const string VersaoPadrao = "1099";

    /// <summary>Nome do arquivo de lancamentos da folha (vantagens/descontos/totalizadores).</summary>
    public const string ArquivoLancamentos = "TCE_4810.TXT";

    /// <summary>Nome do arquivo de cadastro de funcionarios.</summary>
    public const string ArquivoFuncionarios = "TCE_4820.TXT";

    /// <summary>Nome do arquivo de rubricas (vantagens/descontos/totalizadores + base legal).</summary>
    public const string ArquivoRubricas = "TCE_4960.TXT";

    /// <summary>Constroi o <see cref="LeiauteSiapc"/> da folha a partir do modelo de dados (DTO).</summary>
    /// <param name="modelo">Modelo desserializado (JSON/configuracao versionada).</param>
    /// <returns>O <see cref="LeiauteSiapc"/> resolvido.</returns>
    public static LeiauteSiapc Construir(LeiauteSiapcModelo modelo)
    {
        ArgumentNullException.ThrowIfNull(modelo);

        var registros = modelo.Registros
            .Select(registro => RegistroLeiauteDef.Definir(
                registro.CodigoRegistro,
                registro.NomeArquivo,
                registro.Campos
                    .Select(campo => CampoLeiaute.Definir(
                        campo.Nome,
                        campo.PosicaoInicial,
                        campo.Tamanho,
                        Enum.Parse<TipoCampoLeiaute>(campo.Tipo, ignoreCase: true),
                        campo.Obrigatorio,
                        string.IsNullOrWhiteSpace(campo.Preenchimento)
                            ? PreenchimentoCampo.PadraoDoTipo
                            : Enum.Parse<PreenchimentoCampo>(campo.Preenchimento, ignoreCase: true)))
                    .ToList()))
            .ToList();

        return LeiauteSiapc.Definir(modelo.Codigo, modelo.Versao, modelo.TipoSetorGoverno, registros);
    }

    /// <summary>
    /// Modelo da Res. 1099 para Prefeitura ('P'). ESTRUTURAL e fiel ao MT Vol. V §3.1; posicoes/tamanhos
    /// exatos ficam <c>// TODO(validar-leiaute-folha-1099)</c>. Campos contiguos a partir da coluna 1 (o
    /// emissor exige faixa sem buraco/sobreposicao — defesa em profundidade do M4).
    /// </summary>
    /// <returns>Modelo padrao (estrutural) do leiaute de folha.</returns>
    public static LeiauteSiapcModelo ModeloPadrao()
        => new(
            Codigo: Codigo,
            Versao: VersaoPadrao,
            TipoSetorGoverno: 'P',
            Registros:
            [
                // TCE_4810 — Folha de Pagamento (lancamentos). MT Vol. V §3.1.1.
                // TODO(validar-leiaute-folha-1099): grade EXATA (ver pesquisa §3.1 — 27 campos; abaixo o
                // subconjunto minimo, contiguo, para exercitar o emissor posicional reusado do M4).
                new RegistroModelo(
                    CodigoRegistro: "4810",
                    NomeArquivo: ArquivoLancamentos,
                    Campos:
                    [
                        new CampoModelo("TipoFolha", 1, 1, "Numerico", true, null),
                        new CampoModelo("CodigoRegistroFuncionario", 2, 12, "Caractere", true, null),
                        new CampoModelo("DataCompetencia", 14, 8, "Data", true, null),
                        new CampoModelo("DataPagamento", 22, 8, "Data", false, null),
                        new CampoModelo("ValorOperacao", 30, 17, "Valor", true, null),
                        new CampoModelo("Operacao", 47, 1, "Caractere", true, null),
                        new CampoModelo("IncidenciaIrrf", 48, 1, "Caractere", true, null),
                        new CampoModelo("IncidenciaRpps", 49, 1, "Caractere", true, null),
                        new CampoModelo("IncidenciaInss", 50, 1, "Caractere", true, null),
                        new CampoModelo("CodigoRubrica", 51, 5, "Numerico", true, null),
                    ]),

                // TCE_4820 — Cadastro de Funcionarios. MT Vol. V §3.1.2.
                // TODO(validar-leiaute-folha-1099): tabela completa de campos (este modelo captura o nucleo).
                new RegistroModelo(
                    CodigoRegistro: "4820",
                    NomeArquivo: ArquivoFuncionarios,
                    Campos:
                    [
                        new CampoModelo("DataAtualizacao", 1, 8, "Data", true, null),
                        new CampoModelo("CodigoRegistroFuncionario", 9, 12, "Caractere", true, null),
                        new CampoModelo("Cpf", 21, 14, "Caractere", true, null),
                        new CampoModelo("Nome", 35, 70, "Caractere", true, null),
                        new CampoModelo("DataNascimento", 105, 8, "Data", false, null),
                        new CampoModelo("DataAdmissao", 113, 8, "Data", true, null),
                        new CampoModelo("DataDemissao", 121, 8, "Data", false, null),
                        new CampoModelo("CodigoCargo", 129, 8, "Caractere", true, null),
                        new CampoModelo("NomeCargo", 137, 30, "Caractere", true, null),
                        new CampoModelo("Matricula", 167, 14, "Caractere", true, null),
                    ]),

                // TCE_4960 — Tabela de Vantagens/Descontos/Totalizadores (rubricas). MT Vol. V §3.1.3.
                // TODO(validar-leiaute-folha-1099): tabela completa de campos + Plano de Contas da Folha (cod. TCE).
                new RegistroModelo(
                    CodigoRegistro: "4960",
                    NomeArquivo: ArquivoRubricas,
                    Campos:
                    [
                        new CampoModelo("DataAtualizacao", 1, 8, "Data", true, null),
                        new CampoModelo("Nome", 9, 45, "Caractere", true, null),
                        new CampoModelo("BaseLegal", 54, 150, "Caractere", false, null),
                        new CampoModelo("CodigoRubrica", 204, 5, "Numerico", true, null),
                        new CampoModelo("ContaPlanoFolhaTce", 209, 6, "Caractere", false, null),
                        new CampoModelo("Operacao", 215, 1, "Caractere", true, null),
                    ]),
            ]);
}
