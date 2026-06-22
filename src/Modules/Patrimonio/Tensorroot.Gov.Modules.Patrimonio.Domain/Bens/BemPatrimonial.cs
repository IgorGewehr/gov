using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>Identificador forte do agregado <see cref="BemPatrimonial"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct BemPatrimonialId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="BemPatrimonialId"/>.</returns>
    public static BemPatrimonialId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Bem público (móvel ou imóvel) gerido em todo o ciclo de vida patrimonial —
/// incorporação, tombamento, movimentação, depreciação linear, reavaliação, impairment
/// e baixa/alienação — com mensuração contábil conforme MCASP/STN e NBC TSP 07.
/// </summary>
public sealed class BemPatrimonial : AggregateRoot<BemPatrimonialId>, IMustHaveTenant
{
    private readonly List<MovimentacaoPatrimonial> _movimentacoes = [];
    private readonly List<HistoricoDepreciacao> _historicosDepreciacao = [];
    private readonly List<Reavaliacao> _reavaliacoes = [];
    private readonly List<Impairment> _impairments = [];

    private BemPatrimonial()
    {
    }

    private BemPatrimonial(
        BemPatrimonialId id,
        Guid tenantId,
        string descricao,
        TipoBem tipo,
        ValorMonetario valorInicial,
        ValorMonetario valorResidual,
        decimal valorTerreno,
        int vidaUtilMeses,
        DateOnly dataIncorporacao,
        string origem)
        : base(id)
    {
        TenantId = tenantId;
        Descricao = descricao;
        Tipo = tipo;
        ValorInicial = valorInicial;
        ValorResidual = valorResidual;
        ValorContabil = valorInicial;
        ValorTerreno = valorTerreno;
        VidaUtilMeses = vidaUtilMeses;
        DataIncorporacao = dataIncorporacao;
        EmCondicoesDeUso = false;
        Situacao = SituacaoBemPatrimonial.EmIncorporacao;
        RaiseDomainEvent(new BemIncorporado(id, valorInicial, origem));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Número de tombo; nulo antes de <see cref="Tombar"/>.</summary>
    public NumeroTombamento? NumeroTombamento { get; private set; }

    /// <summary>Descrição do bem.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Tipo do bem (móvel ou imóvel).</summary>
    public TipoBem Tipo { get; private set; }

    /// <summary>Valor de incorporação (custo de ingresso).</summary>
    public ValorMonetario ValorInicial { get; private set; } = default!;

    /// <summary>Resíduo estimado ao fim da vida útil.</summary>
    public ValorMonetario ValorResidual { get; private set; } = default!;

    /// <summary>Valor líquido atual do bem (nunca inferior ao residual).</summary>
    public ValorMonetario ValorContabil { get; private set; } = default!;

    /// <summary>Parcela do valor correspondente ao terreno (imóvel), que não deprecia.</summary>
    public decimal ValorTerreno { get; private set; }

    /// <summary>Vida útil em meses.</summary>
    public int VidaUtilMeses { get; private set; }

    /// <summary>Data de ingresso ao acervo.</summary>
    public DateOnly DataIncorporacao { get; private set; }

    /// <summary>Indica se o bem está em condições de uso (início da depreciação).</summary>
    public bool EmCondicoesDeUso { get; private set; }

    /// <summary>Situação atual no ciclo de vida patrimonial.</summary>
    public SituacaoBemPatrimonial Situacao { get; private set; }

    /// <summary>Transferências de localização/responsável.</summary>
    public IReadOnlyCollection<MovimentacaoPatrimonial> Movimentacoes => _movimentacoes;

    /// <summary>Depreciação reconhecida por competência.</summary>
    public IReadOnlyCollection<HistoricoDepreciacao> HistoricosDepreciacao => _historicosDepreciacao;

    /// <summary>Reavaliações a valor justo.</summary>
    public IReadOnlyCollection<Reavaliacao> Reavaliacoes => _reavaliacoes;

    /// <summary>Perdas por recuperabilidade (impairment).</summary>
    public IReadOnlyCollection<Impairment> Impairments => _impairments;

    /// <summary>Valor depreciável corrente (parte não-terreno acima do residual).</summary>
    public ValorMonetario ValorDepreciavel => ValorContabil.Subtrair(ValorResidual);

    /// <summary>Parcela mensal linear de depreciação.</summary>
    public decimal ParcelaMensal
        => Depreciacao.De(ValorInicial.Valor - ValorTerreno, ValorResidual.Valor, VidaUtilMeses).ParcelaMensal;

    /// <summary>Incorpora um bem ao acervo (situação inicial <see cref="SituacaoBemPatrimonial.EmIncorporacao"/>).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="descricao">Descrição do bem.</param>
    /// <param name="tipo">Tipo (móvel ou imóvel).</param>
    /// <param name="valorInicial">Valor de incorporação.</param>
    /// <param name="valorResidual">Resíduo estimado.</param>
    /// <param name="vidaUtilMeses">Vida útil em meses (maior que zero).</param>
    /// <param name="dataIncorporacao">Data de ingresso.</param>
    /// <param name="origem">Origem do ingresso.</param>
    /// <param name="valorTerreno">Parcela do terreno (imóvel) que não deprecia; zero para móvel.</param>
    /// <returns>Novo <see cref="BemPatrimonial"/>.</returns>
    /// <exception cref="ArgumentException">Se a descrição/origem for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se valores ou vida útil forem inválidos.</exception>
    public static BemPatrimonial Incorporar(
        Guid tenantId,
        string descricao,
        TipoBem tipo,
        ValorMonetario valorInicial,
        ValorMonetario valorResidual,
        int vidaUtilMeses,
        DateOnly dataIncorporacao,
        string origem,
        decimal valorTerreno = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(origem);
        ArgumentNullException.ThrowIfNull(valorInicial);
        ArgumentNullException.ThrowIfNull(valorResidual);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaUtilMeses);
        ArgumentOutOfRangeException.ThrowIfNegative(valorTerreno);
        if (valorResidual.MaiorQue(valorInicial))
        {
            throw new ArgumentOutOfRangeException(nameof(valorResidual), "Valor residual não pode exceder o valor inicial.");
        }

        if (valorTerreno > valorInicial.Valor)
        {
            throw new ArgumentOutOfRangeException(nameof(valorTerreno), "Valor do terreno não pode exceder o valor inicial.");
        }

        return new BemPatrimonial(
            BemPatrimonialId.New(),
            tenantId,
            descricao,
            tipo,
            valorInicial,
            valorResidual,
            valorTerreno,
            vidaUtilMeses,
            dataIncorporacao,
            origem);
    }

    /// <summary>Marca o bem como em condições de uso, iniciando a depreciação.</summary>
    /// <exception cref="InvalidOperationException">Se o bem estiver encerrado.</exception>
    public void ColocarEmCondicoesDeUso()
    {
        GarantirNaoEncerrado();
        EmCondicoesDeUso = true;
    }

    /// <summary>Tomba o bem, atribuindo um número de tombo único por tenant.</summary>
    /// <param name="numeroTombamento">Número de tombamento.</param>
    /// <exception cref="ArgumentException">Se o número de tombo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situação não for <see cref="SituacaoBemPatrimonial.EmIncorporacao"/>.</exception>
    public void Tombar(string numeroTombamento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroTombamento);
        if (Situacao != SituacaoBemPatrimonial.EmIncorporacao)
        {
            throw new InvalidOperationException($"O tombamento só pode ocorrer a partir de EmIncorporacao. Situação atual: {Situacao}.");
        }

        var tombo = ValueObjects.NumeroTombamento.De(numeroTombamento);
        NumeroTombamento = tombo;
        Situacao = SituacaoBemPatrimonial.Tombado;
        RaiseDomainEvent(new BemTombado(Id, tombo.Valor));
    }

    /// <summary>Reconhece a depreciação linear da competência (terreno não deprecia; limita-se ao residual).</summary>
    /// <param name="competencia">Competência (mês/ano) do reconhecimento.</param>
    /// <returns>Valor efetivamente depreciado na competência.</returns>
    /// <exception cref="InvalidOperationException">Se o bem não estiver <see cref="SituacaoBemPatrimonial.Tombado"/>.</exception>
    public decimal Depreciar(DateOnly competencia)
    {
        GarantirNaoEncerrado();
        if (Situacao != SituacaoBemPatrimonial.Tombado)
        {
            throw new InvalidOperationException($"A depreciação só ocorre para bem Tombado. Situação atual: {Situacao}.");
        }

        // I-2: só deprecia quando em condições de uso.
        if (!EmCondicoesDeUso)
        {
            return 0m;
        }

        // I-3: terreno não deprecia; o residual já delimita o piso (I-4).
        var depreciavel = ValorContabil.Subtrair(ValorResidual);
        if (depreciavel.Valor <= 0m)
        {
            return 0m;
        }

        var parcela = Math.Min(ParcelaMensal, depreciavel.Valor);
        if (parcela <= 0m)
        {
            return 0m;
        }

        ValorContabil = ValorContabil.Subtrair(ValorMonetario.De(parcela));
        _historicosDepreciacao.Add(HistoricoDepreciacao.Registrar(competencia, parcela, ValorContabil));
        RaiseDomainEvent(new BemDepreciado(Id, parcela, competencia));
        return parcela;
    }

    /// <summary>Reavalia o bem ao valor justo informado (somente ativo no acervo).</summary>
    /// <param name="novoValorJusto">Novo valor justo.</param>
    /// <param name="laudoUri">Referência (URI) do laudo (obrigatório).</param>
    /// <param name="data">Data da reavaliação.</param>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    /// <exception cref="InvalidOperationException">Se o bem não estiver ativo no acervo.</exception>
    public void Reavaliar(decimal novoValorJusto, string laudoUri, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        GarantirAtivoNoAcervo();

        var novoValor = ValorMonetario.De(novoValorJusto);
        ValorContabil = novoValor;
        _reavaliacoes.Add(Reavaliacao.Registrar(data, novoValorJusto, laudoUri));
        RaiseDomainEvent(new BemReavaliado(Id, novoValorJusto));
    }

    /// <summary>Reconhece perda por impairment quando o valor recuperável é inferior ao contábil.</summary>
    /// <param name="valorRecuperavel">Valor recuperável apurado.</param>
    /// <param name="laudoUri">Referência (URI) do laudo/teste (obrigatório).</param>
    /// <param name="data">Data do teste de recuperabilidade.</param>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    /// <exception cref="InvalidOperationException">Se o bem não estiver ativo no acervo ou o recuperável não for inferior ao contábil.</exception>
    public void RegistrarImpairment(decimal valorRecuperavel, string laudoUri, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        GarantirAtivoNoAcervo();

        var recuperavel = ValorMonetario.De(valorRecuperavel);
        // I-6: só reconhece se recuperável < contábil.
        if (!recuperavel.MenorQue(ValorContabil))
        {
            throw new InvalidOperationException("Impairment requer valor recuperável inferior ao valor contábil.");
        }

        var perda = ValorContabil.Valor - recuperavel.Valor;
        ValorContabil = recuperavel;
        _impairments.Add(Impairment.Registrar(data, valorRecuperavel, perda, laudoUri));
        RaiseDomainEvent(new BemReavaliado(Id, valorRecuperavel));
    }

    /// <summary>Transfere o bem para nova localização/responsável (somente ativo no acervo).</summary>
    /// <param name="localizacaoDestino">Localização de destino.</param>
    /// <param name="responsavelDestinoId">Responsável de destino.</param>
    /// <param name="data">Data da transferência.</param>
    /// <param name="localizacaoOrigem">Localização de origem (opcional).</param>
    /// <exception cref="InvalidOperationException">Se o bem não estiver ativo no acervo.</exception>
    public void Transferir(string localizacaoDestino, Guid responsavelDestinoId, DateOnly data, string? localizacaoOrigem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localizacaoDestino);
        GarantirAtivoNoAcervo();
        _movimentacoes.Add(MovimentacaoPatrimonial.Registrar(
            localizacaoOrigem ?? string.Empty,
            localizacaoDestino,
            responsavelDestinoId,
            data));
    }

    /// <summary>Cede o bem em cessão/comodato a terceiro, mantendo-o no acervo (sem baixa contábil).</summary>
    /// <exception cref="InvalidOperationException">Se a situação não for <see cref="SituacaoBemPatrimonial.Tombado"/>.</exception>
    public void Ceder()
    {
        if (Situacao != SituacaoBemPatrimonial.Tombado)
        {
            throw new InvalidOperationException($"A cessão/comodato só ocorre para bem Tombado. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoBemPatrimonial.Cedido;
    }

    /// <summary>Baixa o bem do acervo (exige laudo e autorização).</summary>
    /// <param name="motivoBaixa">Motivo da baixa.</param>
    /// <param name="laudoUri">Referência (URI) do laudo/parecer (obrigatório).</param>
    /// <param name="autorizacaoId">Identificador da autorização (obrigatório).</param>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a autorização não for informada.</exception>
    /// <exception cref="InvalidOperationException">Se o bem estiver encerrado.</exception>
    public void Baixar(string motivoBaixa, string laudoUri, Guid autorizacaoId)
    {
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoBaixa);
        // I-7: sem laudo/autorização, a baixa é rejeitada e nenhum lançamento é emitido.
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        if (autorizacaoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(autorizacaoId), "Autorização é obrigatória para baixa.");
        }

        Situacao = SituacaoBemPatrimonial.Baixada;
        RaiseDomainEvent(new BemBaixado(Id, motivoBaixa, ValorContabil));
    }

    /// <summary>Aliena o bem (exige avaliação prévia; em regra, por leilão — Lei 14.133 art. 31/76).</summary>
    /// <param name="avaliacaoPreviaId">Identificador da avaliação prévia (obrigatório).</param>
    /// <param name="porLeilao">Indica se a alienação foi por leilão.</param>
    /// <param name="valorAlienacao">Valor da alienação.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a avaliação prévia não for informada.</exception>
    /// <exception cref="InvalidOperationException">Se o bem estiver encerrado.</exception>
    public void Alienar(Guid avaliacaoPreviaId, bool porLeilao, decimal valorAlienacao)
    {
        GarantirNaoEncerrado();
        ArgumentOutOfRangeException.ThrowIfNegative(valorAlienacao);
        // I-9: sem avaliação prévia, a alienação é rejeitada.
        if (avaliacaoPreviaId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(avaliacaoPreviaId), "Avaliação prévia é obrigatória para alienação.");
        }

        _ = porLeilao;
        Situacao = SituacaoBemPatrimonial.Alienada;
        RaiseDomainEvent(new BemBaixado(Id, "Alienacao", ValorContabil));
    }

    private void GarantirNaoEncerrado()
    {
        if (Situacao is SituacaoBemPatrimonial.Baixada or SituacaoBemPatrimonial.Alienada)
        {
            throw new InvalidOperationException($"Bem encerrado não admite novas transições. Situação atual: {Situacao}.");
        }
    }

    private void GarantirAtivoNoAcervo()
    {
        if (Situacao is not (SituacaoBemPatrimonial.Tombado or SituacaoBemPatrimonial.Cedido))
        {
            throw new InvalidOperationException($"Operação exige bem ativo no acervo (Tombado/Cedido). Situação atual: {Situacao}.");
        }
    }
}
