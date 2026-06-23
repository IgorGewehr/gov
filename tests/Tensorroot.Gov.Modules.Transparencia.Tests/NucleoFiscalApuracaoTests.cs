using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Testes de unidade do núcleo fiscal M7.0 (domínio puro, sem banco): classificação por função/fonte,
/// apuração Saúde 15% / Educação 25% (atingido e não-atingido) e reprodutibilidade.
/// </summary>
public sealed class NucleoFiscalApuracaoTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Vigencia = new(2026, 1, 1);

    [Theory]
    [InlineData("10", SetorMinimo.Saude)]
    [InlineData("10.301.0002.2010", SetorMinimo.Saude)]
    [InlineData("12", SetorMinimo.Educacao)]
    [InlineData("12.361.0003.2020", SetorMinimo.Educacao)]
    public void Classifica_despesa_por_funcao(string funcional, SetorMinimo setorEsperado)
    {
        var regras = new[]
        {
            FonteRecursoVinculado.Criar(Tenant, "10", SetorMinimo.Saude, Vigencia),
            FonteRecursoVinculado.Criar(Tenant, "12", SetorMinimo.Educacao, Vigencia),
        };

        var classificacao = ClassificadorFonteRecurso.Classificar(
            CodigoFuncional.DeTexto(funcional), fonteRecurso: null, regras);

        classificacao.Setor.Should().Be(setorEsperado);
        classificacao.ComputaNoMinimo.Should().BeTrue();
    }

    [Fact]
    public void Regra_mais_especifica_por_fonte_vence_a_geral_da_funcao()
    {
        var regras = new[]
        {
            // Geral: função 10 computa em Saúde.
            FonteRecursoVinculado.Criar(Tenant, "10", SetorMinimo.Saude, Vigencia),
            // Específica: função 10 + fonte 9999 (ex.: saneamento, LC 141 art. 4º) NÃO computa.
            FonteRecursoVinculado.Criar(Tenant, "10", SetorMinimo.Saude, Vigencia, fonteRecurso: "9999", computaNoMinimo: false),
        };

        var computavel = ClassificadorFonteRecurso.Classificar(CodigoFuncional.De("10"), "0500", regras);
        var naoComputavel = ClassificadorFonteRecurso.Classificar(CodigoFuncional.De("10"), "9999", regras);

        computavel.ComputaNoMinimo.Should().BeTrue();
        naoComputavel.Setor.Should().Be(SetorMinimo.Saude);
        naoComputavel.ComputaNoMinimo.Should().BeFalse();
    }

    [Fact]
    public void Despesa_de_funcao_sem_regra_nao_e_vinculada()
    {
        var regras = new[] { FonteRecursoVinculado.Criar(Tenant, "10", SetorMinimo.Saude, Vigencia) };

        // Função 04 (Administração) não tem regra de mínimo.
        var classificacao = ClassificadorFonteRecurso.Classificar(CodigoFuncional.De("04"), null, regras);

        classificacao.Setor.Should().Be(SetorMinimo.Nenhum);
        classificacao.ComputaNoMinimo.Should().BeFalse();
    }

    [Fact]
    public void Saude_15_porcento_atingido()
    {
        // Receita-base 1.000.000; aplicado 150.000 = 15% exatos.
        var indicador = ApuradorMinimo.Apurar(SetorMinimo.Saude, receitaBase: 1_000_000m, aplicadoComputavel: 150_000m, percentualMinimo: 0.15m);

        indicador.PercentualAplicado.Should().Be(0.15m);
        indicador.PercentualMinimo.Should().Be(0.15m);
        indicador.Situacao.Should().Be(SituacaoMinimo.Atingido);
        indicador.Atingido.Should().BeTrue();
    }

    [Fact]
    public void Saude_15_porcento_nao_atingido()
    {
        // Aplicado 140.000 sobre 1.000.000 = 14% < 15%.
        var indicador = ApuradorMinimo.Apurar(SetorMinimo.Saude, 1_000_000m, 140_000m, 0.15m);

        indicador.PercentualAplicado.Should().Be(0.14m);
        indicador.Situacao.Should().Be(SituacaoMinimo.NaoAtingido);
        indicador.MargemPontos.Should().BeLessThan(0m);
    }

    [Fact]
    public void Educacao_25_porcento_atingido_e_nao_atingido()
    {
        var atingido = ApuradorMinimo.Apurar(SetorMinimo.Educacao, 1_000_000m, 260_000m, 0.25m);
        var naoAtingido = ApuradorMinimo.Apurar(SetorMinimo.Educacao, 1_000_000m, 240_000m, 0.25m);

        atingido.Situacao.Should().Be(SituacaoMinimo.Atingido);
        atingido.PercentualAplicado.Should().Be(0.26m);
        naoAtingido.Situacao.Should().Be(SituacaoMinimo.NaoAtingido);
        naoAtingido.PercentualAplicado.Should().Be(0.24m);
    }

    [Fact]
    public void Percentual_minimo_e_parametrizavel_acima_do_default_legal()
    {
        // Lei Orgânica do município pode exigir mais que o piso legal (ex.: 18% em saúde).
        var indicador = ApuradorMinimo.Apurar(SetorMinimo.Saude, 1_000_000m, 150_000m, percentualMinimo: 0.18m);

        indicador.PercentualAplicado.Should().Be(0.15m);
        indicador.Situacao.Should().Be(SituacaoMinimo.NaoAtingido);
    }

    [Fact]
    public void Receita_base_zero_nao_divide_por_zero()
    {
        var indicador = ApuradorMinimo.Apurar(SetorMinimo.Saude, receitaBase: 0m, aplicadoComputavel: 0m, percentualMinimo: 0.15m);

        indicador.PercentualAplicado.Should().Be(0m);
        indicador.Situacao.Should().Be(SituacaoMinimo.NaoAtingido);
    }

    [Fact]
    public void Apuracao_e_reproduzivel_sem_relogio()
    {
        // Mesma entrada → mesmo resultado, sempre (sem dependência de DateTime.Now nem estado).
        IndicadorMinimo Apurar() => ApuradorMinimo.Apurar(SetorMinimo.Saude, 1_234_567.89m, 185_185.18m, 0.15m);

        var primeira = Apurar();
        var segunda = Apurar();

        primeira.Should().Be(segunda);
        primeira.PercentualAplicado.Should().Be(segunda.PercentualAplicado);
    }

    [Fact]
    public void ApurarTodos_casa_setor_com_despesa_correspondente()
    {
        var despesas = new[]
        {
            new DespesaSetorialApurada(SetorMinimo.Saude, 150_000m),
            new DespesaSetorialApurada(SetorMinimo.Educacao, 240_000m),
        };
        var parametros = new[]
        {
            new ParametroMinimo(SetorMinimo.Saude, 0.15m),
            new ParametroMinimo(SetorMinimo.Educacao, 0.25m),
        };

        var indicadores = ApuradorMinimo.ApurarTodos(1_000_000m, despesas, parametros);

        indicadores.Should().HaveCount(2);
        indicadores[0].Setor.Should().Be(SetorMinimo.Saude);
        indicadores[0].Situacao.Should().Be(SituacaoMinimo.Atingido);
        indicadores[1].Setor.Should().Be(SetorMinimo.Educacao);
        indicadores[1].Situacao.Should().Be(SituacaoMinimo.NaoAtingido);
    }
}
