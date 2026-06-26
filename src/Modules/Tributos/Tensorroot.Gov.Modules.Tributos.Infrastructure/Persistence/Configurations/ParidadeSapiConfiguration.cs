using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Desif;
using Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;
using Tensorroot.Gov.Modules.Tributos.Domain.Sim;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DeclaracaoDesif"/> (DES-IF) e seus subtítulos.</summary>
public sealed class DeclaracaoDesifConfiguration : IEntityTypeConfiguration<DeclaracaoDesif>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeclaracaoDesif> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DeclaracoesDesif");
        builder.HasKey(declaracao => declaracao.Id);
        builder.Property(declaracao => declaracao.Id)
            .HasConversion(id => id.Value, value => new DeclaracaoDesifId(value))
            .ValueGeneratedNever();

        builder.Property(declaracao => declaracao.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(declaracao => declaracao.Modulo).HasConversion<string>().HasMaxLength(40);
        builder.Property(declaracao => declaracao.Competencia)
            .HasConversion(c => (c.Ano * 100) + c.Mes, valor => Competencia.De(valor / 100, valor % 100));
        builder.Property(declaracao => declaracao.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(declaracao => declaracao.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(declaracao => declaracao.DataEntrega);

        builder.Property(declaracao => declaracao.ReceitaTributavelTotal)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(declaracao => declaracao.IssqnDevidoBruto)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(declaracao => declaracao.DeducoesReceita)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(declaracao => declaracao.IncentivosFiscais)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(declaracao => declaracao.DepositosJudiciais)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(declaracao => declaracao.IssqnARecolher)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasMany(declaracao => declaracao.Subtitulos).WithOne().HasForeignKey(s => s.DeclaracaoDesifId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(declaracao => declaracao.Subtitulos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(declaracao => new { declaracao.TenantId, declaracao.ContribuinteId, declaracao.Competencia });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="SubtituloDesif"/> (Registro 0430).</summary>
public sealed class SubtituloDesifConfiguration : IEntityTypeConfiguration<SubtituloDesif>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SubtituloDesif> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SubtitulosDesif");
        builder.HasKey(subtitulo => subtitulo.Id);
        builder.Property(subtitulo => subtitulo.Id)
            .HasConversion(id => id.Value, value => new SubtituloDesifId(value))
            .ValueGeneratedNever();

        builder.Property(subtitulo => subtitulo.DeclaracaoDesifId)
            .HasConversion(id => id.Value, value => new DeclaracaoDesifId(value));

        builder.Property(subtitulo => subtitulo.ContaCosif).HasMaxLength(30).IsRequired();
        builder.Property(subtitulo => subtitulo.CodigoTributacaoDesif).HasMaxLength(20).IsRequired();
        builder.Property(subtitulo => subtitulo.ItemListaServico).HasMaxLength(10).IsRequired();
        builder.Property(subtitulo => subtitulo.Descricao).HasMaxLength(300).IsRequired();
        builder.Property(subtitulo => subtitulo.BaseCalculo)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(subtitulo => subtitulo.AliquotaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(subtitulo => subtitulo.IssqnDevido)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(subtitulo => subtitulo.DeclaracaoDesifId);
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="TituloRegistroSim"/> (S.I.M.) e seus produtos.</summary>
public sealed class TituloRegistroSimConfiguration : IEntityTypeConfiguration<TituloRegistroSim>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TituloRegistroSim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TitulosRegistroSim");
        builder.HasKey(titulo => titulo.Id);
        builder.Property(titulo => titulo.Id)
            .HasConversion(id => id.Value, value => new TituloRegistroSimId(value))
            .ValueGeneratedNever();

        builder.Property(titulo => titulo.ResponsavelId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(titulo => titulo.RazaoSocialEstabelecimento).HasMaxLength(200).IsRequired();
        builder.Property(titulo => titulo.Natureza).HasConversion<string>().HasMaxLength(30);
        builder.Property(titulo => titulo.EnderecoEstabelecimento).HasMaxLength(300).IsRequired();
        builder.Property(titulo => titulo.DataRequerimento);
        builder.Property(titulo => titulo.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(titulo => titulo.NumeroSim).HasMaxLength(40);
        builder.Property(titulo => titulo.DataRegistro);
        builder.Property(titulo => titulo.FimVigencia);

        builder.HasMany(titulo => titulo.Produtos).WithOne().HasForeignKey(p => p.TituloRegistroSimId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(titulo => titulo.Produtos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(titulo => new { titulo.TenantId, titulo.NumeroSim });
        builder.HasIndex(titulo => new { titulo.TenantId, titulo.ResponsavelId });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="ProdutoInspecionadoSim"/>.</summary>
public sealed class ProdutoInspecionadoSimConfiguration : IEntityTypeConfiguration<ProdutoInspecionadoSim>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProdutoInspecionadoSim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProdutosInspecionadosSim");
        builder.HasKey(produto => produto.Id);
        builder.Property(produto => produto.Id)
            .HasConversion(id => id.Value, value => new ProdutoInspecionadoSimId(value))
            .ValueGeneratedNever();

        builder.Property(produto => produto.TituloRegistroSimId)
            .HasConversion(id => id.Value, value => new TituloRegistroSimId(value));

        builder.Property(produto => produto.Denominacao).HasMaxLength(200).IsRequired();
        builder.Property(produto => produto.Classificacao).HasMaxLength(120).IsRequired();
        builder.Property(produto => produto.NumeroRotulo).HasMaxLength(40).IsRequired();

        builder.HasIndex(produto => produto.TituloRegistroSimId);
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="DomicilioEletronicoContribuinte"/> (DEC) e suas mensagens.</summary>
public sealed class DomicilioEletronicoContribuinteConfiguration : IEntityTypeConfiguration<DomicilioEletronicoContribuinte>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DomicilioEletronicoContribuinte> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DomiciliosEletronicos");
        builder.HasKey(domicilio => domicilio.Id);
        builder.Property(domicilio => domicilio.Id)
            .HasConversion(id => id.Value, value => new DomicilioEletronicoContribuinteId(value))
            .ValueGeneratedNever();

        builder.Property(domicilio => domicilio.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(domicilio => domicilio.DataAdesao);
        builder.Property(domicilio => domicilio.DiasCienciaTacita);
        builder.Property(domicilio => domicilio.Ativo);

        builder.HasMany(domicilio => domicilio.Mensagens).WithOne().HasForeignKey(m => m.DomicilioEletronicoContribuinteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(domicilio => domicilio.Mensagens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(domicilio => new { domicilio.TenantId, domicilio.ContribuinteId });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="MensagemFiscal"/> (caixa do DEC).</summary>
public sealed class MensagemFiscalConfiguration : IEntityTypeConfiguration<MensagemFiscal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MensagemFiscal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("MensagensFiscais");
        builder.HasKey(mensagem => mensagem.Id);
        builder.Property(mensagem => mensagem.Id)
            .HasConversion(id => id.Value, value => new MensagemFiscalId(value))
            .ValueGeneratedNever();

        builder.Property(mensagem => mensagem.DomicilioEletronicoContribuinteId)
            .HasConversion(id => id.Value, value => new DomicilioEletronicoContribuinteId(value));

        builder.Property(mensagem => mensagem.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(mensagem => mensagem.Assunto).HasMaxLength(200).IsRequired();
        builder.Property(mensagem => mensagem.Corpo).HasMaxLength(8000).IsRequired();
        builder.Property(mensagem => mensagem.ReferenciaExterna).HasMaxLength(60);
        builder.Property(mensagem => mensagem.DataDisponibilizacao);
        builder.Property(mensagem => mensagem.DataLimiteCienciaTacita);
        builder.Property(mensagem => mensagem.DiasPrazoManifestacao);
        builder.Property(mensagem => mensagem.Forma).HasConversion<string>().HasMaxLength(20);
        builder.Property(mensagem => mensagem.DataCiencia);
        builder.Property(mensagem => mensagem.DataLimiteManifestacao);

        builder.HasIndex(mensagem => mensagem.DomicilioEletronicoContribuinteId);
    }
}
