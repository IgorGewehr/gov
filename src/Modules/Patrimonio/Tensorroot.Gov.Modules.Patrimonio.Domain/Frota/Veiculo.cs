using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte do agregado <see cref="Veiculo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VeiculoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VeiculoId"/>.</returns>
    public static VeiculoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Veículo da frota pública. É-um bem patrimonial (controla tombamento, valor contábil,
/// vida útil e situação no ciclo patrimonial) e acresce a gestão de frota conforme o
/// CTB (Lei 9.503/1997): placa/RENAVAM, abastecimento sob cota, manutenção (ordem de
/// serviço), multas, licenciamento/IPVA e motorista (CNH). Raiz de agregado.
/// </summary>
public sealed partial class Veiculo : AggregateRoot<VeiculoId>, IMustHaveTenant
{
    private readonly List<Abastecimento> _abastecimentos = [];
    private readonly List<ManutencaoOS> _ordensServico = [];
    private readonly List<Multa> _multas = [];
    private readonly List<Licenciamento> _licenciamentos = [];
    private readonly List<Motorista> _motoristas = [];
    private readonly List<HistoricoDepreciacao> _historicosDepreciacao = [];

    private Veiculo()
    {
    }

    private Veiculo(
        VeiculoId id,
        Guid tenantId,
        string descricao,
        ValorMonetario valorInicial,
        ValorMonetario valorResidual,
        int vidaUtilMeses,
        DateOnly dataIncorporacao,
        string origem,
        Placa placa,
        Renavam renavam,
        Odometro odometro,
        Horimetro horimetro)
        : base(id)
    {
        TenantId = tenantId;
        Descricao = descricao;
        ValorInicial = valorInicial;
        ValorResidual = valorResidual;
        ValorContabil = valorInicial;
        VidaUtilMeses = vidaUtilMeses;
        DataIncorporacao = dataIncorporacao;
        Origem = origem;
        Placa = placa;
        Renavam = renavam;
        Odometro = odometro;
        Horimetro = horimetro;
        EmCondicoesDeUso = true;
        Situacao = SituacaoBemPatrimonial.EmIncorporacao;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Descrição do veículo.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Número de tombamento, quando tombado.</summary>
    public string? NumeroTombamento { get; private set; }

    /// <summary>Valor de incorporação (custo de ingresso).</summary>
    public ValorMonetario ValorInicial { get; private set; } = default!;

    /// <summary>Valor residual (piso da depreciação).</summary>
    public ValorMonetario ValorResidual { get; private set; } = default!;

    /// <summary>Valor contábil atual.</summary>
    public ValorMonetario ValorContabil { get; private set; } = default!;

    /// <summary>Vida útil estimada, em meses.</summary>
    public int VidaUtilMeses { get; private set; }

    /// <summary>Data de incorporação ao acervo.</summary>
    public DateOnly DataIncorporacao { get; private set; }

    /// <summary>Origem do ingresso (aquisição, doação, produção própria).</summary>
    public string Origem { get; private set; } = default!;

    /// <summary>Indica se o veículo está em condições de uso (apto a depreciar).</summary>
    public bool EmCondicoesDeUso { get; private set; }

    /// <summary>Situação do veículo no ciclo patrimonial.</summary>
    public SituacaoBemPatrimonial Situacao { get; private set; }

    /// <summary>Placa do veículo (CTB).</summary>
    public Placa Placa { get; private set; }

    /// <summary>RENAVAM do veículo (CTB).</summary>
    public Renavam Renavam { get; private set; }

    /// <summary>Quilometragem atual (monotônica não decrescente).</summary>
    public Odometro Odometro { get; private set; }

    /// <summary>Horas de uso atuais (monotônica não decrescente).</summary>
    public Horimetro Horimetro { get; private set; }

    /// <summary>Motorista atualmente designado (nulo se sem condutor).</summary>
    public Guid? MotoristaAtualId { get; private set; }

    /// <summary>Histórico de abastecimentos.</summary>
    public IReadOnlyCollection<Abastecimento> Abastecimentos => _abastecimentos.AsReadOnly();

    /// <summary>Ordens de serviço de manutenção.</summary>
    public IReadOnlyCollection<ManutencaoOS> OrdensServico => _ordensServico.AsReadOnly();

    /// <summary>Multas registradas.</summary>
    public IReadOnlyCollection<Multa> Multas => _multas.AsReadOnly();

    /// <summary>Licenciamentos/IPVA por exercício.</summary>
    public IReadOnlyCollection<Licenciamento> Licenciamentos => _licenciamentos.AsReadOnly();

    /// <summary>Condutores vinculados (CNH).</summary>
    public IReadOnlyCollection<Motorista> Motoristas => _motoristas.AsReadOnly();

    /// <summary>Depreciação reconhecida por competência (BUG-P4).</summary>
    public IReadOnlyCollection<HistoricoDepreciacao> HistoricosDepreciacao => _historicosDepreciacao.AsReadOnly();

    /// <summary>Indica se o veículo está ativo no acervo (apto a operações de frota, I-5).</summary>
    public bool AtivoNoAcervo => Situacao is SituacaoBemPatrimonial.Tombado or SituacaoBemPatrimonial.Cedido;

    /// <summary>Número de competências já depreciadas (BUG-P4).</summary>
    public int CompetenciasDepreciadas => _historicosDepreciacao.Count;

    /// <summary>Meses remanescentes de vida útil (mínimo zero) — BUG-P4.</summary>
    public int VidaUtilRemanescenteMeses => Math.Max(0, VidaUtilMeses - CompetenciasDepreciadas);

    /// <summary>
    /// Parcela mensal linear de depreciação do veículo (MCASP / NBC TSP 07; BUG-P4): valor contábil
    /// corrente menos residual, distribuído pela vida útil remanescente. Piso no residual.
    /// </summary>
    public decimal ParcelaMensalDepreciacao
    {
        get
        {
            var remanescente = VidaUtilRemanescenteMeses;
            if (remanescente <= 0)
            {
                return 0m;
            }

            return Depreciacao.De(ValorContabil.Valor, ValorResidual.Valor, remanescente).ParcelaMensal;
        }
    }

    /// <summary>Incorpora um novo veículo ao acervo da frota (estado inicial <see cref="SituacaoBemPatrimonial.EmIncorporacao"/>).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="descricao">Descrição do veículo.</param>
    /// <param name="valorInicial">Valor de incorporação.</param>
    /// <param name="valorResidual">Valor residual (não pode exceder o inicial).</param>
    /// <param name="vidaUtilMeses">Vida útil estimada, em meses (positivo).</param>
    /// <param name="dataIncorporacao">Data de incorporação.</param>
    /// <param name="origem">Origem do ingresso.</param>
    /// <param name="placa">Placa válida (CTB).</param>
    /// <param name="renavam">RENAVAM válido (CTB).</param>
    /// <param name="odometroInicial">Quilometragem inicial.</param>
    /// <param name="horimetroInicial">Horas de uso iniciais.</param>
    /// <returns>Novo <see cref="Veiculo"/> em incorporação.</returns>
    /// <exception cref="ArgumentException">Se a descrição for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a vida útil não for positiva ou o residual exceder o inicial.</exception>
    public static Veiculo IncorporarVeiculo(
        Guid tenantId,
        string descricao,
        ValorMonetario valorInicial,
        ValorMonetario valorResidual,
        int vidaUtilMeses,
        DateOnly dataIncorporacao,
        string origem,
        Placa placa,
        Renavam renavam,
        Odometro odometroInicial,
        Horimetro horimetroInicial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(origem);
        ArgumentNullException.ThrowIfNull(valorInicial);
        ArgumentNullException.ThrowIfNull(valorResidual);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaUtilMeses);
        if (valorResidual.MaiorQue(valorInicial))
        {
            throw new ArgumentOutOfRangeException(nameof(valorResidual), "Valor residual não pode exceder o inicial.");
        }

        var veiculo = new Veiculo(
            VeiculoId.New(),
            tenantId,
            descricao,
            valorInicial,
            valorResidual,
            vidaUtilMeses,
            dataIncorporacao,
            origem,
            placa,
            renavam,
            odometroInicial,
            horimetroInicial);

        return veiculo;
    }

    /// <summary>Tomba o veículo, atribuindo número de tombo único e ativando-o no acervo.</summary>
    /// <param name="numeroTombamento">Número de tombamento.</param>
    /// <exception cref="ArgumentException">Se o número de tombamento for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o veículo não estiver em incorporação.</exception>
    public void Tombar(string numeroTombamento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroTombamento);
        if (Situacao != SituacaoBemPatrimonial.EmIncorporacao)
        {
            throw new InvalidOperationException(
                $"O tombamento exige veículo em incorporação. Situação atual: {Situacao}.");
        }

        NumeroTombamento = numeroTombamento;
        Situacao = SituacaoBemPatrimonial.Tombado;
    }

    /// <summary>Registra um abastecimento, validando cota e medições (I-3/I-4/I-5/I-6/I-10).</summary>
    /// <param name="data">Data do abastecimento.</param>
    /// <param name="litros">Litros abastecidos (positivo).</param>
    /// <param name="valor">Valor do abastecimento.</param>
    /// <param name="odometro">Leitura do odômetro (maior ou igual ao atual).</param>
    /// <param name="horimetro">Leitura do horímetro (maior ou igual ao atual).</param>
    /// <param name="cotaLitros">Cota vigente de litros para o abastecimento (I-6).</param>
    /// <param name="motoristaId">Motorista, quando informado.</param>
    /// <exception cref="ArgumentNullException">Se o valor for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se encerrado, fora da cota ou medição retroativa.</exception>
    public void RegistrarAbastecimento(
        DateOnly data,
        decimal litros,
        ValorMonetario valor,
        int odometro,
        decimal horimetro,
        decimal cotaLitros,
        Guid? motoristaId)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirAtivoNoAcervo();
        if (litros > cotaLitros)
        {
            throw new InvalidOperationException(
                $"Abastecimento de {litros} L excede a cota vigente de {cotaLitros} L.");
        }

        var novoOdometro = Odometro.Avancar(odometro);
        var novoHorimetro = Horimetro.Avancar(horimetro);

        var abastecimento = Abastecimento.Registrar(data, litros, valor, novoOdometro, novoHorimetro, motoristaId);
        _abastecimentos.Add(abastecimento);
        Odometro = novoOdometro;
        Horimetro = novoHorimetro;

        RaiseDomainEvent(new AbastecimentoRegistrado(Id, litros, valor.Valor, data));
    }

    /// <summary>Abre uma ordem de serviço de manutenção (I-3/I-5).</summary>
    /// <param name="descricao">Descrição do serviço.</param>
    /// <param name="custoEstimado">Custo estimado.</param>
    /// <param name="odometro">Leitura do odômetro (maior ou igual ao atual).</param>
    /// <returns>Identificador da ordem de serviço aberta.</returns>
    /// <exception cref="ArgumentNullException">Se o custo estimado for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se encerrado ou odômetro retroativo.</exception>
    public ManutencaoOsId AbrirOrdemServico(string descricao, ValorMonetario custoEstimado, int odometro)
    {
        ArgumentNullException.ThrowIfNull(custoEstimado);
        GarantirAtivoNoAcervo();
        var leitura = Odometro.Avancar(odometro);
        Odometro = leitura;

        var ordem = ManutencaoOS.Abrir(descricao, custoEstimado, leitura);
        _ordensServico.Add(ordem);
        return ordem.Id;
    }

    /// <summary>Conclui uma ordem de serviço de manutenção (I-7).</summary>
    /// <param name="ordemServicoId">Identificador da ordem de serviço.</param>
    /// <param name="custoRealizado">Custo realizado.</param>
    /// <param name="dataConclusao">Data de conclusão.</param>
    /// <exception cref="ArgumentNullException">Se o custo realizado for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se a OS não existir ou não estiver aberta.</exception>
    public void ConcluirManutencao(ManutencaoOsId ordemServicoId, ValorMonetario custoRealizado, DateOnly dataConclusao)
    {
        ArgumentNullException.ThrowIfNull(custoRealizado);
        var ordem = _ordensServico.FirstOrDefault(os => os.Id == ordemServicoId)
            ?? throw new InvalidOperationException("Ordem de serviço não encontrada.");

        ordem.Concluir(custoRealizado, dataConclusao);
        RaiseDomainEvent(new ManutencaoConcluida(Id, ordemServicoId, custoRealizado.Valor));
    }

    /// <summary>Registra uma multa de trânsito atribuída ao veículo (I-5/I-8).</summary>
    /// <param name="codigoInfracaoCtb">Código de infração do CTB.</param>
    /// <param name="valor">Valor da multa.</param>
    /// <param name="dataInfracao">Data da infração.</param>
    /// <param name="motoristaId">Condutor responsável, quando informado.</param>
    /// <exception cref="ArgumentNullException">Se o valor for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se o veículo estiver encerrado.</exception>
    public void RegistrarMulta(string codigoInfracaoCtb, ValorMonetario valor, DateOnly dataInfracao, Guid? motoristaId)
    {
        ArgumentNullException.ThrowIfNull(valor);
        GarantirAtivoNoAcervo();
        var multa = Multa.Registrar(codigoInfracaoCtb, valor, dataInfracao, motoristaId);
        _multas.Add(multa);
        RaiseDomainEvent(new MultaRegistrada(Id, codigoInfracaoCtb, valor.Valor, dataInfracao));
    }

    /// <summary>Registra o licenciamento anual/IPVA de um exercício (I-5/I-11).</summary>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <param name="valorIpva">Valor do IPVA.</param>
    /// <param name="valorTaxa">Valor da taxa de licenciamento.</param>
    /// <param name="data">Data do licenciamento.</param>
    /// <exception cref="ArgumentNullException">Se algum valor for nulo.</exception>
    /// <exception cref="InvalidOperationException">Se encerrado ou exercício já regular.</exception>
    public void RegistrarLicenciamento(int exercicio, ValorMonetario valorIpva, ValorMonetario valorTaxa, DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(valorIpva);
        ArgumentNullException.ThrowIfNull(valorTaxa);
        GarantirAtivoNoAcervo();
        if (_licenciamentos.Any(l => l.Exercicio == exercicio && l.Situacao == SituacaoLicenciamento.Regular))
        {
            throw new InvalidOperationException($"Já existe licenciamento regular para o exercício {exercicio}.");
        }

        var licenciamento = Licenciamento.Registrar(exercicio, valorIpva, valorTaxa, data);
        _licenciamentos.Add(licenciamento);
    }

    /// <summary>Designa (vincula) um motorista habilitado ao veículo (I-5/I-9).</summary>
    /// <param name="nome">Nome do motorista.</param>
    /// <param name="cnh">Número da CNH.</param>
    /// <param name="categoriaCnh">Categoria da CNH.</param>
    /// <param name="validadeCnh">Validade da CNH.</param>
    /// <param name="hoje">Data de referência para a checagem de validade.</param>
    /// <exception cref="ArgumentException">Se nome ou CNH forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se encerrado ou CNH vencida.</exception>
    public void DesignarMotorista(string nome, string cnh, string categoriaCnh, DateOnly validadeCnh, DateOnly hoje)
    {
        GarantirAtivoNoAcervo();
        var motorista = Motorista.Vincular(nome, cnh, categoriaCnh, validadeCnh, hoje);
        _motoristas.Add(motorista);
        MotoristaAtualId = motorista.Id.Value;
    }

    /// <summary>
    /// Reconhece a depreciação linear da competência para o veículo (BUG-P4): reaproveita o motor
    /// linear, com piso no residual e idempotência por competência (não repete a despesa do mês —
    /// evita reincidir o BUG-P2). Veículo é-um bem patrimonial e deprecia por MCASP/NBC TSP 07.
    /// </summary>
    /// <param name="competencia">Competência (mês/ano) do reconhecimento.</param>
    /// <returns>Valor efetivamente depreciado na competência.</returns>
    /// <exception cref="InvalidOperationException">Se o veículo não estiver ativo no acervo (Tombado/Cedido).</exception>
    public decimal Depreciar(DateOnly competencia)
    {
        GarantirAtivoNoAcervo();

        if (!EmCondicoesDeUso)
        {
            return 0m;
        }

        // BUG-P4/BUG-P2: idempotência por competência — reconhecer 2x a mesma competência é no-op.
        if (_historicosDepreciacao.Any(h => h.Competencia == competencia))
        {
            return 0m;
        }

        var depreciavel = ValorContabil.Valor - ValorResidual.Valor;
        if (depreciavel <= 0m)
        {
            return 0m;
        }

        var parcela = Math.Min(ParcelaMensalDepreciacao, depreciavel);
        if (parcela <= 0m)
        {
            return 0m;
        }

        ValorContabil = ValorContabil.Subtrair(ValorMonetario.De(parcela));
        _historicosDepreciacao.Add(HistoricoDepreciacao.Registrar(competencia, parcela, ValorContabil));
        RaiseDomainEvent(new VeiculoDepreciado(Id, parcela, competencia));
        return parcela;
    }

    private void GarantirAtivoNoAcervo()
    {
        if (!AtivoNoAcervo)
        {
            throw new InvalidOperationException(
                $"Operação de frota exige veículo ativo no acervo (Tombado/Cedido). Situação atual: {Situacao}.");
        }
    }
}
