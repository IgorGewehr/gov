using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte do agregado <see cref="Pneu"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PneuId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PneuId"/>.</returns>
    public static PneuId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Pneu da frota como item controlado individualmente (raiz de agregado próprio): tem ciclo de vida
/// independente do veículo — entra em estoque, é instalado numa posição (eixo/lado) de um veículo,
/// roda acumulando km, é removido/reposicionado em rodízio, pode ser recapado e por fim é descartado
/// ao atingir o sulco mínimo legal ou o limite de km. Controla o desgaste por sulco (mm), a vida útil
/// por sulco e por km, o número de recapagens e o custo. É o controle típico de pneus da frota pública
/// (gestão de frota; CONTRAN/CTB para o sulco mínimo de circulação, parametrizável por tenant).
/// </summary>
public sealed class Pneu : AggregateRoot<PneuId>, IMustHaveTenant
{
    private Pneu()
    {
    }

    private Pneu(
        PneuId id,
        Guid tenantId,
        string numeroFogo,
        string marca,
        string modelo,
        string medida,
        string? dot,
        Sulco sulcoNovo,
        int vidaUtilKmEstimada,
        ValorMonetario valorAquisicao,
        DateOnly dataAquisicao)
        : base(id)
    {
        TenantId = tenantId;
        NumeroFogo = numeroFogo;
        Marca = marca;
        Modelo = modelo;
        Medida = medida;
        Dot = dot;
        SulcoNovo = sulcoNovo;
        SulcoAtual = sulcoNovo;
        VidaUtilKmEstimada = vidaUtilKmEstimada;
        ValorAquisicao = valorAquisicao;
        DataAquisicao = dataAquisicao;
        Situacao = SituacaoPneu.EmEstoque;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Número de fogo (identificação única gravada/atribuída ao pneu pela frota), chave operacional de
    /// rastreio do pneu individual. Único por tenant.
    /// </summary>
    public string NumeroFogo { get; private set; } = default!;

    /// <summary>Marca do pneu (ex.: Pirelli, Michelin).</summary>
    public string Marca { get; private set; } = default!;

    /// <summary>Modelo/desenho do pneu.</summary>
    public string Modelo { get; private set; } = default!;

    /// <summary>Medida do pneu (ex.: "275/80 R22.5"), padrão da indústria.</summary>
    public string Medida { get; private set; } = default!;

    /// <summary>Código DOT (semana/ano de fabricação), quando informado.</summary>
    public string? Dot { get; private set; }

    /// <summary>Sulco de fábrica (novo) — referência de 100% de banda de rodagem.</summary>
    public Sulco SulcoNovo { get; private set; }

    /// <summary>Sulco atual aferido (mm); decresce com a rodagem e é restaurado pela recapagem.</summary>
    public Sulco SulcoAtual { get; private set; }

    /// <summary>Vida útil estimada por quilometragem (km), parâmetro de planejamento da troca.</summary>
    public int VidaUtilKmEstimada { get; private set; }

    /// <summary>Valor de aquisição do pneu (custo de ingresso).</summary>
    public ValorMonetario ValorAquisicao { get; private set; } = default!;

    /// <summary>Custo acumulado de recapagens reformas aplicadas ao pneu.</summary>
    public ValorMonetario CustoRecapagens { get; private set; } = ValorMonetario.Zero;

    /// <summary>Data de aquisição do pneu.</summary>
    public DateOnly DataAquisicao { get; private set; }

    /// <summary>Situação atual do pneu no seu ciclo de vida.</summary>
    public SituacaoPneu Situacao { get; private set; }

    /// <summary>Veículo onde o pneu está instalado atualmente (nulo se não instalado).</summary>
    public VeiculoId? VeiculoAtualId { get; private set; }

    /// <summary>Posição (eixo/lado) onde o pneu está montado atualmente (nula se não instalado).</summary>
    public PosicaoPneu? PosicaoAtual { get; private set; }

    /// <summary>Odômetro do veículo no momento da instalação corrente (km); base do km do ciclo.</summary>
    public int? OdometroInstalacao { get; private set; }

    /// <summary>Quilômetros totais já rodados pelo pneu (somatório dos ciclos encerrados).</summary>
    public int KmAcumulado { get; private set; }

    /// <summary>Número de recapagens (reformas da banda) já aplicadas ao pneu.</summary>
    public int Recapagens { get; private set; }

    /// <summary>Indica se o pneu está instalado em um veículo (em rodagem).</summary>
    public bool EstaInstalado => Situacao == SituacaoPneu.Instalado;

    /// <summary>Indica se o pneu foi descartado (estado terminal).</summary>
    public bool EstaDescartado => Situacao == SituacaoPneu.Descartado;

    /// <summary>
    /// Custo total do pneu (aquisição + recapagens). Base do custo por quilômetro do pneu.
    /// </summary>
    public decimal CustoTotal => ValorAquisicao.Valor + CustoRecapagens.Valor;

    /// <summary>
    /// Custo por quilômetro rodado do pneu (custo total ÷ km acumulado); nulo enquanto não houver
    /// quilometragem rodada (indeterminável). Indicador de economicidade na gestão de pneus.
    /// </summary>
    public decimal? CustoPorKm => KmAcumulado > 0
        ? decimal.Round(CustoTotal / KmAcumulado, 4, MidpointRounding.AwayFromZero)
        : null;

    /// <summary>
    /// Percentual de banda de rodagem remanescente (sulco atual ÷ sulco novo), de 0 a 100. Proxy de
    /// vida útil por desgaste do sulco. Cem por cento quando recém-recapado/novo.
    /// </summary>
    public decimal PercentualBandaRemanescente => SulcoNovo.Milimetros <= 0m
        ? 0m
        : decimal.Round(SulcoAtual.Milimetros / SulcoNovo.Milimetros * 100m, 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Cadastra um novo pneu, ingressando-o em estoque (<see cref="SituacaoPneu.EmEstoque"/>).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="numeroFogo">Número de fogo (identificação única do pneu na frota).</param>
    /// <param name="marca">Marca do pneu.</param>
    /// <param name="modelo">Modelo/desenho do pneu.</param>
    /// <param name="medida">Medida do pneu (padrão da indústria).</param>
    /// <param name="dot">Código DOT (semana/ano de fabricação), opcional.</param>
    /// <param name="sulcoNovoMilimetros">Sulco de fábrica (mm), positivo.</param>
    /// <param name="vidaUtilKmEstimada">Vida útil estimada por km, positiva.</param>
    /// <param name="valorAquisicao">Valor de aquisição.</param>
    /// <param name="dataAquisicao">Data de aquisição.</param>
    /// <returns>Novo <see cref="Pneu"/> em estoque.</returns>
    /// <exception cref="ArgumentException">Se número de fogo/marca/modelo/medida forem vazios.</exception>
    /// <exception cref="ArgumentNullException">Se o valor de aquisição for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o sulco novo ou a vida útil não forem positivos.</exception>
    public static Pneu Cadastrar(
        Guid tenantId,
        string numeroFogo,
        string marca,
        string modelo,
        string medida,
        string? dot,
        decimal sulcoNovoMilimetros,
        int vidaUtilKmEstimada,
        ValorMonetario valorAquisicao,
        DateOnly dataAquisicao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroFogo);
        ArgumentException.ThrowIfNullOrWhiteSpace(marca);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelo);
        ArgumentException.ThrowIfNullOrWhiteSpace(medida);
        ArgumentNullException.ThrowIfNull(valorAquisicao);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sulcoNovoMilimetros);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaUtilKmEstimada);

        return new Pneu(
            PneuId.New(),
            tenantId,
            numeroFogo,
            marca,
            modelo,
            medida,
            dot,
            Sulco.De(sulcoNovoMilimetros),
            vidaUtilKmEstimada,
            valorAquisicao,
            dataAquisicao);
    }

    /// <summary>
    /// Instala o pneu numa posição (eixo/lado) de um veículo. Exige o pneu em estoque (não instalado
    /// nem descartado). Registra o odômetro de instalação para apurar o km do ciclo na remoção.
    /// </summary>
    /// <param name="veiculoId">Veículo onde instalar.</param>
    /// <param name="posicao">Posição de montagem (eixo/lado).</param>
    /// <param name="odometroVeiculo">Odômetro atual do veículo (km), não-negativo.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o odômetro for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o pneu não estiver em estoque.</exception>
    public void Instalar(VeiculoId veiculoId, PosicaoPneu posicao, int odometroVeiculo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(odometroVeiculo);
        if (Situacao != SituacaoPneu.EmEstoque)
        {
            throw new InvalidOperationException(
                $"A instalação exige pneu em estoque. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoPneu.Instalado;
        VeiculoAtualId = veiculoId;
        PosicaoAtual = posicao;
        OdometroInstalacao = odometroVeiculo;

        RaiseDomainEvent(new PneuInstalado(Id, veiculoId, posicao, odometroVeiculo));
    }

    /// <summary>
    /// Remove o pneu do veículo (rodízio/reposicionamento/manutenção), encerrando o ciclo de instalação.
    /// Acumula os km rodados no ciclo (odômetro de remoção − odômetro de instalação) e afere o sulco
    /// remanescente. O pneu volta a <see cref="SituacaoPneu.Removido"/>, pronto para reinstalação, recapagem
    /// ou descarte. Para reposicionar de um veículo para outro (ou outra posição), remova e instale.
    /// </summary>
    /// <param name="odometroVeiculo">Odômetro do veículo na remoção (km); ≥ odômetro de instalação.</param>
    /// <param name="sulcoAferidoMilimetros">Sulco aferido na remoção (mm); ≤ sulco atual (desgaste).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o odômetro for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o pneu não estiver instalado ou o odômetro retroceder.</exception>
    public void Remover(int odometroVeiculo, decimal sulcoAferidoMilimetros)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(odometroVeiculo);
        if (Situacao != SituacaoPneu.Instalado || VeiculoAtualId is not { } veiculoId || OdometroInstalacao is not { } odometroInstalacao)
        {
            throw new InvalidOperationException(
                $"A remoção exige pneu instalado. Situação atual: {Situacao}.");
        }

        if (odometroVeiculo < odometroInstalacao)
        {
            throw new InvalidOperationException(
                $"Odômetro de remoção ({odometroVeiculo} km) não pode ser inferior ao de instalação ({odometroInstalacao} km).");
        }

        var kmCiclo = odometroVeiculo - odometroInstalacao;
        KmAcumulado += kmCiclo;
        SulcoAtual = SulcoAtual.Desgastar(sulcoAferidoMilimetros);

        Situacao = SituacaoPneu.Removido;
        VeiculoAtualId = null;
        PosicaoAtual = null;
        OdometroInstalacao = null;

        RaiseDomainEvent(new PneuRemovido(Id, veiculoId, kmCiclo));
    }

    /// <summary>
    /// Retorna o pneu (removido) ao estoque, pronto para nova instalação/rodízio. Reposicionamento =
    /// remover e instalar novamente noutra posição/veículo.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o pneu não estiver removido.</exception>
    public void RetornarAoEstoque()
    {
        if (Situacao != SituacaoPneu.Removido)
        {
            throw new InvalidOperationException(
                $"Só um pneu removido retorna ao estoque. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoPneu.EmEstoque;
    }

    /// <summary>
    /// Envia o pneu (removido) para recapagem. Estado intermediário até a conclusão (<see cref="ConcluirRecapagem"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o pneu não estiver removido.</exception>
    public void EnviarParaRecapagem()
    {
        if (Situacao != SituacaoPneu.Removido)
        {
            throw new InvalidOperationException(
                $"Só um pneu removido pode ir para recapagem. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoPneu.EmRecapagem;
    }

    /// <summary>
    /// Conclui a recapagem do pneu: restaura o sulco (nova banda de rodagem), acresce o custo da reforma
    /// e devolve o pneu ao estoque. A recapagem é a única operação que aumenta o sulco do pneu.
    /// </summary>
    /// <param name="sulcoRecapadoMilimetros">Sulco restaurado pela recapagem (mm), positivo.</param>
    /// <param name="custoRecapagem">Custo da recapagem.</param>
    /// <exception cref="ArgumentNullException">Se o custo for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o sulco recapado não for positivo.</exception>
    /// <exception cref="InvalidOperationException">Se o pneu não estiver em recapagem.</exception>
    public void ConcluirRecapagem(decimal sulcoRecapadoMilimetros, ValorMonetario custoRecapagem)
    {
        ArgumentNullException.ThrowIfNull(custoRecapagem);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sulcoRecapadoMilimetros);
        if (Situacao != SituacaoPneu.EmRecapagem)
        {
            throw new InvalidOperationException(
                $"A conclusão de recapagem exige pneu em recapagem. Situação atual: {Situacao}.");
        }

        // Recapagem restaura a banda de rodagem: o sulco recapado torna-se a nova referência de "novo".
        SulcoNovo = Sulco.De(sulcoRecapadoMilimetros);
        SulcoAtual = SulcoNovo;
        CustoRecapagens = CustoRecapagens.Somar(custoRecapagem);
        Recapagens++;
        Situacao = SituacaoPneu.EmEstoque;
    }

    /// <summary>
    /// Descarta/sucateia o pneu (fim de vida útil por sulco mínimo, idade ou avaria irreparável),
    /// estado terminal. O pneu deve estar fora do veículo (em estoque ou removido). Após o descarte o
    /// pneu não admite novas operações.
    /// </summary>
    /// <param name="motivo">Motivo do descarte.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o pneu estiver instalado ou já descartado.</exception>
    public void Descartar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is SituacaoPneu.Instalado)
        {
            throw new InvalidOperationException("Remova o pneu do veículo antes de descartá-lo.");
        }

        if (Situacao is SituacaoPneu.Descartado)
        {
            throw new InvalidOperationException("Pneu já descartado.");
        }

        Situacao = SituacaoPneu.Descartado;
        VeiculoAtualId = null;
        PosicaoAtual = null;
        OdometroInstalacao = null;

        RaiseDomainEvent(new PneuDescartado(Id, motivo, KmAcumulado));
    }
}
