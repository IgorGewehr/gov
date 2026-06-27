using System.Globalization;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes.Leiautes;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Catálogo de leiautes (dev/demonstração): aceita o leiaute SIAPC, RESOLVE a grade posicional a partir do
/// modelo de dados versionado (<see cref="LeiauteSiapcSeed"/>) e deriva data-limite/identificação do ente de
/// forma determinística. A implementação real carrega a Resolução TCE-RS vigente e os prazos/CNPJ
/// parametrizados por tenant (I-11). A grade exata é <c>// TODO(validar-leiaute-MT-2026)</c>.
/// </summary>
public sealed class SimuladoLeiauteCatalogo(IConfiguration configuration) : ILeiauteCatalogo
{
    private const string CodigoSuportado = "SIAPC";

    /// <summary>
    /// Prazo legal padrão de remessa: <b>30 dias corridos após o encerramento do período de competência</b>
    /// (TCE-RS Resolução 1099/2018 — folha mensal desde jan/2019). Usado quando o tenant não parametriza o
    /// prazo. Não é número mágico de cálculo: é o default normativo, sobrescrevível por
    /// <c>Transparencia:Tce:DiasPrazoRemessa</c> (CLAUDE.md §7 — parametrizável por tenant).
    /// </summary>
    private const int DiasPrazoRemessaPadraoRes1099 = 30;

    /// <inheritdoc />
    public Task<bool> SuportaAsync(Leiaute leiaute, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(leiaute);
        var suportado = !string.IsNullOrWhiteSpace(leiaute.Versao)
            && (string.Equals(leiaute.Codigo, CodigoSuportado, StringComparison.OrdinalIgnoreCase)
                || string.Equals(leiaute.Codigo, LeiauteFolhaTceSeed.Codigo, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(suportado);
    }

    /// <inheritdoc />
    public Task<LeiauteSiapc> ResolverAsync(Leiaute leiaute, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(leiaute);

        // REMESSA DE FOLHA ao TCE-RS (Res. 1099 / SIAPC Vol. V v2.0 §3.2): TCE_4810/4820/4960.
        if (string.Equals(leiaute.Codigo, LeiauteFolhaTceSeed.Codigo, StringComparison.OrdinalIgnoreCase))
        {
            var modeloFolha = LeiauteFolhaTceSeed.ModeloPadrao() with { Versao = leiaute.Versao };
            return Task.FromResult(LeiauteFolhaTceSeed.Construir(modeloFolha));
        }

        if (!string.Equals(leiaute.Codigo, CodigoSuportado, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Leiaute '{leiaute.Codigo}' não suportado.");
        }

        // Modelo dirigido por dados (seed); em produção viria da configuração/JSON do exercício.
        var modelo = LeiauteSiapcSeed.ModeloPadrao2026() with { Versao = leiaute.Versao };
        return Task.FromResult(LeiauteSiapcSeed.Construir(modelo));
    }

    /// <inheritdoc />
    public Task<DateOnly> ObterDataLimiteAsync(Periodo periodo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        // TCE-RS Res. 1099/2018: prazo = encerramento do período + N dias CORRIDOS (folha mensal: 30 dias).
        // N é parametrizável por tenant (config); o default normativo (30) só vale se não houver parâmetro.
        var diasPrazo = ResolverDiasPrazoRemessa();
        var (_, fimPeriodo) = IntervaloPeriodo(periodo);
        var dataLimite = fimPeriodo.AddDays(diasPrazo);
        return Task.FromResult(dataLimite);
    }

    /// <summary>
    /// Resolve o nº de dias de prazo da remessa, parametrizado por tenant via
    /// <c>Transparencia:Tce:DiasPrazoRemessa</c>; default = <see cref="DiasPrazoRemessaPadraoRes1099"/>
    /// (Res. 1099/2018). Valor inválido (≤ 0 ou não numérico) cai no default normativo.
    /// </summary>
    private int ResolverDiasPrazoRemessa()
    {
        var bruto = configuration["Transparencia:Tce:DiasPrazoRemessa"];
        return int.TryParse(bruto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dias) && dias > 0
            ? dias
            : DiasPrazoRemessaPadraoRes1099;
    }

    /// <inheritdoc />
    public Task<IdentificacaoEnteRemessa> ObterIdentificacaoEnteAsync(Periodo periodo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        // CNPJ/nome parametrizáveis por tenant; placeholder determinístico para dev.
        var cnpj = configuration["Transparencia:Tce:Cnpj"] ?? "99999999000199";
        var nomeSetor = configuration["Transparencia:Tce:NomeSetorGoverno"] ?? "PREFEITURA MUNICIPAL";

        var (inicio, fim) = IntervaloPeriodo(periodo);

        // Código da Remessa gerado pelo PRÓPRIO ENTE (Numérico 12). Dev: determinístico por período.
        var codigoRemessa = (long)(periodo.Exercicio * 100 + Math.Max(periodo.Numero, 0));

        return Task.FromResult(new IdentificacaoEnteRemessa(cnpj, nomeSetor, inicio, fim, codigoRemessa));
    }

    private static (DateOnly Inicio, DateOnly Fim) IntervaloPeriodo(Periodo periodo)
    {
        switch (periodo.Tipo)
        {
            case TipoPeriodo.Mensal:
                var inicioMes = new DateOnly(periodo.Exercicio, periodo.Numero, 1);
                return (inicioMes, inicioMes.AddMonths(1).AddDays(-1));
            case TipoPeriodo.Bimestre:
                var mesIniBim = (periodo.Numero - 1) * 2 + 1;
                var inicioBim = new DateOnly(periodo.Exercicio, mesIniBim, 1);
                return (inicioBim, inicioBim.AddMonths(2).AddDays(-1));
            case TipoPeriodo.Quadrimestre:
                var mesIniQua = (periodo.Numero - 1) * 4 + 1;
                var inicioQua = new DateOnly(periodo.Exercicio, mesIniQua, 1);
                return (inicioQua, inicioQua.AddMonths(4).AddDays(-1));
            default:
                return (new DateOnly(periodo.Exercicio, 1, 1), new DateOnly(periodo.Exercicio, 12, 31));
        }
    }
}
