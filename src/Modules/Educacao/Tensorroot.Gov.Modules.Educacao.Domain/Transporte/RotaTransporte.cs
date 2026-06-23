using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

/// <summary>
/// Rota de transporte escolar (PNATE): itinerario por turno atendendo uma escola, com a quilometragem,
/// a modalidade (proprio/terceirizado), o veiculo da Frota (FK logica por Id — Patrimonio) e os alunos
/// transportados (reuso de Aluno/Matricula por Id, mesmo modulo). Raiz de agregado que nasce
/// <see cref="SituacaoRotaTransporte.Planejada"/> via <see cref="Criar"/>. Operacao local; prestacao de
/// contas PNATE ao FNDE = M10 (atras de ACL).
/// </summary>
public sealed class RotaTransporte : AggregateRoot<RotaTransporteId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do nome da rota.</summary>
    public const int ComprimentoNome = 120;

    private readonly List<AlunoTransportado> _alunos = [];

    private RotaTransporte()
    {
    }

    private RotaTransporte(
        RotaTransporteId id,
        Guid tenantId,
        EscolaId escolaId,
        string nome,
        Turno turno,
        ModalidadeTransporte modalidade,
        Guid? veiculoId,
        decimal quilometragem)
        : base(id)
    {
        TenantId = tenantId;
        EscolaId = escolaId;
        Nome = nome;
        Turno = turno;
        Modalidade = modalidade;
        VeiculoId = veiculoId;
        Quilometragem = quilometragem;
        Situacao = SituacaoRotaTransporte.Planejada;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Escola atendida pela rota (FK logica ao agregado Escola — referencia por Id).</summary>
    public EscolaId EscolaId { get; private set; }

    /// <summary>Nome/identificacao da rota (ex.: "Linha Rural Norte").</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Turno de atendimento (reusa o Turno de Turmas).</summary>
    public Turno Turno { get; private set; }

    /// <summary>Modalidade de execucao (proprio/terceirizado).</summary>
    public ModalidadeTransporte Modalidade { get; private set; }

    /// <summary>
    /// Veiculo da Frota (FK logica ao agregado Veiculo de Patrimonio — referencia por Id). Obrigatorio
    /// na modalidade <see cref="ModalidadeTransporte.Proprio"/>; nulo no terceirizado (I-R2).
    /// </summary>
    public Guid? VeiculoId { get; private set; }

    /// <summary>Quilometragem do itinerario (km, &gt;= 0).</summary>
    public decimal Quilometragem { get; private set; }

    /// <summary>Situacao atual da rota.</summary>
    public SituacaoRotaTransporte Situacao { get; private set; }

    /// <summary>Alunos transportados (entidades-filhas).</summary>
    public IReadOnlyCollection<AlunoTransportado> Alunos => _alunos.AsReadOnly();

    /// <summary>Quantidade de alunos ativos na rota.</summary>
    public int TotalAtivos => _alunos.Count(aluno => aluno.Ativo);

    /// <summary>
    /// Cria uma rota de transporte (situacao inicial <see cref="SituacaoRotaTransporte.Planejada"/>).
    /// Na modalidade Proprio o veiculo da Frota e obrigatorio; no Terceirizado deve ser nulo (I-R2).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="escolaId">Escola atendida.</param>
    /// <param name="nome">Nome da rota (obrigatorio).</param>
    /// <param name="turno">Turno de atendimento.</param>
    /// <param name="modalidade">Modalidade de execucao.</param>
    /// <param name="veiculoId">Veiculo da Frota (Patrimonio) por Id — obrigatorio se Proprio.</param>
    /// <param name="quilometragem">Quilometragem do itinerario (&gt;= 0).</param>
    /// <returns>Nova <see cref="RotaTransporte"/> em situacao Planejada.</returns>
    /// <exception cref="ArgumentException">Se nome/escola forem invalidos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se turno/modalidade invalidos ou km negativa.</exception>
    /// <exception cref="InvalidOperationException">Se a coerencia veiculo x modalidade for violada (I-R2).</exception>
    public static RotaTransporte Criar(
        Guid tenantId,
        EscolaId escolaId,
        string nome,
        Turno turno,
        ModalidadeTransporte modalidade,
        Guid? veiculoId,
        decimal quilometragem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        if (escolaId.Value == Guid.Empty)
        {
            throw new ArgumentException("Escola obrigatoria.", nameof(escolaId));
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Nome excede {ComprimentoNome} caracteres.", nameof(nome));
        }

        if (!Enum.IsDefined(turno))
        {
            throw new ArgumentOutOfRangeException(nameof(turno), "Turno invalido.");
        }

        if (!Enum.IsDefined(modalidade))
        {
            throw new ArgumentOutOfRangeException(nameof(modalidade), "Modalidade invalida.");
        }

        if (quilometragem < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quilometragem), "Quilometragem nao pode ser negativa.");
        }

        GarantirCoerenciaVeiculo(modalidade, veiculoId);

        var rota = new RotaTransporte(
            RotaTransporteId.New(), tenantId, escolaId, nomeNormalizado, turno, modalidade, veiculoId, quilometragem);
        rota.RaiseDomainEvent(new RotaTransporteCriada(rota.Id));
        return rota;
    }

    /// <summary>
    /// Vincula um aluno a rota com o ponto de embarque. Admitido enquanto Planejada ou Ativa (nao
    /// Encerrada — I-R1); o aluno nao pode ter vinculo ativo duplicado na mesma rota (I-R4).
    /// </summary>
    /// <param name="alunoId">Aluno (por Id).</param>
    /// <param name="matriculaId">Matricula vigente (opcional, por Id).</param>
    /// <param name="pontoEmbarque">Ponto de embarque.</param>
    /// <returns>O identificador do vinculo criado.</returns>
    /// <exception cref="InvalidOperationException">Se a rota estiver Encerrada (I-R1) ou houver duplicidade (I-R4).</exception>
    public AlunoTransportadoId VincularAluno(AlunoId alunoId, MatriculaId? matriculaId, string pontoEmbarque)
    {
        if (Situacao == SituacaoRotaTransporte.Encerrada)
        {
            throw new InvalidOperationException("Rota encerrada nao admite novos alunos (I-R1).");
        }

        if (_alunos.Any(aluno => aluno.Ativo && aluno.AlunoId == alunoId))
        {
            throw new InvalidOperationException("Aluno ja vinculado ativamente a esta rota (I-R4).");
        }

        var transportado = AlunoTransportado.Vincular(alunoId, matriculaId, pontoEmbarque);
        _alunos.Add(transportado);
        RaiseDomainEvent(new AlunoVinculadoRota(Id, alunoId));
        return transportado.Id;
    }

    /// <summary>Desliga (logicamente) um aluno da rota.</summary>
    /// <param name="alunoTransportadoId">Vinculo a desligar.</param>
    /// <exception cref="InvalidOperationException">Se o vinculo nao existir ou ja estiver inativo.</exception>
    public void DesligarAluno(AlunoTransportadoId alunoTransportadoId)
    {
        var transportado = _alunos.FirstOrDefault(aluno => aluno.Id == alunoTransportadoId)
            ?? throw new InvalidOperationException("Vinculo de aluno nao encontrado na rota.");
        if (!transportado.Ativo)
        {
            throw new InvalidOperationException("Aluno ja desligado da rota.");
        }

        transportado.Desligar();
    }

    /// <summary>Ativa a rota para operacao (Planejada -&gt; Ativa). Exige ao menos um aluno ativo (I-R5).</summary>
    /// <exception cref="InvalidOperationException">Se nao estiver Planejada ou nao houver alunos (I-R5).</exception>
    public void Ativar()
    {
        if (Situacao != SituacaoRotaTransporte.Planejada)
        {
            throw new InvalidOperationException($"A ativacao exige rota Planejada. Situacao atual: {Situacao}.");
        }

        if (TotalAtivos == 0)
        {
            throw new InvalidOperationException("Rota sem alunos nao pode ser ativada (I-R5).");
        }

        Situacao = SituacaoRotaTransporte.Ativa;
    }

    /// <summary>Encerra a rota (estado terminal).</summary>
    /// <exception cref="InvalidOperationException">Se ja estiver Encerrada.</exception>
    public void Encerrar()
    {
        if (Situacao == SituacaoRotaTransporte.Encerrada)
        {
            throw new InvalidOperationException("Rota ja encerrada.");
        }

        Situacao = SituacaoRotaTransporte.Encerrada;
    }

    private static void GarantirCoerenciaVeiculo(ModalidadeTransporte modalidade, Guid? veiculoId)
    {
        var temVeiculo = veiculoId is { } id && id != Guid.Empty;
        if (modalidade == ModalidadeTransporte.Proprio && !temVeiculo)
        {
            throw new InvalidOperationException("Modalidade Proprio exige veiculo da Frota (I-R2).");
        }

        if (modalidade == ModalidadeTransporte.Terceirizado && temVeiculo)
        {
            throw new InvalidOperationException("Modalidade Terceirizado nao admite veiculo proprio (I-R2).");
        }
    }
}
