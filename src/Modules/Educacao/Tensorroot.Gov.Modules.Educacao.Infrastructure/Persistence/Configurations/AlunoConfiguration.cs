using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Aluno"/> e de suas entidades filhas (responsaveis).</summary>
public sealed class AlunoConfiguration : IEntityTypeConfiguration<Aluno>
{
    // Conversor de CPF opcional (reference-type Value Object anulavel) — armazena os digitos sem mascara.
    private static readonly ValueConverter<Cpf?, string?> CpfOpcionalConverter = new(
        cpf => cpf == null ? null : cpf.Digitos,
        digitos => digitos == null ? null : Cpf.Create(digitos));

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Aluno> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Alunos");
        builder.HasKey(aluno => aluno.Id);
        builder.Property(aluno => aluno.Id)
            .HasConversion(id => id.Value, value => new AlunoId(value))
            .ValueGeneratedNever();

        builder.Property(aluno => aluno.Cpf)
            .HasConversion(CpfOpcionalConverter)
            .HasMaxLength(11);

        builder.Property(aluno => aluno.CodigoInepAluno).HasMaxLength(Aluno.ComprimentoCodigoInepAluno);
        builder.Property(aluno => aluno.Situacao).HasConversion<string>().HasMaxLength(20);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(aluno => aluno.Ativo);

        // Dados civis e endereco residencial (readonly record struct) — complex types na mesma tabela.
        builder.ComplexProperty(aluno => aluno.DadosCivis, MapearDadosCivis);
        builder.ComplexProperty(aluno => aluno.Endereco, MapearEndereco);

        builder.OwnsMany(aluno => aluno.Responsaveis, MapearResponsaveis);
        builder.Navigation(aluno => aluno.Responsaveis).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indice de lookup por CPF dentro do tenant (I-A4 — a unicidade efetiva e garantida pelo
        // handler via ExisteCpfAsync; nao-unico no banco porque o CPF e opcional e ha multiplos nulos,
        // cuja semantica difere entre SQLite e SqlServer).
        builder.HasIndex(aluno => new { aluno.TenantId, aluno.Cpf });
    }

    private static void MapearDadosCivis(ComplexPropertyBuilder<DadosCivis> dados)
    {
        dados.Property(item => item.Nome).HasColumnName("Nome").HasMaxLength(DadosCivis.ComprimentoNome);
        dados.Property(item => item.NomeSocial).HasColumnName("NomeSocial").HasMaxLength(DadosCivis.ComprimentoNome);
        dados.Property(item => item.DataNascimento).HasColumnName("DataNascimento");
        dados.Property(item => item.Sexo).HasColumnName("Sexo").HasConversion<string>().HasMaxLength(20);
        dados.Property(item => item.NomeMae).HasColumnName("NomeMae").HasMaxLength(DadosCivis.ComprimentoNome);
        dados.Property(item => item.NomePai).HasColumnName("NomePai").HasMaxLength(DadosCivis.ComprimentoNome);
    }

    private static void MapearEndereco(ComplexPropertyBuilder<EnderecoAluno> endereco)
    {
        endereco.Property(item => item.Logradouro).HasColumnName("EnderecoLogradouro").HasMaxLength(200);
        endereco.Property(item => item.Numero).HasColumnName("EnderecoNumero").HasMaxLength(30);
        endereco.Property(item => item.Bairro).HasColumnName("EnderecoBairro").HasMaxLength(120);
        endereco.Property(item => item.Municipio).HasColumnName("EnderecoMunicipio").HasMaxLength(120);
        endereco.Property(item => item.Uf).HasColumnName("EnderecoUf").HasMaxLength(EnderecoAluno.ComprimentoUf);
        endereco.Property(item => item.Cep).HasColumnName("EnderecoCep").HasMaxLength(8);
    }

    private static void MapearResponsaveis(OwnedNavigationBuilder<Aluno, Responsavel> responsaveis)
    {
        responsaveis.ToTable("AlunosResponsaveis");
        responsaveis.WithOwner().HasForeignKey("AlunoId");
        responsaveis.HasKey(responsavel => responsavel.Id);
        responsaveis.Property(responsavel => responsavel.Id)
            .HasConversion(id => id.Value, value => new ResponsavelId(value))
            .ValueGeneratedNever();

        responsaveis.Property(responsavel => responsavel.Nome).HasMaxLength(200);
        responsaveis.Property(responsavel => responsavel.Cpf)
            .HasConversion(CpfOpcionalConverter)
            .HasMaxLength(11);
        responsaveis.Property(responsavel => responsavel.Parentesco).HasConversion<string>().HasMaxLength(20);
        responsaveis.Property(responsavel => responsavel.Telefone).HasMaxLength(20);
        responsaveis.Property(responsavel => responsavel.ResponsavelFinanceiro);
        responsaveis.Property(responsavel => responsavel.AutorizadoBuscar);
    }
}
