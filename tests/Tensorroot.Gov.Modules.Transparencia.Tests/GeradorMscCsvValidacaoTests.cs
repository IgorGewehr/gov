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
/// P0-1/P0-2 (AUDITORIA-FINAL): o caminho de geracao do CSV da MSC (SICONFI) deve aplicar FAIL-CLOSED as
/// validacoes DURAS que se perdem na ponte de integracao Financas->Transparencia (so o saldo final, em texto
/// livre, chega ate aqui). O gerador NAO pode emitir um artefato que o e-Validador SICONFI rejeitaria.
/// Regras (Regras Gerais MSC 2026, Anexo I Port. STN 642/2019): (1) Poder/Orgao (PO) obrigatorio em TODAS as
/// linhas (IC nº1); (2) balanco D=C por CLASSE contabil e global; e o PO sai SEMPRE no slot fixo TIPO1/IC1.
/// </summary>
public sealed class GeradorMscCsvValidacaoTests
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string PoderOrgaoValido = "01001";

    private sealed class IdentificacaoEnteSiconfiFake : IIdentificacaoEnteSiconfi
    {
        public Task<string> ObterCodigoSiconfiAsync(CancellationToken cancellationToken)
            => Task.FromResult("4312104EX");
    }

    private static DeclaracaoFiscal Declaracao(params LinhaContabil[] linhas)
        => DeclaracaoFiscal.ConsolidarMatriz(
            TenantA,
            TipoDeclaracaoFiscal.Msc,
            2026,
            Competencia.De(2026, 6),
            bimestre: null,
            quadrimestre: null,
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 6, 5),
            MatrizSaldos.Montar(linhas));

    private static string GerarCsv(DeclaracaoFiscal declaracao)
    {
        var artefato = new GeradorMscCsv(new IdentificacaoEnteSiconfiFake())
            .GerarAsync(declaracao, default).GetAwaiter().GetResult();

        using var memoria = new MemoryStream(artefato.Conteudo.ToArray());
        using var zip = new ZipArchive(memoria, ZipArchiveMode.Read);
        var entrada = zip.Entries.Single(e => e.Name.EndsWith(".csv", StringComparison.Ordinal));
        using var leitor = new StreamReader(entrada.Open(), Encoding.UTF8);
        return leitor.ReadToEnd();
    }

    [Fact] // P0-1: linha SEM Poder/Orgao (PO) => rejeita ANTES de emitir (fail-closed), nao gera CSV.
    public void Rejeita_matriz_com_linha_sem_poder_orgao()
    {
        // Matriz balanceada (D=C global e por classe), mas a linha credora NAO carrega o PO obrigatorio.
        var declaracao = Declaracao(
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(1000m)));

        var gerar = () => GerarCsv(declaracao);

        gerar.Should().Throw<MscLinhaSemPoderOrgaoCsvException>()
            .Which.ContaPcasp.Should().Be("2.1.1.1.01.00");
    }

    [Fact] // P0-1: PO presente mas invalido (nao 5 digitos) => tambem rejeita (IC nº1 exige 5 digitos).
    public void Rejeita_matriz_com_poder_orgao_invalido()
    {
        var declaracao = Declaracao(
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m), "PO=123"),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(1000m), "PO=123"));

        var gerar = () => GerarCsv(declaracao);

        gerar.Should().Throw<MscLinhaSemPoderOrgaoCsvException>();
    }

    [Fact] // P0-1: classe contabil desbalanceada (D!=C dentro da classe) => rejeita, mesmo com o total geral fechando.
    public void Rejeita_matriz_com_classe_desbalanceada()
    {
        // Global fecha (1000 D = 1000 C), MAS a classe Patrimonial (1-4) tem so 1000 D (0 C) e a classe
        // Orcamentaria (5-6) so 1000 C (0 D) — uma "compensa" a outra no total geral. O balanco POR CLASSE
        // (nao so o total) deve pegar isso. Qualquer das duas classes desbalanceadas e um catch valido.
        var declaracao = Declaracao(
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"),
            LinhaContabil.Criar("5.2.1.0.00.00", NaturezaSaldo.Credor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"));

        var gerar = () => GerarCsv(declaracao);

        gerar.Should().Throw<MscClasseDesbalanceadaCsvException>()
            .Which.Classe.Should().BeOneOf("Patrimonial", "Orcamentaria");
    }

    [Fact] // P0-2: o Poder/Orgao (PO) sai SEMPRE no par fixo TIPO1/IC1, mesmo que venha por ULTIMO no texto livre.
    public void Poder_orgao_sai_sempre_no_slot_fixo_tipo1_ic1()
    {
        // O texto livre poe a Fonte de Recurso (FR) ANTES do PO; o leiaute exige PO em TIPO1/IC1 — o gerador
        // mapeia por CODIGO, nunca pela ordem do texto.
        var declaracao = Declaracao(
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m), $"FR=1500;PO={PoderOrgaoValido}"),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"));

        var csv = GerarCsv(declaracao);
        var linhas = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // Cabecalho: Periodo|Cod.Siconfi|NaturezaInformacao|Conta|Tipo_Valor|Valor|TIPO1|IC1|...
        var cabecalho = linhas[0].Split('|');
        cabecalho[6].Should().Be("TIPO1");
        cabecalho[7].Should().Be("IC1");

        // Linha de dados da conta devedora: TIPO1 = "PO" e IC1 = o codigo do PO, mesmo o texto trazendo FR antes.
        var devedora = linhas.First(l => l.StartsWith("2026-06|", StringComparison.Ordinal) && l.Contains("1.1.1.1.01.00", StringComparison.Ordinal));
        var colunas = devedora.Split('|');
        colunas[6].Should().Be("PO", "o PO e sempre o primeiro par de informacao complementar (TIPO1)");
        colunas[7].Should().Be(PoderOrgaoValido);
        // A FR aparece num slot POSTERIOR (TIPO2/IC2), nao no slot do PO.
        colunas[8].Should().Be("FR");
        colunas[9].Should().Be("1500");
    }

    [Fact] // Caminho feliz: matriz com PO em todas as linhas e classes balanceadas => emite o CSV normalmente.
    public void Emite_csv_quando_matriz_valida()
    {
        var declaracao = Declaracao(
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(1000m), $"PO={PoderOrgaoValido}"));

        var csv = GerarCsv(declaracao);

        csv.Should().Contain("saldo_final");
        csv.Should().Contain(PoderOrgaoValido);
    }
}
