using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Testes-chave do agregado <see cref="Inventario"/> (Lei 4.320 art. 96, Onda 1): comissao minima,
/// snapshot congelado, conciliacao fisico × contabil (Falta/Sobra/DivergenciaLocalizacao) e o
/// encerramento que exige conciliacao previa (emitindo <see cref="InventarioEncerrado"/>). Dominio puro.
/// </summary>
public sealed class InventarioDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Abertura = new(2026, 6, 1);

    private static IEnumerable<MembroComissao> ComissaoValida() =>
    [
        MembroComissao.Designar(Guid.NewGuid(), "Presidente", presidente: true),
        MembroComissao.Designar(Guid.NewGuid(), "Membro 2", presidente: false),
        MembroComissao.Designar(Guid.NewGuid(), "Membro 3", presidente: false),
    ];

    private static Inventario Abrir() =>
        Inventario.Abrir(Tenant, 2026, TipoInventario.Anual, setor: null, "Portaria 1/2026", ComissaoValida(), Abertura);

    private static SnapshotBem Bem(string tombo, string local, decimal valor)
        => new(BemPatrimonialId.New(), tombo, $"Bem {tombo}", local, ValorMonetario.De(valor));

    [Fact]
    public void Abrir_com_comissao_insuficiente_deve_rejeitar()
    {
        var acao = () => Inventario.Abrir(Tenant, 2026, TipoInventario.Anual, null, "Portaria 1/2026",
            [MembroComissao.Designar(Guid.NewGuid(), "Unico", true)], Abertura);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Abrir_valido_nasce_em_abertura_e_emite_evento()
    {
        var inv = Abrir();

        inv.Situacao.Should().Be(SituacaoInventario.EmAbertura);
        inv.Comissao.Should().HaveCount(3);
        inv.DomainEvents.Should().ContainSingle(e => e is InventarioAberto);
    }

    [Fact]
    public void Snapshot_so_congela_uma_vez_evita_drift()
    {
        var inv = Abrir();
        inv.CarregarSnapshotContabil([Bem("100", "Almoxarifado", 500m)]);

        inv.Situacao.Should().Be(SituacaoInventario.EmContagem);
        inv.Itens.Should().ContainSingle();

        var acao = () => inv.CarregarSnapshotContabil([Bem("200", "Sala", 300m)]);
        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Conciliar_apura_falta_localizacao_e_sobra()
    {
        var inv = Abrir();
        var bemLocalizado = Bem("100", "Almoxarifado", 500m);
        var bemForaDeLocal = Bem("200", "Sala 1", 300m);
        var bemAusente = Bem("300", "Patio", 800m);
        inv.CarregarSnapshotContabil([bemLocalizado, bemForaDeLocal, bemAusente]);

        inv.RegistrarContagem(bemLocalizado.BemPatrimonialId, SituacaoEncontrada.Localizado, "Almoxarifado", null);
        inv.RegistrarContagem(bemForaDeLocal.BemPatrimonialId, SituacaoEncontrada.LocalizadoOutroSetor, "Sala 2", null);
        // bemAusente nao contado -> Falta.
        inv.RegistrarBemNaoCadastrado("Cadeira sem tombo", "Recepcao", 150m);

        var divergencias = inv.Conciliar();

        inv.Situacao.Should().Be(SituacaoInventario.EmConciliacao);
        divergencias.Should().Contain(d => d.Tipo == TipoDivergencia.Falta);
        divergencias.Should().Contain(d => d.Tipo == TipoDivergencia.DivergenciaLocalizacao);
        divergencias.Should().Contain(d => d.Tipo == TipoDivergencia.Sobra);
    }

    [Fact]
    public void Encerrar_sem_conciliacao_deve_rejeitar()
    {
        var inv = Abrir();
        inv.CarregarSnapshotContabil([Bem("100", "Almoxarifado", 500m)]);

        var acao = () => inv.Encerrar(new DateOnly(2026, 6, 30));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Encerrar_apos_conciliacao_emite_inventario_encerrado()
    {
        var inv = Abrir();
        var bem = Bem("100", "Almoxarifado", 500m);
        inv.CarregarSnapshotContabil([bem]);
        inv.RegistrarContagem(bem.BemPatrimonialId, SituacaoEncontrada.Localizado, "Almoxarifado", null);
        inv.Conciliar();

        inv.Encerrar(new DateOnly(2026, 6, 30));

        inv.Situacao.Should().Be(SituacaoInventario.Encerrado);
        inv.DataEncerramento.Should().Be(new DateOnly(2026, 6, 30));
        inv.DomainEvents.Should().Contain(e => e is InventarioEncerrado);
    }
}
