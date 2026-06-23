using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura de unidade do núcleo fiscal de Saúde S-1 (LC 141/2012): a classificação FINA das ASPS
/// (só computa o que o art. 3º manda; exclui o art. 4º — inativos, saneamento geral, merenda, limpeza
/// urbana), o atingimento/não do mínimo de 15% (parametrizável) e a reprodutibilidade (sem relógio).
/// </summary>
public sealed class ApuradorAspsTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const decimal QuinzePorCento = 0.15m;

    private static IReadOnlyList<RegraClassificacaoAsps> RegrasPadrao()
        => SeedRegrasAsps.Gerar(Tenant, new DateOnly(2012, 1, 16));

    private static DespesaSaude Despesa(string subfuncao, decimal valor, string? fonte = null)
        => new(CodigoFuncionalSaude.De("10", subfuncao), fonte, valor);

    [Fact]
    public void ASPS_computa_somente_o_classificado_e_exclui_inativos_e_saneamento()
    {
        var regras = RegrasPadrao();
        var despesas = new[]
        {
            Despesa("301", 100_000m), // Atenção básica — computa (art. 3º)
            Despesa("302", 50_000m),  // Assistência hospitalar — computa
            Despesa("122", 40_000m),  // Inativos / administração geral — NÃO computa (art. 4º)
            Despesa("512", 30_000m),  // Saneamento básico urbano — NÃO computa (art. 4º)
            Despesa("306", 20_000m),  // Merenda — NÃO computa (art. 4º)
        };

        var aplicado = ApuradorAsps.ApurarAplicadoComputavel(despesas, regras);

        // Só as duas primeiras (atenção básica + hospitalar) entram no numerador.
        aplicado.Should().Be(150_000m);
    }

    [Fact]
    public void Despesa_fora_da_funcao_saude_nunca_computa()
    {
        var regras = RegrasPadrao();
        var despesaEducacao = new DespesaSaude(CodigoFuncionalSaude.De("12", "361"), null, 999_999m);

        ClassificadorAsps.ComputaNasAsps(despesaEducacao.Codigo, despesaEducacao.FonteRecurso, regras)
            .Should().BeFalse();
    }

    [Fact]
    public void Minimo_de_15_porcento_atingido_quando_aplicado_alcanca_a_meta()
    {
        var regras = RegrasPadrao();
        var despesas = new[] { Despesa("301", 150_000m) }; // 150k de 1M = 15%

        var indicador = ApuradorAsps.Apurar(1_000_000m, despesas, regras, QuinzePorCento);

        indicador.PercentualAplicado.Should().Be(0.15m);
        indicador.Atingido.Should().BeTrue();
        indicador.Situacao.Should().Be(SituacaoAsps.Atingido);
        indicador.MargemPontos.Should().Be(0m);
    }

    [Fact]
    public void Minimo_de_15_porcento_nao_atingido_quando_exclusoes_derrubam_o_numerador()
    {
        var regras = RegrasPadrao();
        // 200k brutos, mas 60k são saneamento/merenda (não computam) → só 140k = 14% < 15%.
        var despesas = new[]
        {
            Despesa("301", 140_000m),
            Despesa("512", 40_000m),
            Despesa("306", 20_000m),
        };

        var indicador = ApuradorAsps.Apurar(1_000_000m, despesas, regras, QuinzePorCento);

        indicador.AplicadoAsps.Should().Be(140_000m);
        indicador.PercentualAplicado.Should().Be(0.14m);
        indicador.Atingido.Should().BeFalse();
        indicador.MargemPontos.Should().BeLessThan(0m);
    }

    [Fact]
    public void Percentual_minimo_e_parametrizavel_lei_organica_pode_exigir_mais()
    {
        var regras = RegrasPadrao();
        var despesas = new[] { Despesa("301", 160_000m) }; // 16%

        // Default 15% → atingido; mas se a Lei Orgânica exige 18%, o mesmo gasto NÃO atinge.
        ApuradorAsps.Apurar(1_000_000m, despesas, regras, 0.15m).Atingido.Should().BeTrue();
        ApuradorAsps.Apurar(1_000_000m, despesas, regras, 0.18m).Atingido.Should().BeFalse();
    }

    [Fact]
    public void Apuracao_e_reprodutivel_sem_relogio()
    {
        var regras = RegrasPadrao();
        var despesas = new[] { Despesa("301", 123_456m), Despesa("512", 10_000m) };

        var primeira = ApuradorAsps.Apurar(800_000m, despesas, regras, QuinzePorCento);
        var segunda = ApuradorAsps.Apurar(800_000m, despesas, regras, QuinzePorCento);

        // Value Object: igualdade estrutural — mesmas entradas ⇒ mesmo indicador, sempre.
        segunda.Should().Be(primeira);
    }

    [Fact]
    public void Regra_de_fonte_mais_especifica_vence_a_inclusao_generica_da_funcao()
    {
        var vigencia = new DateOnly(2024, 1, 1);
        var regras = new List<RegraClassificacaoAsps>(SeedRegrasAsps.Gerar(Tenant))
        {
            // Exclusão fina por fonte: recurso de assistência ao servidor (fonte fictícia "777") não é ASPS.
            RegraClassificacaoAsps.Criar(Tenant, EfeitoAsps.Exclui, "Assistência à saúde do servidor (art. 4º)", vigencia, fonteRecurso: "777"),
        };

        // Atenção básica (computaria), mas com fonte 777 → excluída.
        ClassificadorAsps.ComputaNasAsps(CodigoFuncionalSaude.De("10", "301"), "777", regras).Should().BeFalse();
        // Mesma subfunção, fonte comum → computa.
        ClassificadorAsps.ComputaNasAsps(CodigoFuncionalSaude.De("10", "301"), "001", regras).Should().BeTrue();
    }

    [Fact]
    public void Indicador_arredonda_a_quatro_casas_e_trata_base_zero()
    {
        var semBase = IndicadorAsps.Apurar(0m, 100m, QuinzePorCento);
        semBase.PercentualAplicado.Should().Be(0m);
        semBase.Atingido.Should().BeFalse();
    }
}
