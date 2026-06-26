using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes.Leiautes;

/// <summary>
/// Modelo (dirigido por DADOS) da grade posicional da REMESSA DE FOLHA ao TCE-RS (Resolucao 1099/2018 +
/// Manual Tecnico SIAPC Volume V, secao 3.2): os tres arquivos texto de largura fixa, ISO-8859-1 —
/// <c>TCE_4810.TXT</c> (lancamentos), <c>TCE_4820.TXT</c> (cadastro de funcionarios) e <c>TCE_4960.TXT</c>
/// (rubricas/base legal). REUSA o <see cref="EmissorRegistroSiapc"/> e o modelo de leiaute do M4
/// (<see cref="LeiauteSiapcModelo"/>), exatamente como a remessa SIAPC contabil.
/// </summary>
/// <remarks>
/// REGRA [OFICIAL] §16: a grade abaixo encarna as POSICOES/TAMANHOS/TIPOS OFICIAIS do
/// "MT-ASCE-0105 — SIAPC/PAD — Resumo do Leiaute dos Dados a Disposicao, Vol. V, Versao 2.0 (Set/2010)",
/// secao 3.2 (Folha de Pagamento): 3.2.1 TCE_4810 (pag. 6), 3.2.2 TCE_4820 (pag. 7) e 3.2.3 TCE_4960
/// (pag. 8). Cada campo lista o ordenamento de colunas exato da norma. Em producao a grade vem da
/// configuracao versionada por exercicio (JSON), nunca constante em C# (CLAUDE.md §7/§16) — esta classe e
/// apenas o SEED/baseline oficial. Campos "Reservado para uso futuro" sao emitidos com zeros (Numerico).
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
    /// Modelo da Res. 1099 para Prefeitura ('P') — grade OFICIAL do MT SIAPC Vol. V v2.0 (Set/2010) §3.2.
    /// Posicoes/tamanhos/tipos exatos conforme a norma; campos contiguos sem buraco/sobreposicao (o emissor
    /// posicional reusado do M4 exige faixa continua a partir da coluna 1).
    /// </summary>
    /// <returns>Modelo oficial (seed/baseline) do leiaute de folha.</returns>
    public static LeiauteSiapcModelo ModeloPadrao()
        => new(
            Codigo: Codigo,
            Versao: VersaoPadrao,
            TipoSetorGoverno: 'P',
            Registros:
            [
                // TCE_4810 — Folha de Pagamento (lancamentos). MT Vol. V v2.0 §3.2.1 (pag. 6) — 14 campos, 1..146.
                new RegistroModelo(
                    CodigoRegistro: "4810",
                    NomeArquivo: ArquivoLancamentos,
                    Campos:
                    [
                        new CampoModelo("TipoFolha", 1, 1, "Numerico", true, null),                       // 1-1
                        new CampoModelo("CodigoRegistroFuncionario", 2, 12, "Numerico", true, null),      // 2-13
                        new CampoModelo("DataCompetencia", 14, 8, "Data", true, null),                    // 14-21
                        new CampoModelo("DataPagamento", 22, 8, "Data", false, null),                     // 22-29
                        new CampoModelo("ReservadoUsoFuturo1", 30, 3, "Numerico", false, null),           // 30-32 (zeros)
                        new CampoModelo("ValorOperacao", 33, 17, "Valor", true, null),                    // 33-49
                        new CampoModelo("Operacao", 50, 1, "Caractere", true, null),                      // 50-50 (V/D/T/O)
                        new CampoModelo("IncidenciaIrrf", 51, 1, "Caractere", true, null),                // 51-51 (S/N)
                        new CampoModelo("BancoDepositoEntidade", 52, 5, "Numerico", false, null),         // 52-56 (Febraban)
                        new CampoModelo("AgenciaDepositoEntidade", 57, 5, "Numerico", false, null),       // 57-61
                        new CampoModelo("ContaDepositoEntidade", 62, 20, "Numerico", false, null),        // 62-81
                        new CampoModelo("BancoFuncionario", 82, 5, "Numerico", false, null),              // 82-86
                        new CampoModelo("AgenciaFuncionario", 87, 5, "Numerico", false, null),            // 87-91
                        new CampoModelo("ContaFuncionario", 92, 20, "Numerico", false, null),             // 92-111
                        new CampoModelo("Observacoes", 112, 30, "Caractere", false, null),                // 112-141
                        new CampoModelo("CodigoRubrica", 142, 5, "Numerico", true, null),                 // 142-146
                    ]),

                // TCE_4820 — Cadastro de Funcionarios. MT Vol. V v2.0 §3.2.2 (pag. 7) — 1..347.
                new RegistroModelo(
                    CodigoRegistro: "4820",
                    NomeArquivo: ArquivoFuncionarios,
                    Campos:
                    [
                        new CampoModelo("DataAtualizacao", 1, 8, "Data", true, null),                     // 1-8
                        new CampoModelo("CodigoRegistroFuncionario", 9, 12, "Numerico", true, null),      // 9-20
                        new CampoModelo("Cpf", 21, 14, "Numerico", true, null),                           // 21-34
                        new CampoModelo("Nome", 35, 70, "Caractere", true, null),                         // 35-104
                        new CampoModelo("DataNascimento", 105, 8, "Data", false, null),                   // 105-112
                        new CampoModelo("DataAdmissao", 113, 8, "Data", true, null),                      // 113-120
                        new CampoModelo("DataDemissao", 121, 8, "Data", false, null),                     // 121-128
                        new CampoModelo("CodigoCargo", 129, 8, "Numerico", true, null),                   // 129-136
                        new CampoModelo("NomeCargo", 137, 30, "Caractere", true, null),                   // 137-166
                        new CampoModelo("CodigoSetor", 167, 8, "Numerico", false, null),                  // 167-174
                        new CampoModelo("NomeSetor", 175, 30, "Caractere", false, null),                  // 175-204
                        new CampoModelo("Sexo", 205, 1, "Numerico", false, null),                         // 205-205 (1/2)
                        new CampoModelo("QtdDependentesIrrf", 206, 3, "Numerico", false, null),           // 206-208
                        new CampoModelo("SituacaoFuncionario", 209, 2, "Numerico", false, null),          // 209-210 (01/02/03/99)
                        new CampoModelo("RegimeJuridico", 211, 1, "Caractere", false, null),              // 211-211 (E/C/O)
                        new CampoModelo("NaturezaCargo", 212, 1, "Caractere", false, null),               // 212-212 (E/C/T/O)
                        new CampoModelo("RegimePrevidenciario", 213, 2, "Numerico", false, null),         // 213-214 (01/02/99)
                        new CampoModelo("RegistroGeralRg", 215, 14, "Numerico", false, null),             // 215-228
                        new CampoModelo("Cbo", 229, 6, "Numerico", false, null),                          // 229-234
                        new CampoModelo("NitPisPasep", 235, 11, "Numerico", false, null),                 // 235-245
                        new CampoModelo("CategoriaTrabalhador", 246, 2, "Numerico", false, null),         // 246-247
                        new CampoModelo("Endereco", 248, 30, "Caractere", false, null),                   // 248-277
                        new CampoModelo("Cidade", 278, 30, "Caractere", false, null),                     // 278-307
                        new CampoModelo("Uf", 308, 2, "Caractere", false, null),                          // 308-309
                        new CampoModelo("Cep", 310, 8, "Numerico", false, null),                          // 310-317
                        new CampoModelo("Observacoes", 318, 30, "Caractere", false, null),                // 318-347
                    ]),

                // TCE_4960 — Tabela de Vantagens/Descontos/Totalizadores (rubricas). MT Vol. V v2.0 §3.2.3 (pag. 8) — 1..211.
                new RegistroModelo(
                    CodigoRegistro: "4960",
                    NomeArquivo: ArquivoRubricas,
                    Campos:
                    [
                        new CampoModelo("DataAtualizacao", 1, 8, "Data", true, null),                     // 1-8
                        new CampoModelo("ReservadoUsoFuturo1", 9, 3, "Numerico", false, null),            // 9-11 (zeros)
                        new CampoModelo("Nome", 12, 45, "Caractere", true, null),                         // 12-56
                        new CampoModelo("BaseLegal", 57, 150, "Caractere", false, null),                  // 57-206
                        new CampoModelo("CodigoRubrica", 207, 5, "Numerico", true, null),                 // 207-211
                    ]),
            ]);
}
