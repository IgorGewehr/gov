using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// W10.6 (Patrimônio) — ciclo patrimonial do <see cref="Veiculo"/>: reavaliação/impairment
/// prospectivos e saída do acervo por baixa/alienação. Antes o veículo sinistrado/leiloado não saía
/// do acervo e continuava depreciando indefinidamente no balanço.
/// </summary>
public sealed class VeiculoCicloPatrimonialTests : PatrimonioTestBase
{
    private static Veiculo VeiculoTombado()
    {
        var veiculo = Veiculo.IncorporarVeiculo(
            TenantA,
            "Caminhão basculante",
            ValorMonetario.De(200_000m),
            ValorMonetario.De(20_000m),
            120,
            new DateOnly(2026, 1, 10),
            "Aquisicao",
            Placa.Criar("ABC1D23"),
            Renavam.Criar("12345678900"),
            Odometro.De(50_000),
            Horimetro.De(1_000m));
        veiculo.Tombar("TOMBO-VEIC-1");
        return veiculo;
    }

    [Fact] // Reavaliação altera o contábil prospectivamente (somente ativo no acervo) e emite evento.
    public void Reavaliacao_ajusta_valor_contabil()
    {
        var veiculo = VeiculoTombado();

        veiculo.Reavaliar(180_000m, "laudo://reav-1");

        veiculo.ValorContabil.Valor.Should().Be(180_000m);
    }

    [Fact] // Impairment exige recuperável < contábil; igual/maior é rejeitado (não infla o ativo).
    public void Impairment_exige_recuperavel_inferior_ao_contabil()
    {
        var veiculo = VeiculoTombado();

        veiculo.RegistrarImpairment(150_000m, "laudo://imp-1");
        veiculo.ValorContabil.Valor.Should().Be(150_000m);

        var acaoMaior = () => veiculo.RegistrarImpairment(160_000m, "laudo://imp-2");
        acaoMaior.Should().Throw<InvalidOperationException>().WithMessage("*inferior ao valor contábil*");
    }

    [Fact] // Baixa exige laudo + autorização; sem autorização nenhuma saída contábil é emitida.
    public void Baixa_exige_autorizacao()
    {
        var veiculo = VeiculoTombado();

        var acao = () => veiculo.Baixar("Sinistro com perda total", "laudo://baixa-1", Guid.Empty);

        acao.Should().Throw<ArgumentOutOfRangeException>();
        veiculo.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado, "baixa rejeitada não altera a situação");
    }

    [Fact] // Veículo baixado sai do acervo e CESSA a depreciação (não permanece depreciando no balanço).
    public void Veiculo_baixado_cessa_depreciacao()
    {
        var veiculo = VeiculoTombado();

        veiculo.Baixar("Sinistro com perda total", "laudo://baixa-1", Guid.NewGuid());

        veiculo.Situacao.Should().Be(SituacaoBemPatrimonial.Baixada);
        veiculo.AtivoNoAcervo.Should().BeFalse();
        var acaoDepreciar = () => veiculo.Depreciar(new DateOnly(2026, 7, 1));
        acaoDepreciar.Should().Throw<InvalidOperationException>("veículo fora do acervo não deprecia");
    }

    [Fact] // Alienação por leilão (Lei 14.133) encerra o veículo; exige avaliação prévia.
    public void Alienacao_exige_avaliacao_previa_e_encerra()
    {
        var veiculo = VeiculoTombado();

        var semAvaliacao = () => veiculo.Alienar(Guid.Empty, porLeilao: true, valorAlienacao: 150_000m);
        semAvaliacao.Should().Throw<ArgumentOutOfRangeException>();

        veiculo.Alienar(Guid.NewGuid(), porLeilao: true, valorAlienacao: 150_000m);
        veiculo.Situacao.Should().Be(SituacaoBemPatrimonial.Alienada);
    }

    [Fact] // Veículo encerrado (baixado/alienado) não admite nova transição terminal nem reavaliação.
    public void Veiculo_encerrado_nao_admite_nova_transicao()
    {
        var veiculo = VeiculoTombado();
        veiculo.Baixar("Obsolescencia", "laudo://baixa-2", Guid.NewGuid());

        var rebaixar = () => veiculo.Baixar("De novo", "laudo://x", Guid.NewGuid());
        rebaixar.Should().Throw<InvalidOperationException>().WithMessage("*encerrado*");

        var reavaliar = () => veiculo.Reavaliar(100_000m, "laudo://y");
        reavaliar.Should().Throw<InvalidOperationException>("reavaliação exige veículo ativo no acervo");
    }
}
