using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Events;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Bloqueio por excesso de tentativas (P7) no agregado <see cref="Usuario"/>: contagem de falhas,
/// transicao para bloqueada na N-esima falha, janela de bloqueio avaliada por INSTANTE INJETADO
/// (sem relogio no dominio) e reset no sucesso. E SEGURANCA CRITICA (anti brute-force).
/// </summary>
public sealed class UsuarioLockoutTests
{
    private static readonly Guid Tenant = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset T0 = new(2026, 06, 29, 12, 00, 00, TimeSpan.Zero);
    private static readonly TimeSpan Bloqueio15Min = TimeSpan.FromMinutes(15);

    private static Usuario NovoUsuario()
        => Usuario.Criar(Tenant, "Maria", Email.De("maria@x.gov.br"), "hash-sintetico-de-teste");

    [Fact]
    public void Usuario_novo_nao_esta_bloqueado()
    {
        var usuario = NovoUsuario();

        usuario.AccessFailedCount.Should().Be(0);
        usuario.LockoutEnd.Should().BeNull();
        usuario.EstaBloqueado(T0).Should().BeFalse();
    }

    [Fact]
    public void Falhas_abaixo_do_limite_apenas_incrementam_sem_bloquear()
    {
        var usuario = NovoUsuario();

        usuario.RegistrarFalhaDeLogin(T0, limiteTentativas: 5, Bloqueio15Min);
        usuario.RegistrarFalhaDeLogin(T0, limiteTentativas: 5, Bloqueio15Min);

        usuario.AccessFailedCount.Should().Be(2);
        usuario.LockoutEnd.Should().BeNull();
        usuario.EstaBloqueado(T0).Should().BeFalse();
    }

    [Fact]
    public void Na_quinta_falha_bloqueia_zera_contador_e_emite_evento()
    {
        var usuario = NovoUsuario();
        usuario.ClearDomainEvents();

        for (var i = 0; i < 5; i++)
        {
            usuario.RegistrarFalhaDeLogin(T0, limiteTentativas: 5, Bloqueio15Min);
        }

        usuario.LockoutEnd.Should().Be(T0.Add(Bloqueio15Min));
        usuario.AccessFailedCount.Should().Be(0, "o contador zera ao bloquear (recomeca apos a janela)");
        usuario.EstaBloqueado(T0.AddMinutes(14)).Should().BeTrue();
        usuario.EstaBloqueado(T0.AddMinutes(15)).Should().BeFalse("o bloqueio expira exatamente no fim da janela");
        usuario.DomainEvents.Should().ContainSingle(evento => evento is UsuarioBloqueadoPorTentativas);
    }

    [Fact]
    public void Sucesso_apos_falhas_zera_o_estado()
    {
        var usuario = NovoUsuario();
        usuario.RegistrarFalhaDeLogin(T0, limiteTentativas: 5, Bloqueio15Min);
        usuario.RegistrarFalhaDeLogin(T0, limiteTentativas: 5, Bloqueio15Min);

        usuario.RegistrarAutenticacaoBemSucedida();

        usuario.AccessFailedCount.Should().Be(0);
        usuario.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public void Limite_invalido_e_rejeitado()
    {
        var usuario = NovoUsuario();

        var acao = () => usuario.RegistrarFalhaDeLogin(T0, limiteTentativas: 0, Bloqueio15Min);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
