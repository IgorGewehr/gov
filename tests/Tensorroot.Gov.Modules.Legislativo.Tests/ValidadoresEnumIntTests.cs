using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Regressao: os comandos do modulo trafegam enums como <c>int</c> no contrato HTTP. O validador
/// DEVE aceitar os valores definidos do enum e rejeitar os indefinidos. O bug original usava
/// <c>IsInEnum()</c> sobre uma propriedade <c>int</c>, que reprova TODOS os valores e travava o
/// fluxo de demonstracao por HTTP (registrar voto, iniciar votacao, agendar sessao, apresentar
/// proposicao) — embora o seed, que chama o dominio direto, funcionasse.
/// </summary>
public sealed class ValidadoresEnumIntTests
{
    [Theory]
    [InlineData(1)] // Sim
    [InlineData(2)] // Nao
    [InlineData(3)] // Abstencao
    public void RegistrarVoto_aceita_sentido_definido(int sentido)
    {
        var resultado = new RegistrarVotoValidator()
            .Validate(new RegistrarVotoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sentido));

        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    public void RegistrarVoto_rejeita_sentido_indefinido(int sentido)
    {
        var resultado = new RegistrarVotoValidator()
            .Validate(new RegistrarVotoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), sentido));

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IniciarVotacao_aceita_tipo_e_maioria_definidos()
    {
        var resultado = new IniciarVotacaoValidator()
            .Validate(new IniciarVotacaoCommand(Guid.NewGuid(), Guid.NewGuid(), Tipo: 2, MaioriaExigida: 1, TotalMembros: 9, Presentes: 8, Turno: 1));

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IniciarVotacao_rejeita_tipo_indefinido()
    {
        var resultado = new IniciarVotacaoValidator()
            .Validate(new IniciarVotacaoCommand(Guid.NewGuid(), Guid.NewGuid(), Tipo: 0, MaioriaExigida: 1, TotalMembros: 9, Presentes: 8, Turno: 1));

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AgendarSessao_aceita_tipo_definido()
    {
        var resultado = new AgendarSessaoValidator()
            .Validate(new AgendarSessaoCommand(Tipo: 1, DataHora: DateTimeOffset.UtcNow, TotalMembros: 9));

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AgendarSessao_rejeita_tipo_indefinido()
    {
        var resultado = new AgendarSessaoValidator()
            .Validate(new AgendarSessaoCommand(Tipo: 9, DataHora: DateTimeOffset.UtcNow, TotalMembros: 9));

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApresentarProposicao_aceita_tipo_e_regime_definidos()
    {
        var resultado = new ApresentarProposicaoValidator()
            .Validate(new ApresentarProposicaoCommand(Tipo: 1, Ementa: "Ementa de teste.", Autoria: "Vereador X", Regime: 1));

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApresentarProposicao_rejeita_tipo_indefinido()
    {
        var resultado = new ApresentarProposicaoValidator()
            .Validate(new ApresentarProposicaoCommand(Tipo: 0, Ementa: "Ementa de teste.", Autoria: "Vereador X", Regime: 1));

        resultado.IsValid.Should().BeFalse();
    }
}
