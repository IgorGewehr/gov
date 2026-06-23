using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Bens = Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Veiculo"/> e de suas entidades filhas.</summary>
public sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Veiculos");
        builder.HasKey(veiculo => veiculo.Id);
        builder.Property(veiculo => veiculo.Id)
            .HasConversion(id => id.Value, value => new VeiculoId(value))
            .ValueGeneratedNever();

        builder.Property(veiculo => veiculo.Descricao).HasMaxLength(200);
        builder.Property(veiculo => veiculo.Origem).HasMaxLength(100);
        builder.Property(veiculo => veiculo.NumeroTombamento).HasMaxLength(40);
        builder.Property(veiculo => veiculo.Situacao).HasConversion<string>().HasMaxLength(30);

        builder.Property(veiculo => veiculo.Placa)
            .HasConversion(placa => placa.Valor, valor => Placa.Criar(valor))
            .HasMaxLength(7);
        builder.Property(veiculo => veiculo.Renavam)
            .HasConversion(renavam => renavam.Digitos, valor => Renavam.Criar(valor))
            .HasMaxLength(11);

        // Colunas-sombra (string crua) para busca textual por placa/RENAVAM sem passar pelos value
        // converters dos VOs Placa/Renavam (que causariam InvalidCastException no LIKE). Sincronizadas
        // no SaveChanges do contexto. Espelham o padrao EmentaBusca do Legislativo.
        builder.Property<string>("PlacaBusca").HasMaxLength(7);
        builder.Property<string>("RenavamBusca").HasMaxLength(11);
        builder.HasIndex("TenantId", "PlacaBusca");
        builder.HasIndex("TenantId", "RenavamBusca");
        builder.Property(veiculo => veiculo.Odometro)
            .HasConversion(odometro => odometro.Valor, valor => Odometro.De(valor));
        builder.Property(veiculo => veiculo.Horimetro)
            .HasConversion(horimetro => horimetro.Valor, valor => Horimetro.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(veiculo => veiculo.ValorInicial)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(veiculo => veiculo.ValorResidual)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(veiculo => veiculo.ValorContabil)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        // Propriedade calculada (sem coluna).
        builder.Ignore(veiculo => veiculo.AtivoNoAcervo);

        // Propriedades calculadas (sem coluna) — BUG-P4.
        builder.Ignore(veiculo => veiculo.CompetenciasDepreciadas);
        builder.Ignore(veiculo => veiculo.VidaUtilRemanescenteMeses);
        builder.Ignore(veiculo => veiculo.ParcelaMensalDepreciacao);

        builder.OwnsMany(veiculo => veiculo.Abastecimentos, MapearAbastecimentos);
        builder.OwnsMany(veiculo => veiculo.OrdensServico, MapearOrdensServico);
        builder.OwnsMany(veiculo => veiculo.Multas, MapearMultas);
        builder.OwnsMany(veiculo => veiculo.Licenciamentos, MapearLicenciamentos);
        builder.OwnsMany(veiculo => veiculo.Motoristas, MapearMotoristas);
        builder.OwnsMany(veiculo => veiculo.HistoricosDepreciacao, MapearHistoricosDepreciacao);

        builder.Navigation(veiculo => veiculo.Abastecimentos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(veiculo => veiculo.OrdensServico).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(veiculo => veiculo.Multas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(veiculo => veiculo.Licenciamentos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(veiculo => veiculo.Motoristas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(veiculo => veiculo.HistoricosDepreciacao).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(veiculo => new { veiculo.TenantId, veiculo.Renavam }).IsUnique();
    }

    private static void MapearHistoricosDepreciacao(OwnedNavigationBuilder<Veiculo, Bens.HistoricoDepreciacao> historicos)
    {
        historicos.ToTable("VeiculosHistoricosDepreciacao");
        historicos.WithOwner().HasForeignKey("VeiculoId");
        historicos.HasKey(historico => historico.Id);
        historicos.Property(historico => historico.Id)
            .HasConversion(id => id.Value, value => new Bens.HistoricoDepreciacaoId(value))
            .ValueGeneratedNever();
        historicos.Property(historico => historico.ValorDepreciado).HasColumnType("decimal(18,2)");
        historicos.Property(historico => historico.ValorContabilResultante)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearAbastecimentos(OwnedNavigationBuilder<Veiculo, Abastecimento> abastecimentos)
    {
        abastecimentos.ToTable("VeiculosAbastecimentos");
        abastecimentos.WithOwner().HasForeignKey("VeiculoId");
        abastecimentos.HasKey(abastecimento => abastecimento.Id);
        abastecimentos.Property(abastecimento => abastecimento.Id)
            .HasConversion(id => id.Value, value => new AbastecimentoId(value))
            .ValueGeneratedNever();
        abastecimentos.Property(abastecimento => abastecimento.Litros).HasColumnType("decimal(18,2)");
        abastecimentos.Property(abastecimento => abastecimento.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        abastecimentos.Property(abastecimento => abastecimento.Odometro)
            .HasConversion(odometro => odometro.Valor, valor => Odometro.De(valor));
        abastecimentos.Property(abastecimento => abastecimento.Horimetro)
            .HasConversion(horimetro => horimetro.Valor, valor => Horimetro.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearOrdensServico(OwnedNavigationBuilder<Veiculo, ManutencaoOS> ordens)
    {
        ordens.ToTable("VeiculosOrdensServico");
        ordens.WithOwner().HasForeignKey("VeiculoId");
        ordens.HasKey(ordem => ordem.Id);
        ordens.Property(ordem => ordem.Id)
            .HasConversion(id => id.Value, value => new ManutencaoOsId(value))
            .ValueGeneratedNever();
        ordens.Property(ordem => ordem.Descricao).HasMaxLength(500);
        ordens.Property(ordem => ordem.Situacao).HasConversion<string>().HasMaxLength(20);
        ordens.Property(ordem => ordem.CustoEstimado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        ordens.Property(ordem => ordem.CustoRealizado)
            .HasConversion(valor => valor!.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        ordens.Property(ordem => ordem.Odometro)
            .HasConversion(odometro => odometro.Valor, valor => Odometro.De(valor));
    }

    private static void MapearMultas(OwnedNavigationBuilder<Veiculo, Multa> multas)
    {
        multas.ToTable("VeiculosMultas");
        multas.WithOwner().HasForeignKey("VeiculoId");
        multas.HasKey(multa => multa.Id);
        multas.Property(multa => multa.Id)
            .HasConversion(id => id.Value, value => new MultaId(value))
            .ValueGeneratedNever();
        multas.Property(multa => multa.CodigoInfracaoCtb).HasMaxLength(20);
        multas.Property(multa => multa.Situacao).HasConversion<string>().HasMaxLength(20);
        multas.Property(multa => multa.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearLicenciamentos(OwnedNavigationBuilder<Veiculo, Licenciamento> licenciamentos)
    {
        licenciamentos.ToTable("VeiculosLicenciamentos");
        licenciamentos.WithOwner().HasForeignKey("VeiculoId");
        licenciamentos.HasKey(licenciamento => licenciamento.Id);
        licenciamentos.Property(licenciamento => licenciamento.Id)
            .HasConversion(id => id.Value, value => new LicenciamentoId(value))
            .ValueGeneratedNever();
        licenciamentos.Property(licenciamento => licenciamento.Situacao).HasConversion<string>().HasMaxLength(20);
        licenciamentos.Property(licenciamento => licenciamento.ValorIpva)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        licenciamentos.Property(licenciamento => licenciamento.ValorTaxa)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }

    private static void MapearMotoristas(OwnedNavigationBuilder<Veiculo, Motorista> motoristas)
    {
        motoristas.ToTable("VeiculosMotoristas");
        motoristas.WithOwner().HasForeignKey("VeiculoId");
        motoristas.HasKey(motorista => motorista.Id);
        motoristas.Property(motorista => motorista.Id)
            .HasConversion(id => id.Value, value => new MotoristaId(value))
            .ValueGeneratedNever();
        motoristas.Property(motorista => motorista.Nome).HasMaxLength(200);
        motoristas.Property(motorista => motorista.Cnh).HasMaxLength(20);
        motoristas.Property(motorista => motorista.CategoriaCnh).HasMaxLength(5);
    }
}
