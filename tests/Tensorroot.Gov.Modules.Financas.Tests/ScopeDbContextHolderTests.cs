using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Achado H5: o <see cref="ScopeDbContextHolder"/> NÃO pode aplicar last-writer-wins. Se dois
/// <see cref="ModuleDbContext"/> DIVERGENTES forem resolvidos no mesmo escopo, o segundo
/// <c>Definir</c> deve LANÇAR — caso contrário a unidade de trabalho confirmaria só o último,
/// descartando silenciosamente as mutações do primeiro. (O OutboxBackgroundService usa escopo por
/// módulo, então o fluxo legítimo nunca aciona a guarda.)
/// </summary>
public sealed class ScopeDbContextHolderTests
{
    private static FinancasDbContext NovoContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        // Sem holder no construtor: registramos manualmente para controlar a sequência no teste.
        return new FinancasDbContext(options, new TenantFake());
    }

    [Fact]
    public void Segundo_contexto_divergente_no_mesmo_escopo_lanca()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var holder = new ScopeDbContextHolder();

        using var primeiro = NovoContexto(conexao);
        using var segundo = NovoContexto(conexao);

        holder.Definir(primeiro);

        var acao = () => holder.Definir(segundo);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*mesmo escopo*");
        holder.Atual.Should().BeSameAs(primeiro); // o primeiro permanece — nada foi descartado silenciosamente
    }

    [Fact]
    public void Redefinir_o_mesmo_contexto_e_idempotente()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var holder = new ScopeDbContextHolder();

        using var contexto = NovoContexto(conexao);
        holder.Definir(contexto);

        var acao = () => holder.Definir(contexto);

        acao.Should().NotThrow();
        holder.Atual.Should().BeSameAs(contexto);
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Guid.Parse("88888888-8888-8888-8888-888888888888");

        public bool HasTenant => true;
    }
}
