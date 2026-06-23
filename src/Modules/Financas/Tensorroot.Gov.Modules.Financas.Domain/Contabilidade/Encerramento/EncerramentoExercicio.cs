using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;

/// <summary>Identificador forte do agregado <see cref="EncerramentoExercicio"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EncerramentoExercicioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EncerramentoExercicioId"/>.</returns>
    public static EncerramentoExercicioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Fases do encerramento de exercício (máquina de estados "só para frente" — DESIGN §3). Cada fase
/// só roda após a anterior; reexecutar uma fase concluída é no-op (idempotência).
/// </summary>
public enum StatusEncerramento
{
    /// <summary>Iniciado; nenhuma fase concluída.</summary>
    Aberto = 0,

    /// <summary>Restos a Pagar do exercício corrente inscritos (mês 12).</summary>
    RapInscrito = 1,

    /// <summary>Encerramento parcial concluído (reclassificações/ajustes — mês 13).</summary>
    EncerramentoParcial = 2,

    /// <summary>Resultado patrimonial apurado (classes 3/4 zeradas — mês 13).</summary>
    ApuracaoPatrimonial = 3,

    /// <summary>Resultado orçamentário apurado (classes 5/6 de execução zeradas — mês 13).</summary>
    ApuracaoOrcamentaria = 4,

    /// <summary>Exercício encerrado (congelado): base da MSC de encerramento/DCA.</summary>
    Encerrado = 5,

    /// <summary>Abertura do exercício seguinte concluída (mês 0).</summary>
    AberturaConcluida = 6,
}

/// <summary>
/// Agregado de controle do encerramento de um exercício contábil. Não carrega os lançamentos —
/// apenas registra que cada fase ocorreu, garantindo idempotência e ordem das transições. O
/// encerramento (Status >= Encerrado) CONGELA o exercício: reexecutar = no-op/recusa (DESIGN §5).
/// </summary>
public sealed class EncerramentoExercicio : AggregateRoot<EncerramentoExercicioId>, IMustHaveTenant
{
    private EncerramentoExercicio()
    {
    }

    private EncerramentoExercicio(EncerramentoExercicioId id, Guid tenantId, int exercicio, DateTime iniciadoEmUtc)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Status = StatusEncerramento.Aberto;
        IniciadoEmUtc = iniciadoEmUtc;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício a encerrar.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Fase corrente.</summary>
    public StatusEncerramento Status { get; private set; }

    /// <summary>Início do processo (UTC).</summary>
    public DateTime IniciadoEmUtc { get; private set; }

    /// <summary>Conclusão do encerramento (UTC), se já encerrado.</summary>
    public DateTime? EncerradoEmUtc { get; private set; }

    /// <summary>Conclusão da abertura do exercício seguinte (UTC), se já aberta.</summary>
    public DateTime? AberturaConcluidaEmUtc { get; private set; }

    /// <summary>Indica se o exercício está congelado (encerrado): nenhuma reapuração é admitida.</summary>
    public bool Congelado => Status >= StatusEncerramento.Encerrado;

    /// <summary>
    /// Inicia o controle de encerramento de um exercício (idempotente: o repositório recupera o
    /// agregado existente; este factory só cria quando ainda não há registro para o exercício).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício a encerrar.</param>
    /// <param name="momentoUtc">Instante de início (UTC).</param>
    /// <returns>Novo agregado de encerramento.</returns>
    public static EncerramentoExercicio Iniciar(Guid tenantId, int exercicio, DateTime momentoUtc)
    {
        if (exercicio < 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(exercicio), exercicio, "Exercicio invalido.");
        }

        return new EncerramentoExercicio(EncerramentoExercicioId.New(), tenantId, exercicio, momentoUtc);
    }

    /// <summary>
    /// Tenta avançar para a fase informada. Retorna <c>true</c> se houve transição (a fase deve ser
    /// executada); <c>false</c> se a fase já estava concluída (no-op idempotente). Recusa pular fases.
    /// </summary>
    /// <param name="fase">Fase desejada (deve ser a imediatamente seguinte).</param>
    /// <returns><c>true</c> se a fase deve ser executada agora; <c>false</c> se já concluída.</returns>
    /// <exception cref="InvalidOperationException">Se a transição pular fases ou for inválida.</exception>
    public bool TentarAvancarPara(StatusEncerramento fase)
    {
        if (Status >= fase)
        {
            return false; // já concluída — no-op idempotente
        }

        if ((int)fase != (int)Status + 1)
        {
            throw new InvalidOperationException(
                $"Transicao invalida de {Status} para {fase}: as fases do encerramento avancam uma a uma.");
        }

        Status = fase;
        return true;
    }

    /// <summary>Marca a conclusão de uma fase, registrando o instante e disparando o evento de fase.</summary>
    /// <param name="momentoUtc">Instante (UTC) da conclusão.</param>
    public void RegistrarConclusaoDeFase(DateTime momentoUtc)
    {
        switch (Status)
        {
            case StatusEncerramento.RapInscrito:
                RaiseDomainEvent(new RestosAPagarInscritos(Id, Exercicio));
                break;
            case StatusEncerramento.ApuracaoPatrimonial:
                RaiseDomainEvent(new ResultadoPatrimonialApurado(Id, Exercicio));
                break;
            case StatusEncerramento.ApuracaoOrcamentaria:
                RaiseDomainEvent(new ResultadoOrcamentarioApurado(Id, Exercicio));
                break;
            case StatusEncerramento.Encerrado:
                EncerradoEmUtc = momentoUtc;
                RaiseDomainEvent(new ExercicioEncerrado(Id, Exercicio));
                break;
            case StatusEncerramento.AberturaConcluida:
                AberturaConcluidaEmUtc = momentoUtc;
                RaiseDomainEvent(new ExercicioSeguinteAberto(Id, Exercicio + 1));
                break;
            case StatusEncerramento.Aberto:
            case StatusEncerramento.EncerramentoParcial:
            default:
                break;
        }
    }
}
