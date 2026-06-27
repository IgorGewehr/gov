using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do dominio do Processo Trabalhista (provisao contabil NBC TG 25): a provisao segue o
/// prognostico (provavel = valor estimado; possivel/remota = zero), reavaliacao so em andamento, desfecho
/// (acordo/condenacao/improcedencia) e' terminal e consolida o valor efetivo. Testes PUROS (sem banco).
/// </summary>
public sealed class ProcessoTrabalhistaDominioTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static ProcessoTrabalhista Cadastrar(PrognosticoPerda prognostico, decimal valorCausa = 50_000m)
        => ProcessoTrabalhista.Cadastrar(
            Tenant,
            numeroProcesso: "0001234-56.2026.5.04.0001",
            vara: "1a Vara do Trabalho",
            reclamante: "Fulano de Tal",
            servidorId: null,
            objeto: "Horas extras e adicional noturno",
            valorCausa: valorCausa,
            dataAjuizamento: new DateOnly(2026, 1, 10),
            prognostico: prognostico);

    [Fact]
    public void Cadastrar_comPerdaProvavel_provisiona_valorDaCausa_e_emite_evento()
    {
        var processo = Cadastrar(PrognosticoPerda.Provavel, valorCausa: 50_000m);

        processo.Situacao.Should().Be(SituacaoProcessoTrabalhista.EmAndamento);
        processo.ValorProvisionado.Should().Be(50_000m);
        processo.DomainEvents.OfType<ProcessoTrabalhistaCadastrado>().Should().ContainSingle();
    }

    [Theory]
    [InlineData(PrognosticoPerda.Possivel)]
    [InlineData(PrognosticoPerda.Remota)]
    public void Cadastrar_comPerdaNaoProvavel_nao_provisiona(PrognosticoPerda prognostico)
    {
        var processo = Cadastrar(prognostico, valorCausa: 50_000m);

        processo.ValorProvisionado.Should().Be(0m);
    }

    [Fact]
    public void ReavaliarPrognostico_de_provavel_para_remota_zera_a_provisao_e_emite_evento()
    {
        var processo = Cadastrar(PrognosticoPerda.Provavel, valorCausa: 50_000m);

        processo.ReavaliarPrognostico(PrognosticoPerda.Remota);

        processo.Prognostico.Should().Be(PrognosticoPerda.Remota);
        processo.ValorProvisionado.Should().Be(0m);
        processo.DomainEvents.OfType<ProvisaoProcessoAtualizada>().Should().ContainSingle();
    }

    [Fact]
    public void RegistrarAcordo_encerra_com_valor_efetivo_e_emite_evento()
    {
        var processo = Cadastrar(PrognosticoPerda.Provavel, valorCausa: 50_000m);

        processo.RegistrarAcordo(valorAcordo: 30_000m, dataAcordo: new DateOnly(2026, 6, 1));

        processo.Situacao.Should().Be(SituacaoProcessoTrabalhista.Acordo);
        processo.ValorProvisionado.Should().Be(30_000m);
        processo.DomainEvents.OfType<ProcessoTrabalhistaEncerrado>().Should().ContainSingle();
    }

    [Fact]
    public void RegistrarImprocedencia_encerra_com_provisao_zero()
    {
        var processo = Cadastrar(PrognosticoPerda.Provavel, valorCausa: 50_000m);

        processo.RegistrarImprocedencia(dataTransito: new DateOnly(2026, 6, 1));

        processo.Situacao.Should().Be(SituacaoProcessoTrabalhista.Improcedente);
        processo.ValorProvisionado.Should().Be(0m);
    }

    [Fact]
    public void Desfecho_e_terminal_novo_desfecho_em_processo_encerrado_falha()
    {
        var processo = Cadastrar(PrognosticoPerda.Provavel);
        processo.RegistrarAcordo(valorAcordo: 10_000m, dataAcordo: new DateOnly(2026, 6, 1));

        var acao = () => processo.RegistrarCondenacao(20_000m, new DateOnly(2026, 7, 1));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reavaliar_processo_encerrado_falha()
    {
        var processo = Cadastrar(PrognosticoPerda.Provavel);
        processo.RegistrarImprocedencia(new DateOnly(2026, 6, 1));

        var acao = () => processo.ReavaliarPrognostico(PrognosticoPerda.Possivel);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Arquivar_so_apos_encerrado()
    {
        var emAndamento = Cadastrar(PrognosticoPerda.Provavel);
        var arquivarEmAndamento = emAndamento.Arquivar;
        arquivarEmAndamento.Should().Throw<InvalidOperationException>();

        var encerrado = Cadastrar(PrognosticoPerda.Provavel);
        encerrado.RegistrarCondenacao(15_000m, new DateOnly(2026, 6, 1));
        encerrado.Arquivar();
        encerrado.Situacao.Should().Be(SituacaoProcessoTrabalhista.Arquivado);
    }
}
