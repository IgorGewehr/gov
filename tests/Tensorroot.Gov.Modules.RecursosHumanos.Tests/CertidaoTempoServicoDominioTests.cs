using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do dominio da Certidao de Tempo de Servico/Contribuicao (CTC): apuracao do efetivo exercicio
/// (abatimento de nao-computaveis), fator de conversao, vedacao de concomitancia (art. 96, II, Lei
/// 8.213/1991), numeracao/autenticacao e ciclo de vida. Testes PUROS (sem banco/relogio).
/// </summary>
public sealed class CertidaoTempoServicoDominioTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly ServidorId Servidor = ServidorId.New();

    // ---------- PeriodoTempo: contagem de dias ----------

    [Fact] // Intervalo fechado [inicio, fim] conta ambos os extremos.
    public void PeriodoTempo_conta_dias_brutos_inclusivos()
    {
        var periodo = PeriodoTempo.EfetivoExercicio(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 10));
        periodo.DiasBrutos.Should().Be(10);
        periodo.DiasLiquidos.Should().Be(10);
        periodo.DiasEquivalentes.Should().Be(10);
    }

    [Fact] // Dias nao-computaveis abatem dos liquidos.
    public void PeriodoTempo_abate_nao_computaveis()
    {
        var periodo = PeriodoTempo.EfetivoExercicio(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), diasNaoComputaveis: 5);
        periodo.DiasBrutos.Should().Be(31);
        periodo.DiasLiquidos.Should().Be(26);
    }

    [Fact] // Fator de conversao MAJORA o tempo comum equivalente (ex.: 1,40 para tempo especial 25 anos).
    public void PeriodoTempo_aplica_fator_de_conversao()
    {
        var periodo = PeriodoTempo.Averbado(
            new DateOnly(2010, 1, 1), new DateOnly(2010, 12, 31),
            RegimeOrigemPeriodo.Rgps, "INSS", fator: 1.40m);
        periodo.DiasLiquidos.Should().Be(365);
        periodo.DiasEquivalentes.Should().Be(511); // 365 * 1,40 = 511
    }

    [Fact] // Fator < 1,0 e invalido (conversao nunca REDUZ o tempo comum).
    public void PeriodoTempo_rejeita_fator_menor_que_um()
    {
        var acao = () => PeriodoTempo.EfetivoExercicio(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2), fator: 0.5m);
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // Fim anterior ao inicio e invalido.
    public void PeriodoTempo_rejeita_fim_antes_do_inicio()
    {
        var acao = () => PeriodoTempo.EfetivoExercicio(new DateOnly(2020, 1, 10), new DateOnly(2020, 1, 1));
        acao.Should().Throw<ArgumentException>();
    }

    // ---------- ApuradorTempoServidor ----------

    [Fact] // Sem afastamentos, o efetivo exercicio = intervalo cheio [exercicio, dataBase].
    public void Apurador_sem_nao_computaveis_conta_intervalo_cheio()
    {
        var periodo = ApuradorTempoServidor.ApurarEfetivoExercicio(
            new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), []);
        periodo.DiasLiquidos.Should().Be(366); // 2020 bissexto, inclusivo
    }

    [Fact] // Dois afastamentos SOBREPOSTOS contam o dia uma vez so (uniao, sem dupla contagem).
    public void Apurador_funde_nao_computaveis_sobrepostos()
    {
        var naoComputaveis = new[]
        {
            new IntervaloNaoComputavel(new DateOnly(2020, 3, 1), new DateOnly(2020, 3, 31)),
            new IntervaloNaoComputavel(new DateOnly(2020, 3, 15), new DateOnly(2020, 4, 15)),
        };
        // Uniao 01/03..15/04 = 31 (mar) + 15 (abr) = 46 dias.
        var dias = ApuradorTempoServidor.ContarDiasNaoComputaveis(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), naoComputaveis);
        dias.Should().Be(46);
    }

    [Fact] // Nao-computaveis fora do intervalo de apuracao sao recortados (so conta a interseccao).
    public void Apurador_recorta_nao_computaveis_aos_limites()
    {
        var naoComputaveis = new[]
        {
            new IntervaloNaoComputavel(new DateOnly(2019, 12, 1), new DateOnly(2020, 1, 10)),
        };
        // So 01/01..10/01 = 10 dias caem no intervalo de apuracao.
        var dias = ApuradorTempoServidor.ContarDiasNaoComputaveis(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), naoComputaveis);
        dias.Should().Be(10);
    }

    [Fact] // Data-base anterior ao exercicio e' erro de dominio.
    public void Apurador_rejeita_data_base_anterior_ao_exercicio()
    {
        var acao = () => ApuradorTempoServidor.ApurarEfetivoExercicio(
            new DateOnly(2020, 1, 1), new DateOnly(2019, 12, 31), []);
        acao.Should().Throw<CertidaoTempoServicoException>();
    }

    // ---------- CertidaoTempoServico: emissao e invariantes ----------

    private static CertidaoTempoServico Emitir(params PeriodoTempo[] periodos)
        => CertidaoTempoServico.Emitir(
            Tenant, Servidor, NumeroCertidao.De(2026, 1),
            FinalidadeCertidao.Aposentadoria, new DateOnly(2026, 6, 20),
            "Departamento de Recursos Humanos", periodos);

    [Fact] // I-1: certidao sem periodo nao tem efeito.
    public void Emitir_sem_periodo_falha()
    {
        var acao = () => Emitir();
        acao.Should().Throw<CertidaoTempoServicoException>();
    }

    [Fact] // I-2: periodos sobrepostos (contagem concomitante) sao vedados.
    public void Emitir_com_periodos_concomitantes_falha()
    {
        var p1 = PeriodoTempo.EfetivoExercicio(new DateOnly(2015, 1, 1), new DateOnly(2018, 12, 31));
        var p2 = PeriodoTempo.Averbado(new DateOnly(2018, 6, 1), new DateOnly(2019, 12, 31), RegimeOrigemPeriodo.Rgps, "INSS");
        var acao = () => Emitir(p1, p2);
        acao.Should().Throw<CertidaoTempoServicoException>().WithMessage("*concomitante*");
    }

    [Fact] // I-3: total = soma dos dias equivalentes; periodos contiguos (nao sobrepostos) sao validos.
    public void Emitir_soma_periodos_contiguos()
    {
        var p1 = PeriodoTempo.EfetivoExercicio(new DateOnly(2015, 1, 1), new DateOnly(2015, 12, 31)); // 365
        var p2 = PeriodoTempo.Averbado(new DateOnly(2016, 1, 1), new DateOnly(2016, 12, 31), RegimeOrigemPeriodo.Rgps, "INSS"); // 366 (bissexto)
        var certidao = Emitir(p1, p2);
        certidao.TotalDias.Should().Be(365 + 366);
        certidao.Periodos.Should().HaveCount(2);
    }

    [Fact] // Autenticacao: digest selado, evento emitido com o codigo, situacao Emitida.
    public void Emitir_e_autenticar_emite_evento_com_codigo()
    {
        var certidao = Emitir(PeriodoTempo.EfetivoExercicio(new DateOnly(2015, 1, 1), new DateOnly(2020, 12, 31)));
        certidao.DefinirAutenticacao(CodigoAutenticacao.De("ABCDEF0123456789AA"));

        certidao.Situacao.Should().Be(SituacaoCertidao.Emitida);
        certidao.CodigoAutenticacao.Valor.Should().Be("ABCDEF0123456789");
        certidao.DomainEvents.OfType<CertidaoTempoServicoEmitida>().Should().ContainSingle()
            .Which.CodigoAutenticacao.Should().Be("ABCDEF0123456789");
    }

    [Fact] // Autenticacao so pode ser selada uma vez.
    public void DefinirAutenticacao_duas_vezes_falha()
    {
        var certidao = Emitir(PeriodoTempo.EfetivoExercicio(new DateOnly(2015, 1, 1), new DateOnly(2020, 12, 31)));
        certidao.DefinirAutenticacao(CodigoAutenticacao.De("ABCDEF0123456789"));
        var acao = () => certidao.DefinirAutenticacao(CodigoAutenticacao.De("FEDCBA9876543210"));
        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4: anular preserva a numeracao; situacao vira Anulada e emite evento; reanular falha.
    public void Anular_e_terminal_e_preserva_numeracao()
    {
        var certidao = Emitir(PeriodoTempo.EfetivoExercicio(new DateOnly(2015, 1, 1), new DateOnly(2020, 12, 31)));
        certidao.DefinirAutenticacao(CodigoAutenticacao.De("ABCDEF0123456789"));
        var numeroAntes = certidao.Numero.Formatado;

        certidao.Anular("Erro material na origem");

        certidao.Situacao.Should().Be(SituacaoCertidao.Anulada);
        certidao.Numero.Formatado.Should().Be(numeroAntes);
        certidao.MotivoAnulacao.Should().Be("Erro material na origem");
        certidao.DomainEvents.OfType<CertidaoTempoServicoAnulada>().Should().ContainSingle();

        var reanular = () => certidao.Anular("De novo");
        reanular.Should().Throw<InvalidOperationException>();
    }

    [Fact] // TempoDecomposto: convencao civil 365/30.
    public void TempoDecomposto_usa_convencao_365_30()
    {
        var tempo = TempoDecomposto.De(400); // 1 ano (365) + 1 mes (30) + 5 dias
        tempo.Anos.Should().Be(1);
        tempo.Meses.Should().Be(1);
        tempo.Dias.Should().Be(5);
    }

    [Theory] // Validacao publica do balcao: codigo malformado = "nao confere", nunca erro (sem 500).
    [InlineData("ABCDEFGH")]         // curto demais (8 < 16)
    [InlineData("ZZZZZZZZZZZZZZZZ")] // 16 chars mas nao-hex
    [InlineData("")]                 // vazio
    [InlineData(null)]               // nulo
    public void CodigoAutenticacao_TryDe_nao_lanca_em_entrada_publica_invalida(string? entrada)
    {
        var ok = CodigoAutenticacao.TryDe(entrada, out var codigo);

        ok.Should().BeFalse();
        codigo.Should().BeNull();
    }

    [Fact]
    public void CodigoAutenticacao_TryDe_normaliza_codigo_valido()
    {
        var ok = CodigoAutenticacao.TryDe("abcdef0123456789aa", out var codigo);

        ok.Should().BeTrue();
        codigo!.Valor.Should().Be("ABCDEF0123456789", "trunca a 16 hex maiusculos");
    }
}
