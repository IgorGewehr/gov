using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura do agregado <see cref="Norma"/>: invariantes de promulgacao/revogacao/alteracao (N-1..N-7),
/// busca por ementa/ano com isolamento por tenant e persistencia com auditoria.
/// </summary>
public sealed class NormaTests : LegislativoTestBase
{
    private static Norma NovaNorma(int numero = 10, int ano = 2025)
        => Norma.Promulgar(
            TenantA,
            TipoNorma.Lei,
            numero,
            ano,
            Ementa.De("Dispoe sobre o calendario de eventos do municipio."),
            new DateOnly(ano, 3, 10));

    [Fact] // N-3: nasce EmVigor com 1 evento Promulgacao e emite NormaPromulgada.
    public void Promulgar_nasce_em_vigor_com_evento_no_historico()
    {
        var norma = NovaNorma();

        norma.SituacaoVigencia.Should().Be(SituacaoVigencia.EmVigor);
        norma.HistoricoVigencia.Should().ContainSingle()
            .Which.Tipo.Should().Be(TipoEventoVigencia.Promulgacao);
        norma.DomainEvents.OfType<NormaPromulgada>().Should().ContainSingle();
    }

    [Fact] // N-1: numero invalido e rejeitado.
    public void Promulgar_numero_invalido_e_rejeitado()
    {
        var acao = () => Norma.Promulgar(TenantA, TipoNorma.Lei, 0, 2025, Ementa.De("X"), new DateOnly(2025, 1, 1));

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // N-4: revogar de norma em vigor marca Revogada e emite evento.
    public void Revogar_de_norma_em_vigor_marca_revogada_e_emite_evento()
    {
        var norma = NovaNorma();

        norma.Revogar(new DateOnly(2026, 1, 1));

        norma.SituacaoVigencia.Should().Be(SituacaoVigencia.Revogada);
        norma.DataRevogacao.Should().Be(new DateOnly(2026, 1, 1));
        norma.HistoricoVigencia.Should().Contain(evento => evento.Tipo == TipoEventoVigencia.Revogacao);
        norma.DomainEvents.OfType<NormaRevogada>().Should().ContainSingle();
    }

    [Fact] // N-4: revogar norma ja revogada lanca (terminal).
    public void Revogar_norma_ja_revogada_lanca()
    {
        var norma = NovaNorma();
        norma.Revogar(new DateOnly(2026, 1, 1));

        ((Action)(() => norma.Revogar(new DateOnly(2027, 1, 1))))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // N-4: data de revogacao anterior a promulgacao e rejeitada.
    public void Revogar_com_data_anterior_a_promulgacao_lanca()
    {
        var norma = NovaNorma(ano: 2025);

        ((Action)(() => norma.Revogar(new DateOnly(2024, 1, 1))))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // N-5: registrar alteracao em norma revogada lanca.
    public void RegistrarAlteracao_em_norma_revogada_lanca()
    {
        var norma = NovaNorma();
        norma.Revogar(new DateOnly(2026, 1, 1));

        ((Action)(() => norma.RegistrarAlteracao(new DateOnly(2026, 6, 1), NormaId.New())))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // N-5: alteracao nao e terminal — pode ser revogada depois.
    public void RegistrarAlteracao_marca_alterada_e_admite_revogacao_posterior()
    {
        var norma = NovaNorma();

        norma.RegistrarAlteracao(new DateOnly(2025, 8, 1), NormaId.New());
        norma.SituacaoVigencia.Should().Be(SituacaoVigencia.Alterada);

        norma.Revogar(new DateOnly(2026, 1, 1));
        norma.SituacaoVigencia.Should().Be(SituacaoVigencia.Revogada);
    }

    [Fact] // N-6: vincular proposicao de origem e idempotente (nao sobrescreve).
    public void VincularProposicaoOrigem_e_idempotente()
    {
        var norma = NovaNorma();
        var primeira = new ProposicaoId(Guid.NewGuid());
        var segunda = new ProposicaoId(Guid.NewGuid());

        norma.VincularProposicaoOrigem(primeira);
        norma.VincularProposicaoOrigem(segunda);

        norma.ProposicaoOrigemId.Should().Be(primeira);
    }

    [Fact] // N-2 (handler/EF) + busca por ementa/ano com isolamento por tenant (Global Query Filter).
    public async Task Buscar_por_ementa_e_ano_retorna_apenas_do_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Normas.Add(NovaNorma(numero: 1, ano: 2025));
            contexto.Normas.Add(NovaNorma(numero: 2, ano: 2024));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var de2025 = await contexto.Normas.Where(n => n.Ano == 2025).ToListAsync();
            de2025.Should().ContainSingle();
            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Normas.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact] // Regressao: busca por TERMO na ementa (substring) sobre o VO Ementa convertido — antes
           // lancava InvalidCastException ('System.String' -> Ementa) no NormaRepository.BuscarAsync.
    public async Task Buscar_por_termo_na_ementa_retorna_apenas_correspondentes_do_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Normas.Add(Norma.Promulgar(
                TenantA, TipoNorma.Lei, 100, 2026,
                Ementa.De("Dispoe sobre a denominacao da Rua das Acacias no Municipio."),
                new DateOnly(2026, 6, 10)));
            contexto.Normas.Add(Norma.Promulgar(
                TenantA, TipoNorma.Lei, 101, 2026,
                Ementa.De("Institui o calendario oficial de eventos do Municipio."),
                new DateOnly(2026, 6, 11)));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new Infrastructure.Persistence.Repositories.NormaRepository(contexto);

            // Busca case-insensitive: termo em minusculas casa com "Acacias" capitalizado na ementa.
            var (itens, total) = await repo.BuscarAsync(
                new Application.Abstractions.FiltroNormas(
                    Termo: "acacias", Tipo: null, Numero: null, Ano: null, Situacao: null, Pagina: 1, Tamanho: 20),
                CancellationToken.None);

            total.Should().Be(1);
            itens.Should().ContainSingle()
                .Which.Ementa.Valor.Should().Contain("Acacias");
        }

        // Isolamento: o termo nao vaza para outro tenant.
        await using (var contexto = CriarContexto(TenantB))
        {
            var repo = new Infrastructure.Persistence.Repositories.NormaRepository(contexto);
            var (_, total) = await repo.BuscarAsync(
                new Application.Abstractions.FiltroNormas(
                    Termo: "Acacias", Tipo: null, Numero: null, Ano: null, Situacao: null, Pagina: 1, Tamanho: 20),
                CancellationToken.None);

            total.Should().Be(0);
        }
    }
}
