using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Seguranca do CSV publico/aberto da MSC (OWASP CSV Injection + RFC 4180). Valores como a conta PCASP e os
/// valores de informacao complementar (IC) carregam texto de origem externa; uma celula iniciada por
/// <c>= + - @</c> (ou TAB/CR) vira FORMULA executavel ao abrir no Excel/LibreOffice/Sheets. O gerador deve
/// neutralizar o gatilho (prefixo aspa simples) e citar campos com CR isolado (RFC 4180 §2.6) para nao
/// quebrar a linha do dataset. O payload e injetado como VALOR de um par IC oficial (<c>FR=&lt;payload&gt;</c>),
/// que vira uma celula <c>IC</c> do leiaute (Regras Gerais MSC 2026). O PO obrigatorio (5 digitos) e
/// fornecido valido em TODAS as linhas, pois o gerador agora valida fail-closed o PO antes de emitir (P0-1).
/// </summary>
public sealed class GeradorMscCsvSegurancaTests
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // PO valido (5 digitos) exigido em todas as linhas pela validacao dura do SICONFI (IC nº1).
    private const string PoderOrgaoValido = "01001";

    // Fake do provedor de Cod.Siconfi (IBGE + EX): determinístico, sem configuração.
    private sealed class IdentificacaoEnteSiconfiFake : IIdentificacaoEnteSiconfi
    {
        public Task<string> ObterCodigoSiconfiAsync(CancellationToken cancellationToken)
            => Task.FromResult("4312104EX");
    }

    private static string GerarCsv(string payload)
    {
        // Matriz balanceada minima (debito == credito), com PO valido em ambas as linhas (exigencia dura do
        // SICONFI) e o texto suspeito como VALOR do par IC "FR" (Fonte de Recurso). Patrimonial fecha D=C.
        var declaracao = DeclaracaoFiscal.ConsolidarMatriz(
            TenantA,
            TipoDeclaracaoFiscal.Msc,
            2026,
            Competencia.De(2026, 6),
            bimestre: null,
            quadrimestre: null,
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 6, 5),
            MatrizSaldos.Montar(
            [
                LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido};FR={payload}"),
                LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"),
            ]));

        var artefato = new GeradorMscCsv(new IdentificacaoEnteSiconfiFake())
            .GerarAsync(declaracao, default).GetAwaiter().GetResult();

        using var memoria = new MemoryStream(artefato.Conteudo.ToArray());
        using var zip = new ZipArchive(memoria, ZipArchiveMode.Read);
        var entrada = zip.Entries.Single(e => e.Name.EndsWith(".csv", StringComparison.Ordinal));
        using var leitor = new StreamReader(entrada.Open(), Encoding.UTF8);
        return leitor.ReadToEnd();
    }

    [Theory] // OWASP CSV Injection: celula iniciada por gatilho de formula e neutralizada com prefixo aspa simples.
    [InlineData("=SOMA(A1:A9)")]
    [InlineData("+1+1")]
    [InlineData("-2+3")]
    [InlineData("@SUM(1)")]
    public void Neutraliza_celula_iniciada_por_gatilho_de_formula(string payload)
    {
        var csv = GerarCsv(payload);

        // O conteudo aparece SEMPRE precedido pela aspa simples — nunca como celula que comeca pelo gatilho cru.
        csv.Should().Contain("'" + payload, "a celula deve ser neutralizada para nao virar formula ao abrir na planilha");
    }

    [Fact] // RFC 4180 §2.6: campo com CR isolado (sem LF) deve ser CITADO para nao quebrar o registro do dataset.
    public void Cita_campo_com_cr_isolado_para_nao_corromper_a_linha()
    {
        var csv = GerarCsv("antes\rdepois");

        csv.Should().Contain("\"antes\rdepois\"", "CR isolado exige aspas (RFC 4180) — senao a coluna seguinte e lida errada");
    }
}
