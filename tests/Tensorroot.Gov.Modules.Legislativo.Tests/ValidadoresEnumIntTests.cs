using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Application.Comissoes;
using Tensorroot.Gov.Modules.Legislativo.Application.DiarioOficial;
using Tensorroot.Gov.Modules.Legislativo.Application.Normas;
using Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Application.Tribuna;
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

    // --- Gaps: Enum.IsDefined(typeof(E), valor) sobre int do contrato HTTP (nao IsInEnum). ---

    [Theory]
    [InlineData(1, true)]   // Lei
    [InlineData(6, true)]   // LeiOrganica
    [InlineData(0, false)]
    [InlineData(99, false)]
    public void CadastrarNorma_valida_tipo(int tipo, bool esperado)
    {
        var resultado = new CadastrarNormaValidator()
            .Validate(new CadastrarNormaCommand(tipo, 10, 2025, "Ementa.", new DateOnly(2025, 1, 1), null, null));

        resultado.IsValid.Should().Be(esperado);
    }

    [Theory]
    [InlineData(1, true)]   // Norma
    [InlineData(6, true)]   // Outro
    [InlineData(0, false)]
    public void AdicionarMateria_valida_tipo(int tipo, bool esperado)
    {
        var resultado = new AdicionarMateriaValidator()
            .Validate(new AdicionarMateriaCommand(Guid.NewGuid(), tipo, "Titulo", "conteudo", null));

        resultado.IsValid.Should().Be(esperado);
    }

    [Theory]
    [InlineData(1, true)]   // PequenoExpediente
    [InlineData(4, true)]   // TribunaLivre
    [InlineData(0, false)]
    public void InscreverOrador_valida_fase(int fase, bool esperado)
    {
        var resultado = new InscreverOradorValidator()
            .Validate(new InscreverOradorCommand(Guid.NewGuid(), Guid.NewGuid(), fase, null));

        resultado.IsValid.Should().Be(esperado);
    }

    [Theory]
    [InlineData(1, 0, true)]    // Efetivo / Nenhum
    [InlineData(2, 1, true)]    // Suplente / Presidente
    [InlineData(9, 0, false)]
    [InlineData(1, 9, false)]
    public void DesignarMembro_valida_papel_e_cargo(int papel, int cargo, bool esperado)
    {
        var resultado = new DesignarMembroValidator()
            .Validate(new DesignarMembroCommand(Guid.NewGuid(), Guid.NewGuid(), papel, cargo));

        resultado.IsValid.Should().Be(esperado);
    }

    [Theory]
    [InlineData(1, true)]   // Permanente
    [InlineData(2, true)]   // Temporaria
    [InlineData(0, false)]
    public void CriarComissao_valida_tipo(int tipo, bool esperado)
    {
        var resultado = new CriarComissaoValidator()
            .Validate(new CriarComissaoCommand("Comissao X", tipo));

        resultado.IsValid.Should().Be(esperado);
    }
}
