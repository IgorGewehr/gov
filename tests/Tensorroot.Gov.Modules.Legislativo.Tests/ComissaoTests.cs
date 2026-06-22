using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura do agregado <see cref="Comissao"/>: composicao (efetivos/suplentes), unicidade de
/// presidencia, idempotencia por vereador, extincao e isolamento por tenant (C-1..C-5).
/// </summary>
public sealed class ComissaoTests : LegislativoTestBase
{
    private static Comissao NovaComissao()
        => Comissao.Criar(TenantA, "Constituicao, Justica e Redacao", TipoComissao.Permanente);

    [Fact] // C-1: nasce ativa.
    public void Criar_nasce_ativa()
    {
        var comissao = NovaComissao();

        comissao.Situacao.Should().Be(SituacaoComissao.Ativa);
        comissao.Membros.Should().BeEmpty();
    }

    [Fact] // C-2: designa membros (efetivo/suplente) e presidencia.
    public void DesignarMembro_compoe_efetivos_suplentes_e_presidencia()
    {
        var comissao = NovaComissao();
        var presidente = VereadorId.New();

        comissao.DesignarMembro(presidente, PapelMembro.Efetivo, CargoComissao.Presidente);
        comissao.DesignarMembro(VereadorId.New(), PapelMembro.Efetivo, CargoComissao.Nenhum);
        comissao.DesignarMembro(VereadorId.New(), PapelMembro.Suplente, CargoComissao.Nenhum);

        comissao.Membros.Should().HaveCount(3);
        comissao.Presidente!.VereadorId.Should().Be(presidente);
    }

    [Fact] // C-3: nao admite dois presidentes.
    public void DesignarMembro_segundo_presidente_lanca()
    {
        var comissao = NovaComissao();
        comissao.DesignarMembro(VereadorId.New(), PapelMembro.Efetivo, CargoComissao.Presidente);

        ((Action)(() => comissao.DesignarMembro(VereadorId.New(), PapelMembro.Efetivo, CargoComissao.Presidente)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // C-2: idempotente por vereador — segunda designacao atualiza o cargo.
    public void DesignarMembro_idempotente_por_vereador_atualiza_cargo()
    {
        var comissao = NovaComissao();
        var vereador = VereadorId.New();

        comissao.DesignarMembro(vereador, PapelMembro.Efetivo, CargoComissao.Nenhum);
        comissao.DesignarMembro(vereador, PapelMembro.Efetivo, CargoComissao.Relator);

        comissao.Membros.Should().ContainSingle()
            .Which.Cargo.Should().Be(CargoComissao.Relator);
    }

    [Fact] // C-4: remove membro da composicao.
    public void RemoverMembro_retira_da_composicao()
    {
        var comissao = NovaComissao();
        var vereador = VereadorId.New();
        comissao.DesignarMembro(vereador, PapelMembro.Efetivo, CargoComissao.Nenhum);

        comissao.RemoverMembro(vereador);

        comissao.Membros.Should().BeEmpty();
    }

    [Fact] // C-5: extinta nao admite composicao.
    public void Extinguir_e_terminal()
    {
        var comissao = NovaComissao();
        comissao.Extinguir();

        comissao.Terminal.Should().BeTrue();
        ((Action)(() => comissao.DesignarMembro(VereadorId.New(), PapelMembro.Efetivo, CargoComissao.Nenhum)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // Persistencia da composicao + isolamento por tenant.
    public async Task Persiste_composicao_com_isolamento()
    {
        ComissaoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var comissao = NovaComissao();
            id = comissao.Id;
            comissao.DesignarMembro(VereadorId.New(), PapelMembro.Efetivo, CargoComissao.Presidente);
            contexto.Comissoes.Add(comissao);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var comissao = await contexto.Comissoes.SingleAsync(c => c.Id == id);
            comissao.Membros.Should().ContainSingle();
            comissao.Presidente.Should().NotBeNull();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Comissoes.ToListAsync()).Should().BeEmpty();
        }
    }
}
