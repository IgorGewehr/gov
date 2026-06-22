using System.Globalization;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Leitura simulada (dev/demonstração) dos itens consolidados da transparência para um período. Produz
/// linhas determinísticas mapeadas para a grade do leiaute resolvido, exercitando a montagem posicional do
/// pacote. A implementação real consulta o read model materializado a partir dos Integration Events
/// consumidos de outros módulos (I-13).
/// </summary>
public sealed class SimuladoPublicacaoTransparenciaRepository : IPublicacaoTransparenciaRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ItemConsolidado>> ObterItensConsolidadosAsync(Periodo periodo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        var referencia = periodo.ToString();
        IReadOnlyList<ItemConsolidado> itens =
        [
            new("0000", string.Create(CultureInfo.InvariantCulture, $"CABECALHO|{referencia}")),
            new("0100", string.Create(CultureInfo.InvariantCulture, $"BALANCO_ORCAMENTARIO|{referencia}")),
            new("9999", string.Create(CultureInfo.InvariantCulture, $"RODAPE|{referencia}")),
        ];

        return Task.FromResult(itens);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LinhaConsolidada>> ObterLinhasConsolidadasAsync(
        Periodo periodo,
        LeiauteSiapc leiaute,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(leiaute);

        var linhas = new List<LinhaConsolidada>();
        var dataReferencia = new DateOnly(periodo.Exercicio, NormalizarMes(periodo), 1);

        // Alimenta cada registro/arquivo do leiaute com 2 linhas determinísticas balanceadas (débito/crédito),
        // mapeando os valores por nome de campo conforme a grade resolvida.
        foreach (var definicao in leiaute.Registros)
        {
            linhas.Add(new LinhaConsolidada(
                definicao.NomeArquivo,
                MontarLinha(definicao, "111110100", 1_000.00m, dataReferencia)));
            linhas.Add(new LinhaConsolidada(
                definicao.NomeArquivo,
                MontarLinha(definicao, "211110000", 1_000.00m, dataReferencia)));
        }

        return Task.FromResult<IReadOnlyList<LinhaConsolidada>>(linhas);
    }

    private static List<ValorCampo> MontarLinha(
        RegistroLeiauteDef definicao,
        string conta,
        decimal valor,
        DateOnly dataReferencia)
    {
        var valores = new List<ValorCampo>();
        foreach (var campo in definicao.Campos)
        {
            var valorCampo = campo.Tipo switch
            {
                TipoCampoLeiaute.Numerico => ValorCampo.Numerico(campo.Nome, NumeroPadrao(campo.Nome)),
                TipoCampoLeiaute.Valor => ValorCampo.Monetario(campo.Nome, valor),
                TipoCampoLeiaute.Data => ValorCampo.DataCampo(campo.Nome, dataReferencia),
                _ => ValorCampo.Caractere(campo.Nome, TextoPadrao(campo.Nome, conta)),
            };
            valores.Add(valorCampo);
        }

        return valores;
    }

    private static long NumeroPadrao(string nomeCampo)
        => string.Equals(nomeCampo, "TipoRegistro", StringComparison.OrdinalIgnoreCase) ? 10 : 0;

    private static string TextoPadrao(string nomeCampo, string conta)
        => string.Equals(nomeCampo, "ContaContabil", StringComparison.OrdinalIgnoreCase) ? conta : nomeCampo;

    private static int NormalizarMes(Periodo periodo)
        => periodo.Tipo switch
        {
            TipoPeriodo.Mensal => periodo.Numero,
            TipoPeriodo.Bimestre => Math.Min(12, periodo.Numero * 2),
            TipoPeriodo.Quadrimestre => Math.Min(12, periodo.Numero * 4),
            _ => 12,
        };
}
