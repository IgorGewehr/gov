using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="Usuario"/>. A senha e persistida APENAS como hash
/// (coluna <c>SenhaHash</c>); a senha em claro nunca toca o banco. O e-mail e mapeado como coluna
/// indexada e UNICA por tenant. Os papeis (conjunto de <see cref="PapelId"/>) sao serializados em
/// coluna JSON, mantendo o agregado coeso (sem expor tabela de juncao no dominio).
/// </summary>
public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Usuarios");
        builder.HasKey(usuario => usuario.Id);
        builder.Property(usuario => usuario.Id)
            .HasConversion(id => id.Value, value => new UsuarioId(value))
            .ValueGeneratedNever();

        builder.Property(usuario => usuario.TenantId).IsRequired();
        builder.Property(usuario => usuario.Nome).HasMaxLength(Usuario.ComprimentoMaximoNome).IsRequired();

        // Email como Value Object convertido para a coluna "Email" (normalizado no dominio).
        builder.Property(usuario => usuario.Email)
            .HasConversion(email => email.Valor, valor => Email.De(valor))
            .HasColumnName("Email")
            .HasMaxLength(Email.ComprimentoMaximo)
            .IsRequired();

        // Hash da senha (BCrypt: "$2a$..." ~ 60 chars; folga para futuras variantes/custos).
        builder.Property(usuario => usuario.SenhaHash).HasMaxLength(200).IsRequired();
        builder.Property(usuario => usuario.Ativo).IsRequired();

        // E-mail unico POR TENANT (a unicidade global e garantida pelo indice central da plataforma).
        builder.HasIndex(usuario => new { usuario.TenantId, usuario.Email }).IsUnique();

        // Papeis (HashSet<PapelId>) serializados em coluna JSON, acessados pelo backing field.
        var conversor = new ValueConverter<IReadOnlySet<PapelId>, string>(
            papeis => Serializar(papeis),
            json => Desserializar(json));

        var comparador = new ValueComparer<IReadOnlySet<PapelId>>(
            (esquerda, direita) => esquerda!.SetEquals(direita!),
            papeis => papeis.Aggregate(0, (hash, papel) => HashCode.Combine(hash, papel.Value)),
            papeis => new HashSet<PapelId>(papeis));

        builder.Property(usuario => usuario.Papeis)
            .HasField("_papeis")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Papeis")
            .HasConversion(conversor)
            .Metadata.SetValueComparer(comparador);

        // FONTE DE VERDADE RICA (MODELO §2.2/§10.3): as atribuicoes de papel COM ESCOPO de UO sao
        // persistidas como entidades-filhas (owned) na tabela "AtribuicoesPapel". A coluna JSON
        // "Papeis" acima permanece como visao PLANA derivada (mantida em sincronia pelo dominio),
        // preservando o enforcement RBAC/claim "perm" atual; o escopo organizacional (UO + vigencia
        // + origem) vive aqui. As atribuicoes sao SEMPRE carregadas com o usuario (sao parte do
        // agregado e necessarias ao calculo do escopo efetivo e a prova da regra I4). A navegacao e
        // mapeada pela PROPRIEDADE publica (somente-leitura) com acesso pelo backing field _atribuicoes.
        builder.OwnsMany(usuario => usuario.Atribuicoes, ConfigurarAtribuicoes);
        builder.Navigation(usuario => usuario.Atribuicoes)
            .HasField("_atribuicoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }

    private static void ConfigurarAtribuicoes(OwnedNavigationBuilder<Usuario, AtribuicaoDePapel> atribuicoes)
    {
        atribuicoes.ToTable("AtribuicoesPapel");

        // Chave do owned-type + FK implicita para o Usuario (UsuarioId, sombra/convencao do EF).
        atribuicoes.WithOwner().HasForeignKey("UsuarioId");
        atribuicoes.HasKey(atribuicao => atribuicao.Id);
        atribuicoes.Property(atribuicao => atribuicao.Id)
            .HasConversion(id => id.Value, value => new AtribuicaoDePapelId(value))
            .HasColumnName("Id")
            .ValueGeneratedNever();

        atribuicoes.Property(atribuicao => atribuicao.PapelId)
            .HasConversion(id => id.Value, value => new PapelId(value))
            .HasColumnName("PapelId")
            .IsRequired();

        atribuicoes.Property(atribuicao => atribuicao.UnidadeId)
            .HasConversion(id => id.Value, value => new UnidadeOrganizacionalId(value))
            .HasColumnName("UnidadeId")
            .IsRequired();

        atribuicoes.Property(atribuicao => atribuicao.IncluiSubunidades).IsRequired();

        // AA-5/D3: profundidade da cadeia de (sub)delegacao (0 = direta). Default 0 cobre as linhas
        // legadas (criadas antes do controle de profundidade).
        atribuicoes.Property(atribuicao => atribuicao.ProfundidadeDelegacao)
            .HasColumnName("ProfundidadeDelegacao")
            .HasDefaultValue(0)
            .IsRequired();

        // Vigencia (VO) achatada em colunas. O fim e opcional (vigencia aberta).
        atribuicoes.OwnsOne(atribuicao => atribuicao.Vigencia, vigencia =>
        {
            vigencia.Property(valor => valor.Inicio).HasColumnName("VigenciaInicio").IsRequired();
            vigencia.Property(valor => valor.Fim).HasColumnName("VigenciaFim");
        });
        atribuicoes.Navigation(atribuicao => atribuicao.Vigencia).IsRequired();

        // Origem (VO): tipo + concedente opcional (procedencia/trilha — I8).
        atribuicoes.OwnsOne(atribuicao => atribuicao.Origem, origem =>
        {
            origem.Property(valor => valor.Tipo).HasColumnName("OrigemTipo").IsRequired();
            origem.Property(valor => valor.ConcedentId)
                .HasConversion(
                    id => id == null ? (Guid?)null : id.Value.Value,
                    value => value == null ? (UsuarioId?)null : new UsuarioId(value.Value))
                .HasColumnName("OrigemConcedentId");
        });
        atribuicoes.Navigation(atribuicao => atribuicao.Origem).IsRequired();

        atribuicoes.HasIndex("UsuarioId", "PapelId", "UnidadeId");
    }

    private static string Serializar(IReadOnlySet<PapelId> papeis)
        => JsonSerializer.Serialize(papeis.Select(papel => papel.Value), JsonOptions);

    private static HashSet<PapelId> Desserializar(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var ids = JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
        return ids.Select(id => new PapelId(id)).ToHashSet();
    }
}
