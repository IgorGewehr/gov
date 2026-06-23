using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Paciente"/> e de suas entidades filhas.</summary>
public sealed class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Pacientes");
        builder.HasKey(paciente => paciente.Id);
        builder.Property(paciente => paciente.Id)
            .HasConversion(id => id.Value, value => new PacienteId(value))
            .ValueGeneratedNever();

        builder.Property(paciente => paciente.Cns)
            .HasConversion(cns => cns.Valor, valor => new Cns(valor))
            .HasMaxLength(Cns.Comprimento);

        builder.Property(paciente => paciente.CnsConfirmado);
        builder.Property(paciente => paciente.Situacao).HasConversion<string>().HasMaxLength(20);

        // Dados civis (owned struct com guarda nao mapeavel) persistidos como coluna JSON unica.
        // Sem HasColumnType explicito: o provider mapeia o conversor de string ilimitada para
        // nvarchar(max) (SQL Server) ou TEXT (SQLite), mantendo portabilidade nos testes.
        builder.Property(paciente => paciente.Identificacao)
            .HasConversion(new IdentificacaoConverter())
            .HasColumnName("Identificacao");

        // Colunas-sombra (string crua) para busca translatavel em SQL sem passar pelo value converter
        // JSON da Identificacao (que causaria InvalidCastException ao comparar com string). Mantidas em
        // sincronia via gatilho de gravacao no SaveChanges do contexto. NomeBusca normalizado (lowercase
        // + sem diacriticos); CpfBusca apenas digitos. Espelha o padrao EmentaBusca do Legislativo.
        builder.Property<string>("NomeBusca").HasMaxLength(Identificacao.ComprimentoNome);
        builder.Property<string?>("CpfBusca").HasMaxLength(11);
        builder.HasIndex("TenantId", "NomeBusca");
        builder.HasIndex("TenantId", "CpfBusca");

        builder.ComplexProperty(paciente => paciente.Endereco, MapearEndereco);

        builder.OwnsMany(paciente => paciente.Condicoes, MapearCondicoes);
        builder.OwnsMany(paciente => paciente.Alergias, MapearAlergias);
        builder.Navigation(paciente => paciente.Condicoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(paciente => paciente.Alergias).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unicidade do CNS por tenant (I-9): um paciente por CNS no ente publico.
        builder.HasIndex(paciente => new { paciente.TenantId, paciente.Cns }).IsUnique();
    }

    private static void MapearEndereco(ComplexPropertyBuilder<Endereco> endereco)
    {
        endereco.Property(dados => dados.Logradouro).HasColumnName("Logradouro").HasMaxLength(200);
        endereco.Property(dados => dados.Numero).HasColumnName("Numero").HasMaxLength(30);
        endereco.Property(dados => dados.Bairro).HasColumnName("Bairro").HasMaxLength(120);
        endereco.Property(dados => dados.Municipio).HasColumnName("Municipio").HasMaxLength(120);
        endereco.Property(dados => dados.Uf).HasColumnName("Uf").HasMaxLength(Endereco.ComprimentoUf);
        endereco.Property(dados => dados.Cep).HasColumnName("Cep").HasMaxLength(8);
    }

    private static void MapearCondicoes(OwnedNavigationBuilder<Paciente, CondicaoDeSaude> condicoes)
    {
        condicoes.ToTable("PacientesCondicoes");
        condicoes.WithOwner().HasForeignKey("PacienteId");
        condicoes.HasKey(condicao => condicao.Id);
        condicoes.Property(condicao => condicao.Id)
            .HasConversion(id => id.Value, value => new CondicaoDeSaudeId(value))
            .ValueGeneratedNever();
        condicoes.Property(condicao => condicao.Codigo).HasMaxLength(CondicaoDeSaude.ComprimentoCodigo);
        condicoes.Property(condicao => condicao.Descricao).HasMaxLength(200);
    }

    private static void MapearAlergias(OwnedNavigationBuilder<Paciente, Alergia> alergias)
    {
        alergias.ToTable("PacientesAlergias");
        alergias.WithOwner().HasForeignKey("PacienteId");
        alergias.HasKey(alergia => alergia.Id);
        alergias.Property(alergia => alergia.Id)
            .HasConversion(id => id.Value, value => new AlergiaId(value))
            .ValueGeneratedNever();
        alergias.Property(alergia => alergia.Substancia).HasMaxLength(Alergia.ComprimentoSubstancia);
        alergias.Property(alergia => alergia.Gravidade).HasMaxLength(40);
    }
}
