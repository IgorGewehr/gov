using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ProntuarioSuas"/> e de suas entidades filhas (conteudo sigiloso — LGPD art. 11).</summary>
public sealed class ProntuarioSuasConfiguration : IEntityTypeConfiguration<ProntuarioSuas>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProntuarioSuas> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Prontuarios");
        builder.HasKey(prontuario => prontuario.Id);
        builder.Property(prontuario => prontuario.Id)
            .HasConversion(id => id.Value, value => new ProntuarioSuasId(value))
            .ValueGeneratedNever();

        builder.Property(prontuario => prontuario.FamiliaId);
        builder.Property(prontuario => prontuario.UnidadeAtendimentoId);
        builder.Property(prontuario => prontuario.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(prontuario => prontuario.DataAbertura);
        builder.Property(prontuario => prontuario.MotivoEncerramento).HasMaxLength(500);
        builder.Property(prontuario => prontuario.DataEncerramento);

        builder.OwnsOne(prontuario => prontuario.Plano, MapearPlano);

        builder.OwnsMany(prontuario => prontuario.Registros, MapearRegistros);
        builder.OwnsMany(prontuario => prontuario.Violacoes, MapearViolacoes);
        builder.OwnsMany(prontuario => prontuario.Acessos, MapearAcessos);

        builder.Navigation(prontuario => prontuario.Registros).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(prontuario => prontuario.Violacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(prontuario => prontuario.Acessos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(prontuario => new { prontuario.TenantId, prontuario.FamiliaId }).IsUnique();
    }

    private static void MapearPlano(OwnedNavigationBuilder<ProntuarioSuas, PlanoAcompanhamentoFamiliar> plano)
    {
        plano.ToTable("ProntuariosPlanos");
        plano.WithOwner().HasForeignKey("ProntuarioSuasId");
        plano.HasKey(item => item.Id);
        plano.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new PlanoId(value))
            .ValueGeneratedNever();
        plano.Property(item => item.Objetivos).HasMaxLength(2000);
        plano.Property(item => item.DataPactuacao);

        // Compromissos: lista de primitivos serializada como JSON (campo de apoio _compromissos),
        // mantendo a navegacao somente-leitura intacta no dominio.
        var comparadorCompromissos = new ValueComparer<List<string>>(
            (esquerda, direita) => (esquerda ?? new List<string>()).SequenceEqual(direita ?? new List<string>()),
            lista => lista.Aggregate(0, static (acumulado, item) => HashCode.Combine(acumulado, item.GetHashCode(StringComparison.Ordinal))),
            lista => lista.ToList());

        plano.Property<List<string>>("_compromissos")
            .HasColumnName("Compromissos")
            .HasMaxLength(4000)
            .HasConversion(
                lista => JsonSerializer.Serialize(lista, (JsonSerializerOptions?)null),
                texto => JsonSerializer.Deserialize<List<string>>(texto, (JsonSerializerOptions?)null) ?? new List<string>(),
                comparadorCompromissos);
        plano.Ignore(item => item.Compromissos);
    }

    private static void MapearRegistros(OwnedNavigationBuilder<ProntuarioSuas, RegistroAcompanhamento> registros)
    {
        registros.ToTable("ProntuariosRegistros");
        registros.WithOwner().HasForeignKey("ProntuarioSuasId");
        registros.HasKey(registro => registro.Id);
        registros.Property(registro => registro.Id)
            .HasConversion(id => id.Value, value => new RegistroAcompanhamentoId(value))
            .ValueGeneratedNever();
        registros.Property(registro => registro.Servico).HasConversion<string>().HasMaxLength(20);
        registros.Property(registro => registro.DataAtendimento);
        registros.Property(registro => registro.Descricao).HasMaxLength(4000);
        registros.Property(registro => registro.ProfissionalId);
    }

    private static void MapearViolacoes(OwnedNavigationBuilder<ProntuarioSuas, ViolacaoDireito> violacoes)
    {
        violacoes.ToTable("ProntuariosViolacoes");
        violacoes.WithOwner().HasForeignKey("ProntuarioSuasId");
        violacoes.HasKey(violacao => violacao.Id);
        violacoes.Property(violacao => violacao.Id)
            .HasConversion(id => id.Value, value => new ViolacaoId(value))
            .ValueGeneratedNever();
        violacoes.Property(violacao => violacao.TipoViolacao).HasConversion<string>().HasMaxLength(30);
        violacoes.Property(violacao => violacao.EnvolveCriancaAdolescente);
        violacoes.Property(violacao => violacao.DataIdentificacao);
    }

    private static void MapearAcessos(OwnedNavigationBuilder<ProntuarioSuas, AcessoProntuario> acessos)
    {
        acessos.ToTable("ProntuariosAcessos");
        acessos.WithOwner().HasForeignKey("ProntuarioSuasId");
        acessos.HasKey(acesso => acesso.Id);
        acessos.Property(acesso => acesso.Id)
            .HasConversion(id => id.Value, value => new AcessoId(value))
            .ValueGeneratedNever();
        acessos.Property(acesso => acesso.UsuarioId);
        acessos.Property(acesso => acesso.MotivoAcesso).HasMaxLength(500);
        acessos.Property(acesso => acesso.DataHoraAcessoUtc);
    }
}
