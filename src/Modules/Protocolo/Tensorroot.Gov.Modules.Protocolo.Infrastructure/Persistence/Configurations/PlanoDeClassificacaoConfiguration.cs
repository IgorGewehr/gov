using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="PlanoDeClassificacao"/> e suas classes (arvore).</summary>
public sealed class PlanoDeClassificacaoConfiguration : IEntityTypeConfiguration<PlanoDeClassificacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoDeClassificacao> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlanosClassificacao");
        builder.HasKey(plano => plano.Id);
        builder.Property(plano => plano.Id)
            .HasConversion(id => id.Value, value => new PlanoDeClassificacaoId(value))
            .ValueGeneratedNever();

        builder.Property(plano => plano.Nome).HasMaxLength(200).IsRequired();
        builder.Property(plano => plano.Ativo);

        builder.OwnsMany(plano => plano.Classes, MapearClasses);
        builder.Navigation(plano => plano.Classes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Um plano ATIVO por tenant na pratica; indice de busca por (tenant, ativo).
        builder.HasIndex(plano => new { plano.TenantId, plano.Ativo });
    }

    private static void MapearClasses(OwnedNavigationBuilder<PlanoDeClassificacao, ClasseDocumental> classes)
    {
        classes.ToTable("ClassesDocumentais");
        classes.WithOwner().HasForeignKey("PlanoDeClassificacaoId");
        classes.HasKey(classe => classe.Id);
        classes.Property(classe => classe.Id)
            .HasConversion(id => id.Value, value => new ClasseDocumentalId(value))
            .ValueGeneratedNever();

        classes.Property(classe => classe.CodigoClassificacao).HasMaxLength(ClasseDocumental.ComprimentoMaximoCodigo).IsRequired();
        classes.Property(classe => classe.Assunto).HasMaxLength(ClasseDocumental.ComprimentoMaximoAssunto).IsRequired();
        classes.Property(classe => classe.Atividade).HasConversion<string>().HasMaxLength(10);
        classes.Property(classe => classe.CodigoPai).HasMaxLength(ClasseDocumental.ComprimentoMaximoCodigo);

        classes.HasIndex("PlanoDeClassificacaoId", nameof(ClasseDocumental.CodigoClassificacao));
    }
}
