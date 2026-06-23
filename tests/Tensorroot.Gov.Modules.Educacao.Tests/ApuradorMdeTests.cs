using FluentAssertions;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Cobertura de unidade do núcleo fiscal de Educação E-1 (CF art. 212; LDB arts. 70/71): a classificação
/// FINA da MDE (só computa o que o art. 70 manda; exclui o art. 71 — merenda, assistência médica ao aluno,
/// obras urbanas fora das escolas, inativos), o atingimento/não do mínimo de 25% (parametrizável), a
/// separação aferição-anual × indicador-bimestral e a reprodutibilidade (sem relógio).
/// </summary>
public sealed class ApuradorMdeTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const decimal VinteCincoPorCento = 0.25m;

    private static IReadOnlyList<RegraClassificacaoMde> RegrasPadrao()
        => SeedRegrasMde.Gerar(Tenant, new DateOnly(1996, 12, 20));

    private static DespesaEducacao Despesa(string subfuncao, decimal valor, string? fonte = null)
        => new(CodigoFuncionalEducacao.De("12", subfuncao), fonte, valor);

    [Fact]
    public void MDE_computa_somente_o_classificado_e_exclui_merenda_e_assistencia_ao_aluno()
    {
        var regras = RegrasPadrao();
        var despesas = new[]
        {
            Despesa("361", 200_000m), // Ensino fundamental — computa (art. 70)
            Despesa("365", 100_000m), // Educação infantil — computa
            Despesa("306", 50_000m),  // Merenda escolar — NÃO computa (art. 71, IV)
            Despesa("301", 40_000m),  // Assistência médica ao aluno — NÃO computa (art. 71, IV)
            Despesa("122", 30_000m),  // Inativos / administração geral — NÃO computa (art. 71)
        };

        var aplicado = ApuradorMde.ApurarAplicadoComputavel(despesas, regras);

        // Só ensino fundamental + educação infantil entram no numerador.
        aplicado.Should().Be(300_000m);
    }

    [Fact]
    public void Despesa_fora_da_funcao_educacao_nunca_computa()
    {
        var regras = RegrasPadrao();
        var despesaSaude = new DespesaEducacao(CodigoFuncionalEducacao.De("10", "301"), null, 999_999m);

        ClassificadorMde.ComputaNaMde(despesaSaude.Codigo, despesaSaude.FonteRecurso, regras)
            .Should().BeFalse();
    }

    [Fact]
    public void Minimo_de_25_porcento_atingido_quando_aplicado_alcanca_a_meta()
    {
        var regras = RegrasPadrao();
        var despesas = new[] { Despesa("361", 250_000m) }; // 250k de 1M = 25%

        var indicador = ApuradorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 1_000_000m, despesas, regras, VinteCincoPorCento);

        indicador.PercentualAplicado.Should().Be(0.25m);
        indicador.Atingido.Should().BeTrue();
        indicador.Situacao.Should().Be(SituacaoMde.Atingido);
        indicador.EhConformidade.Should().BeTrue();
        indicador.MargemPontos.Should().Be(0m);
    }

    [Fact]
    public void Minimo_de_25_porcento_nao_atingido_quando_exclusoes_derrubam_o_numerador()
    {
        var regras = RegrasPadrao();
        // 300k brutos, mas 70k são merenda/assistência (não computam) → só 230k = 23% < 25%.
        var despesas = new[]
        {
            Despesa("361", 230_000m),
            Despesa("306", 50_000m),
            Despesa("301", 20_000m),
        };

        var indicador = ApuradorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 1_000_000m, despesas, regras, VinteCincoPorCento);

        indicador.AplicadoMde.Should().Be(230_000m);
        indicador.PercentualAplicado.Should().Be(0.23m);
        indicador.Atingido.Should().BeFalse();
        indicador.MargemPontos.Should().BeLessThan(0m);
    }

    [Fact]
    public void Percentual_minimo_e_parametrizavel_lei_organica_pode_exigir_mais()
    {
        var regras = RegrasPadrao();
        var despesas = new[] { Despesa("361", 260_000m) }; // 26%

        ApuradorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 1_000_000m, despesas, regras, 0.25m).Atingido.Should().BeTrue();
        ApuradorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 1_000_000m, despesas, regras, 0.28m).Atingido.Should().BeFalse();
    }

    [Fact]
    public void Indicador_bimestral_nao_e_conformidade_mesmo_quando_abaixo_do_minimo()
    {
        var regras = RegrasPadrao();
        // No 1º bimestre o aplicado é baixo (5%): NÃO é conformidade — só acompanhamento (RISCO #3).
        var despesas = new[] { Despesa("361", 50_000m) };

        var bimestral = ApuradorMde.Apurar(NaturezaAferimentoMde.IndicadorBimestral, 1_000_000m, despesas, regras, VinteCincoPorCento);

        bimestral.EhConformidade.Should().BeFalse();
        bimestral.Natureza.Should().Be(NaturezaAferimentoMde.IndicadorBimestral);
        bimestral.PercentualAplicado.Should().Be(0.05m);
    }

    [Fact]
    public void Apuracao_e_reprodutivel_sem_relogio()
    {
        var regras = RegrasPadrao();
        var despesas = new[] { Despesa("361", 123_456m), Despesa("306", 10_000m) };

        var primeira = ApuradorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 800_000m, despesas, regras, VinteCincoPorCento);
        var segunda = ApuradorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 800_000m, despesas, regras, VinteCincoPorCento);

        // Value Object: igualdade estrutural — mesmas entradas ⇒ mesmo indicador, sempre.
        segunda.Should().Be(primeira);
    }

    [Fact]
    public void Regra_de_fonte_mais_especifica_vence_a_inclusao_generica_da_funcao()
    {
        var vigencia = new DateOnly(2024, 1, 1);
        var regras = new List<RegraClassificacaoMde>(SeedRegrasMde.Gerar(Tenant))
        {
            // Exclusão fina por fonte: recurso de programa suplementar (fonte fictícia "555") não é MDE.
            RegraClassificacaoMde.Criar(Tenant, EfeitoMde.Exclui, "Programa suplementar — não MDE (art. 71)", vigencia, fonteRecurso: "555"),
        };

        // Ensino fundamental (computaria), mas com fonte 555 → excluído.
        ClassificadorMde.ComputaNaMde(CodigoFuncionalEducacao.De("12", "361"), "555", regras).Should().BeFalse();
        // Mesma subfunção, fonte comum → computa.
        ClassificadorMde.ComputaNaMde(CodigoFuncionalEducacao.De("12", "361"), "001", regras).Should().BeTrue();
    }

    [Fact]
    public void Indicador_arredonda_a_quatro_casas_e_trata_base_zero()
    {
        var semBase = IndicadorMde.Apurar(NaturezaAferimentoMde.AferimentoAnual, 0m, 100m, VinteCincoPorCento);
        semBase.PercentualAplicado.Should().Be(0m);
        semBase.Atingido.Should().BeFalse();
    }
}
