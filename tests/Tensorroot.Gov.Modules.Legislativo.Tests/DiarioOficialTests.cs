using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura do agregado <see cref="EdicaoDiario"/>: invariantes de rascunho/publicacao/retificacao
/// (D-1..D-6) e persistencia com isolamento por tenant.
/// </summary>
public sealed class DiarioOficialTests : LegislativoTestBase
{
    private static readonly DateTimeOffset Agora = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    private static EdicaoDiario NovaEdicaoComMateria(int numero = 1, int ano = 2026)
    {
        var edicao = EdicaoDiario.Abrir(TenantA, numero, ano);
        edicao.AdicionarMateria(TipoMateria.Norma, "Lei n. 10/2026", conteudo: null, referenciaId: Guid.NewGuid());
        return edicao;
    }

    [Fact] // D-1: nasce rascunho sem data.
    public void Abrir_nasce_rascunho_sem_data()
    {
        var edicao = EdicaoDiario.Abrir(TenantA, 1, 2026);

        edicao.Situacao.Should().Be(SituacaoEdicao.Rascunho);
        edicao.DataPublicacao.Should().BeNull();
        edicao.Publicada.Should().BeFalse();
    }

    [Fact] // D-2/D-4: adicionar materia em edicao publicada lanca.
    public void AdicionarMateria_em_edicao_publicada_lanca()
    {
        var edicao = NovaEdicaoComMateria();
        edicao.Publicar(Agora, Agora);

        ((Action)(() => edicao.AdicionarMateria(TipoMateria.Outro, "X", "conteudo", null)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // D-3: publicar sem materia lanca.
    public void Publicar_sem_materia_lanca()
    {
        var edicao = EdicaoDiario.Abrir(TenantA, 1, 2026);

        ((Action)(() => edicao.Publicar(Agora, Agora)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // D-3: publicar define data e emite DiarioPublicado.
    public void Publicar_define_data_e_emite_evento()
    {
        var edicao = NovaEdicaoComMateria();

        edicao.Publicar(Agora, Agora);

        edicao.Situacao.Should().Be(SituacaoEdicao.Publicada);
        edicao.DataPublicacao.Should().Be(Agora);
        edicao.DomainEvents.OfType<DiarioPublicado>().Should().ContainSingle();
    }

    [Fact] // D-6: data de publicacao futura e rejeitada.
    public void Publicar_com_data_futura_lanca()
    {
        var edicao = NovaEdicaoComMateria();

        ((Action)(() => edicao.Publicar(Agora.AddHours(1), Agora)))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // D-3: republicar e no-op (idempotente) — nao reemite o evento.
    public void Publicar_idempotente_nao_republica()
    {
        var edicao = NovaEdicaoComMateria();
        edicao.Publicar(Agora, Agora);
        edicao.ClearDomainEvents();

        edicao.Publicar(Agora.AddMinutes(5), Agora.AddMinutes(5));

        edicao.DataPublicacao.Should().Be(Agora); // data original preservada.
        edicao.DomainEvents.OfType<DiarioPublicado>().Should().BeEmpty();
    }

    [Fact] // D-4: retificacao cria nova edicao referenciando a original, sem mutar a original.
    public void Retificar_cria_nova_edicao_referenciando_original()
    {
        var original = NovaEdicaoComMateria(numero: 1);
        original.Publicar(Agora, Agora);

        var retificadora = EdicaoDiario.Retificar(TenantA, 2, 2026, original.Id);

        retificadora.EdicaoOriginalId.Should().Be(original.Id);
        retificadora.Situacao.Should().Be(SituacaoEdicao.Rascunho);
        original.Situacao.Should().Be(SituacaoEdicao.Publicada); // original intacta.
    }

    [Fact] // D-5 (EF): numero unico por (tenant, ano) + isolamento por tenant.
    public async Task Persiste_com_isolamento_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.DiarioEdicoes.Add(NovaEdicaoComMateria(numero: 1));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            (await contexto.DiarioEdicoes.ToListAsync()).Should().ContainSingle();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.DiarioEdicoes.ToListAsync()).Should().BeEmpty();
        }
    }
}
