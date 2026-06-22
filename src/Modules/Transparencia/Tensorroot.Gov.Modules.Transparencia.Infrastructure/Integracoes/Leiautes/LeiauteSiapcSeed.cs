using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes.Leiautes;

/// <summary>
/// Carrega a grade posicional do leiaute SIAPC a partir de um modelo de DADOS versionado (DTO desserializável
/// de JSON), nunca constante em C# (CLAUDE.md §7/§16). O catálogo (<c>SimuladoLeiauteCatalogo</c>) usa este
/// seed; em produção a fonte é a configuração/JSON do exercício vigente.
/// </summary>
/// <remarks>
/// O leiaute conferido é de 2010 e MUDA em 2026 (BP/BF + DFC). A grade abaixo é um ESQUELETO ESTRUTURAL
/// mínimo para exercitar o emissor posicional — os campos/posições/tamanhos EXATOS são
/// <c>// TODO(validar-leiaute-MT-2026)</c> e devem vir do MT SIAPC 2026 antes de produção.
/// </remarks>
public static class LeiauteSiapcSeed
{
    /// <summary>Constrói o leiaute SIAPC do exercício a partir do modelo de dados (DTO).</summary>
    /// <param name="modelo">Modelo desserializado (JSON/configuração versionada).</param>
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
    /// Modelo-esqueleto padrão do SIAPC 2026 para Prefeitura ('P'). ESTRUTURAL: a grade exata é
    /// <c>// TODO(validar-leiaute-MT-2026)</c>. Usado pelo catálogo simulado e como fallback de dev.
    /// </summary>
    /// <returns>Modelo padrão (não oficial) do leiaute.</returns>
    public static LeiauteSiapcModelo ModeloPadrao2026()
        => new(
            Codigo: "SIAPC",
            Versao: "2026",
            TipoSetorGoverno: 'P',
            Registros:
            [
                // TODO(validar-leiaute-MT-2026): registro/arquivo BALANCETE e seus campos exatos (MT Vol. V).
                new RegistroModelo(
                    CodigoRegistro: "10",
                    NomeArquivo: "BALANCETE.TXT",
                    Campos:
                    [
                        new CampoModelo("TipoRegistro", 1, 2, "Numerico", true, null),
                        new CampoModelo("ContaContabil", 3, 20, "Caractere", true, null),
                        new CampoModelo("SaldoAnterior", 23, 16, "Valor", true, null),
                        new CampoModelo("Movimento", 39, 16, "Valor", false, null),
                        new CampoModelo("SaldoAtual", 55, 16, "Valor", true, null),
                        new CampoModelo("DataReferencia", 71, 8, "Data", true, null),
                    ]),
            ]);
}

/// <summary>DTO desserializável do leiaute SIAPC (configuração versionada por exercício).</summary>
/// <param name="Codigo">Código do leiaute (ex.: "SIAPC").</param>
/// <param name="Versao">Versão/exercício (ex.: "2026").</param>
/// <param name="TipoSetorGoverno">Tipo de Setor de Governo (1 letra).</param>
/// <param name="Registros">Definições de registro/arquivo.</param>
public sealed record LeiauteSiapcModelo(
    string Codigo,
    string Versao,
    char TipoSetorGoverno,
    IReadOnlyList<RegistroModelo> Registros);

/// <summary>DTO de um registro/arquivo do leiaute.</summary>
/// <param name="CodigoRegistro">Código do registro.</param>
/// <param name="NomeArquivo">Nome do arquivo físico.</param>
/// <param name="Campos">Campos posicionais.</param>
public sealed record RegistroModelo(string CodigoRegistro, string NomeArquivo, IReadOnlyList<CampoModelo> Campos);

/// <summary>DTO de um campo posicional do leiaute.</summary>
/// <param name="Nome">Nome lógico do campo.</param>
/// <param name="PosicaoInicial">Posição inicial 1-based.</param>
/// <param name="Tamanho">Largura fixa.</param>
/// <param name="Tipo">Tipo do campo (Caractere/Numerico/Valor/Data).</param>
/// <param name="Obrigatorio">Se é obrigatório.</param>
/// <param name="Preenchimento">Regra de preenchimento (opcional; deriva do tipo).</param>
public sealed record CampoModelo(
    string Nome,
    int PosicaoInicial,
    int Tamanho,
    string Tipo,
    bool Obrigatorio,
    string? Preenchimento);
