using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>
/// Vinculo de pessoal de ente publico (Executivo ou Legislativo): servidor estatutario (RPPS)
/// ou empregado publico celetista (RGPS). Modela o ciclo de vida Nomeacao -> Posse -> Exercicio,
/// a aquisicao de estabilidade (3 anos de efetivo exercicio — CF art. 41), afastamentos e
/// desligamento, com transmissao dos eventos nao periodicos ao eSocial (S-2200/S-2206/S-2230/S-2299).
/// </summary>
public sealed class Servidor : AggregateRoot<ServidorId>, IMustHaveTenant
{
    /// <summary>Anos de efetivo exercicio para aquisicao da estabilidade (CF/1988 art. 41).</summary>
    public const int AnosParaEstabilidade = 3;

    /// <summary>Prazo legal (em dias) para a posse, contado da nomeacao (RJU — Lei 8.112/1990 supletiva).</summary>
    public const int PrazoPosseDias = 30;

    private readonly List<Dependente> _dependentes = [];

    private Servidor()
    {
    }

    private Servidor(
        ServidorId id,
        Guid tenantId,
        Cpf cpf,
        Matricula matricula,
        DadosPessoais dadosPessoais,
        CargoId cargoId,
        RegimePrevidenciario regime,
        DateOnly dataNomeacao)
        : base(id)
    {
        TenantId = tenantId;
        Cpf = cpf;
        Matricula = matricula;
        DadosPessoais = dadosPessoais;
        CargoId = cargoId;
        Regime = regime;
        DataNomeacao = dataNomeacao;
        Situacao = SituacaoServidor.Nomeado;
        RaiseDomainEvent(new ServidorAdmitido(id, matricula, cargoId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>CPF do servidor (dado sensivel — mascarado em logs/projecoes e redigido na trilha — LG-3).</summary>
    [CampoSensivelLgpd]
    public Cpf Cpf { get; private set; } = default!;

    /// <summary>Matricula unica do vinculo no tenant.</summary>
    public Matricula Matricula { get; private set; } = default!;

    /// <summary>Dados cadastrais sensiveis (LGPD).</summary>
    public DadosPessoais DadosPessoais { get; private set; } = default!;

    /// <summary>Cargo provido (associacao ao agregado Cargo).</summary>
    public CargoId CargoId { get; private set; }

    /// <summary>Regime previdenciario: RPPS (efetivo) ou RGPS (demais).</summary>
    public RegimePrevidenciario Regime { get; private set; }

    /// <summary>Data do provimento/nomeacao.</summary>
    public DateOnly DataNomeacao { get; private set; }

    /// <summary>Data da posse; nula antes de registrada.</summary>
    public DateOnly? DataPosse { get; private set; }

    /// <summary>Data de inicio de exercicio; nula antes de iniciado.</summary>
    public DateOnly? DataExercicio { get; private set; }

    /// <summary>Data de aquisicao da estabilidade; gravada ao conceder.</summary>
    public DateOnly? DataEstabilidade { get; private set; }

    /// <summary>Data de encerramento do vinculo; nula enquanto ativo.</summary>
    public DateOnly? DataDesligamento { get; private set; }

    /// <summary>Situacao atual no ciclo de vida.</summary>
    public SituacaoServidor Situacao { get; private set; }

    /// <summary>Dependentes do servidor (IR/beneficios), expostos somente pela raiz.</summary>
    public IReadOnlyCollection<Dependente> Dependentes => _dependentes;

    /// <summary>
    /// Admite (provimento) um servidor a partir de cargo provido; nasce em situacao
    /// <see cref="SituacaoServidor.Nomeado"/> e emite <see cref="ServidorAdmitido"/> (I-9).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cpf">CPF do servidor.</param>
    /// <param name="matricula">Matricula unica no tenant.</param>
    /// <param name="dadosPessoais">Dados cadastrais sensiveis.</param>
    /// <param name="cargoId">Cargo provido.</param>
    /// <param name="regime">Regime previdenciario (RPPS/RGPS).</param>
    /// <param name="dataNomeacao">Data do provimento/nomeacao.</param>
    /// <returns>Novo <see cref="Servidor"/> em situacao <see cref="SituacaoServidor.Nomeado"/>.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="cpf"/>, <paramref name="matricula"/> ou <paramref name="dadosPessoais"/> forem nulos (I-1).</exception>
    public static Servidor Admitir(
        Guid tenantId,
        Cpf cpf,
        Matricula matricula,
        DadosPessoais dadosPessoais,
        CargoId cargoId,
        RegimePrevidenciario regime,
        DateOnly dataNomeacao)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        ArgumentNullException.ThrowIfNull(matricula);
        ArgumentNullException.ThrowIfNull(dadosPessoais);

        return new Servidor(
            ServidorId.New(),
            tenantId,
            cpf,
            matricula,
            dadosPessoais,
            cargoId,
            regime,
            dataNomeacao);
    }

    /// <summary>Acrescenta um dependente ao servidor (IR/beneficios).</summary>
    /// <param name="nome">Nome civil do dependente.</param>
    /// <param name="parentesco">Grau de parentesco.</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <exception cref="InvalidOperationException">Se o servidor estiver desligado (I-8).</exception>
    public void AdicionarDependente(string nome, string parentesco, DateOnly dataNascimento)
    {
        GarantirNaoDesligado();
        _dependentes.Add(Dependente.Registrar(nome, parentesco, dataNascimento));
    }

    /// <summary>
    /// Registra a posse do servidor dentro do prazo legal (I-3/I-4); transita para
    /// <see cref="SituacaoServidor.Empossado"/> e emite <see cref="PosseRegistrada"/>.
    /// </summary>
    /// <param name="dataPosse">Data da posse.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoServidor.Nomeado"/> (I-3) ou se a posse houver caducado (I-4).</exception>
    public void RegistrarPosse(DateOnly dataPosse)
    {
        GarantirNaoDesligado();
        if (Situacao != SituacaoServidor.Nomeado)
        {
            throw new InvalidOperationException($"A posse so pode ocorrer a partir de Nomeado. Situacao atual: {Situacao}.");
        }

        // I-4: a posse caduca se ocorrer alem do prazo legal contado da nomeacao;
        // expirado o prazo, o provimento e tornado sem efeito e nenhum evento e emitido.
        var limite = DataNomeacao.AddDays(PrazoPosseDias);
        if (dataPosse > limite)
        {
            throw new InvalidOperationException("Posse caducada: prazo legal de posse expirado; provimento tornado sem efeito.");
        }

        DataPosse = dataPosse;
        Situacao = SituacaoServidor.Empossado;
        RaiseDomainEvent(new PosseRegistrada(Id, dataPosse));
    }

    /// <summary>
    /// Inicia o efetivo exercicio do servidor (I-3); transita para
    /// <see cref="SituacaoServidor.EmExercicio"/> e emite <see cref="ExercicioIniciado"/>.
    /// </summary>
    /// <param name="dataExercicio">Data de inicio do exercicio.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoServidor.Empossado"/> (I-3).</exception>
    public void IniciarExercicio(DateOnly dataExercicio)
    {
        GarantirNaoDesligado();
        if (Situacao != SituacaoServidor.Empossado)
        {
            throw new InvalidOperationException($"O exercicio so pode iniciar a partir de Empossado. Situacao atual: {Situacao}.");
        }

        DataExercicio = dataExercicio;
        Situacao = SituacaoServidor.EmExercicio;
        RaiseDomainEvent(new ExercicioIniciado(Id, dataExercicio));
    }

    /// <summary>
    /// Concede a estabilidade ao servidor efetivo apos 3 anos de efetivo exercicio (I-5);
    /// transita para <see cref="SituacaoServidor.Estavel"/> e emite <see cref="EstabilidadeConcedida"/>.
    /// </summary>
    /// <param name="hoje">Data de referencia (normalmente hoje, via TimeProvider).</param>
    /// <exception cref="InvalidOperationException">Se o regime nao for RPPS, se nao estiver em exercicio ou se nao houver 3 anos completos de exercicio (I-5).</exception>
    public void ConcederEstabilidade(DateOnly hoje)
    {
        GarantirNaoDesligado();
        if (Situacao != SituacaoServidor.EmExercicio)
        {
            throw new InvalidOperationException($"A estabilidade so pode ser concedida a partir de EmExercicio. Situacao atual: {Situacao}.");
        }

        // I-5: estabilidade somente para servidor efetivo (RPPS).
        if (Regime != RegimePrevidenciario.Rpps)
        {
            throw new InvalidOperationException("Estabilidade exclusiva de servidor efetivo (RPPS).");
        }

        if (DataExercicio is not { } inicioExercicio)
        {
            throw new InvalidOperationException("Servidor sem data de exercicio nao adquire estabilidade.");
        }

        // I-5: exige 3 anos completos de efetivo exercicio.
        var elegivelEm = inicioExercicio.AddYears(AnosParaEstabilidade);
        if (hoje < elegivelEm)
        {
            throw new InvalidOperationException($"Estabilidade exige {AnosParaEstabilidade} anos de efetivo exercicio.");
        }

        DataEstabilidade = elegivelEm;
        Situacao = SituacaoServidor.Estavel;
        RaiseDomainEvent(new EstabilidadeConcedida(Id, elegivelEm));
    }

    /// <summary>
    /// Registra um afastamento temporario quando o servidor esta em atividade plena (I-7);
    /// transita para <see cref="SituacaoServidor.Afastado"/> e emite <see cref="AfastamentoRegistrado"/>.
    /// </summary>
    /// <param name="inicio">Inicio do afastamento.</param>
    /// <param name="fim">Fim previsto (nulo quando indeterminado).</param>
    /// <param name="motivo">Motivo do afastamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="fim"/> for anterior a <paramref name="inicio"/> (B-7).</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao estiver em {EmExercicio, Estavel} (I-7).</exception>
    public void RegistrarAfastamento(DateOnly inicio, DateOnly? fim, string motivo)
    {
        GarantirNaoDesligado();
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (fim is { } termino && termino < inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fim), "Fim do afastamento nao pode ser anterior ao inicio.");
        }

        // I-7: afastamento exige atividade plena (EmExercicio ou Estavel).
        if (Situacao is not (SituacaoServidor.EmExercicio or SituacaoServidor.Estavel))
        {
            throw new InvalidOperationException($"Afastamento exige servidor em atividade plena (EmExercicio/Estavel). Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoServidor.Afastado;
        RaiseDomainEvent(new AfastamentoRegistrado(Id, inicio, fim, motivo));
    }

    /// <summary>
    /// Retorna o servidor do afastamento, recolocando-o em <see cref="SituacaoServidor.EmExercicio"/>.
    /// Exposto para futura orquestracao de caso de uso.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoServidor.Afastado"/> (B-8).</exception>
    public void RetornarDeAfastamento()
    {
        if (Situacao != SituacaoServidor.Afastado)
        {
            throw new InvalidOperationException($"Retorno exige servidor Afastado. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoServidor.EmExercicio;
    }

    /// <summary>
    /// Desliga o servidor (exoneracao/demissao/aposentadoria/rescisao) a partir de qualquer
    /// situacao nao terminal (I-8); transita para <see cref="SituacaoServidor.Desligado"/> e
    /// emite <see cref="ServidorDesligado"/> (I-10).
    /// </summary>
    /// <param name="dataDesligamento">Data de encerramento do vinculo.</param>
    /// <param name="motivo">Motivo do desligamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o servidor ja estiver desligado (I-8).</exception>
    public void Desligar(DateOnly dataDesligamento, string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirNaoDesligado();

        DataDesligamento = dataDesligamento;
        Situacao = SituacaoServidor.Desligado;
        RaiseDomainEvent(new ServidorDesligado(Id, dataDesligamento, motivo));
    }

    private void GarantirNaoDesligado()
    {
        // I-8: desligado e estado terminal; nao admite novas transicoes.
        if (Situacao == SituacaoServidor.Desligado)
        {
            throw new InvalidOperationException("Servidor desligado e estado terminal; nao admite novas transicoes.");
        }
    }
}
