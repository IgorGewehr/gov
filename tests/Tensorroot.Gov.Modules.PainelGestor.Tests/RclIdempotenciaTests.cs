using FluentAssertions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;
using Xunit;

namespace Tensorroot.Gov.Modules.PainelGestor.Tests;

/// <summary>
/// Bug hunt: idempotência da RCL (denominador dos limites LRF). Reprocessar o MESMO mês de referência
/// — mesmo com dois eventos de integração distintos e valores diferentes — não pode alterar a RCL já
/// fixada; só um mês estritamente mais recente sobrescreve.
/// </summary>
public sealed class RclIdempotenciaTests
{
    private static readonly Guid Tenant = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public void Rcl_do_mesmo_mes_nao_e_sobrescrita_por_valor_diferente()
    {
        var snapshot = IndicadorMunicipioSnapshot.Criar(Tenant, 2026);

        snapshot.DefinirReceitaCorrenteLiquida(100m, 5);
        snapshot.DefinirReceitaCorrenteLiquida(120m, 5); // mesmo mês, valor diferente → ignorado

        snapshot.ReceitaCorrenteLiquida.Should().Be(100m);
        snapshot.RclMesReferencia.Should().Be(5);

        snapshot.DefinirReceitaCorrenteLiquida(130m, 6); // mês mais recente → aceito
        snapshot.ReceitaCorrenteLiquida.Should().Be(130m);
        snapshot.RclMesReferencia.Should().Be(6);
    }
}
