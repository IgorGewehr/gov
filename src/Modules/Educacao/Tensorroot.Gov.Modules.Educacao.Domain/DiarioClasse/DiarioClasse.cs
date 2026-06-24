using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

/// <summary>
/// Diario de Classe — registro digital de frequencia, conteudos e notas de uma matricula em uma
/// turma. Mantem vinculo 1-1 com a <see cref="Matricula"/> (I-7) e apura o resultado anual:
/// aprovacao exige frequencia maior ou igual a 75% da carga horaria (I-2); abaixo disso,
/// reprovacao por frequencia. Raiz de agregado que nasce valida via <see cref="Abrir"/> e protege
/// as invariantes da maquina de estados (Aberto -&gt; Apurado).
/// </summary>
public sealed class DiarioClasse : AggregateRoot<DiarioClasseId>, IMustHaveTenant
{
    /// <summary>Frequencia minima para aprovacao: 75% da carga horaria (LDB Lei 9.394/1996).</summary>
    public const decimal FrequenciaMinimaAprovacao = 0.75m;

    /// <summary>Minimo de dias letivos por ano (LDB Lei 9.394/1996).</summary>
    public const int DiasLetivosMinimos = 200;

    /// <summary>Media minima por componente para aprovacao por nota (escala 0 a 10).</summary>
    private const decimal MediaMinimaAprovacao = 6.0m;

    private readonly List<RegistroFrequencia> _frequencias = [];
    private readonly List<RegistroNota> _notas = [];
    private readonly List<RegistroAula> _aulas = [];

    private DiarioClasse()
    {
    }

    private DiarioClasse(DiarioClasseId id, Guid tenantId, MatriculaId matriculaId, int cargaHorariaTotal)
        : base(id)
    {
        TenantId = tenantId;
        MatriculaId = matriculaId;
        CargaHorariaTotal = cargaHorariaTotal;
        Situacao = SituacaoDiario.Aberto;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Matricula vinculada (1-1 — I-7).</summary>
    public MatriculaId MatriculaId { get; private set; }

    /// <summary>Carga horaria anual de referencia (800h Fundamental / 1.000h Medio).</summary>
    public int CargaHorariaTotal { get; private set; }

    /// <summary>Situacao atual do diario.</summary>
    public SituacaoDiario Situacao { get; private set; }

    /// <summary>Resultado apurado (nulo ate a apuracao).</summary>
    public ResultadoAluno? Resultado { get; private set; }

    /// <summary>Registros de frequencia (entidades-filhas).</summary>
    public IReadOnlyCollection<RegistroFrequencia> Frequencias => _frequencias.AsReadOnly();

    /// <summary>Registros de nota (entidades-filhas).</summary>
    public IReadOnlyCollection<RegistroNota> Notas => _notas.AsReadOnly();

    /// <summary>Registros de aula/dia letivo (entidades-filhas).</summary>
    public IReadOnlyCollection<RegistroAula> Aulas => _aulas.AsReadOnly();

    /// <summary>
    /// Percentual de frequencia (I-1): presencas ponderadas pela carga horaria da aula dividido
    /// pela <see cref="CargaHorariaTotal"/>. Propriedade calculada (nao persistida).
    /// </summary>
    public decimal PercentualFrequencia
        => CargaHorariaTotal <= 0
            ? 0m
            : (decimal)_frequencias.Where(f => f.Presente).Sum(f => f.CargaHorariaAula) / CargaHorariaTotal;

    /// <summary>Quantidade de dias letivos registrados (RegistroAula com DiaLetivo = true — I-9).</summary>
    public int DiasLetivosRegistrados => _aulas.Count(a => a.DiaLetivo);

    /// <summary>
    /// Abre um diario de classe para a matricula informada, em situacao
    /// <see cref="SituacaoDiario.Aberto"/> (estado inicial).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="matriculaId">Matricula vinculada (1-1 — I-7).</param>
    /// <param name="cargaHorariaTotal">Carga horaria anual de referencia (positiva).</param>
    /// <returns>Novo <see cref="DiarioClasse"/> aberto.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a carga horaria total nao for positiva.</exception>
    public static DiarioClasse Abrir(Guid tenantId, MatriculaId matriculaId, int cargaHorariaTotal)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cargaHorariaTotal);
        if (matriculaId.Value == Guid.Empty)
        {
            throw new ArgumentException("Matricula obrigatoria para abertura de diario.", nameof(matriculaId));
        }

        return new DiarioClasse(DiarioClasseId.New(), tenantId, matriculaId, cargaHorariaTotal);
    }

    /// <summary>Registra a frequencia (presenca/falta) de uma aula/dia (I-3).</summary>
    /// <param name="data">Data do registro.</param>
    /// <param name="presente">Presenca do aluno.</param>
    /// <param name="cargaHorariaAula">Carga horaria da aula (positiva).</param>
    /// <exception cref="InvalidOperationException">Se o diario nao estiver Aberto (I-3/I-6).</exception>
    public void RegistrarFrequencia(DateOnly data, bool presente, int cargaHorariaAula)
    {
        GarantirAberto();
        _frequencias.Add(RegistroFrequencia.Registrar(data, presente, cargaHorariaAula));
        RaiseDomainEvent(new FrequenciaRegistrada(Id, data));
    }

    /// <summary>Lanca a nota de um componente curricular em um periodo (I-3/I-8).</summary>
    /// <param name="componente">Componente curricular (valido — I-8).</param>
    /// <param name="periodo">Periodo de avaliacao (informado — I-8).</param>
    /// <param name="valor">Valor da nota.</param>
    /// <exception cref="InvalidOperationException">Se o diario nao estiver Aberto (I-3/I-6).</exception>
    /// <exception cref="ArgumentException">Se o componente for invalido ou o periodo nao for informado (I-8).</exception>
    public void LancarNota(ComponenteCurricularId componente, string periodo, decimal valor)
    {
        GarantirAberto();
        _notas.Add(RegistroNota.Lancar(componente, periodo, valor));
        RaiseDomainEvent(new NotaLancada(Id, componente, periodo));
    }

    /// <summary>Registra uma aula/dia (conta como dia letivo quando <paramref name="diaLetivo"/> — I-9).</summary>
    /// <param name="data">Data da aula.</param>
    /// <param name="conteudo">Conteudo ministrado.</param>
    /// <param name="diaLetivo">Indica se conta como dia letivo.</param>
    /// <exception cref="InvalidOperationException">Se o diario nao estiver Aberto (I-3/I-6).</exception>
    public void RegistrarAula(DateOnly data, string conteudo, bool diaLetivo)
    {
        GarantirAberto();
        _aulas.Add(RegistroAula.Registrar(data, conteudo, diaLetivo));
    }

    /// <summary>
    /// Apura o resultado anual (I-4/I-5): calcula a frequencia e as medias por componente e fecha
    /// o diario em <see cref="SituacaoDiario.Apurado"/>, emitindo <see cref="ResultadoApurado"/>.
    /// Frequencia menor que 75% leva a <see cref="ResultadoAluno.ReprovadoPorFrequencia"/> (I-2 —
    /// resultado negativo objetivo, independe de nota). Com frequencia suficiente: sem nenhuma nota
    /// lancada o resultado e <see cref="ResultadoAluno.Cursando"/> (fail-closed — nunca aprova de
    /// forma vacua, I-13); havendo rendimento, <see cref="ResultadoAluno.Aprovado"/> se as medias
    /// forem suficientes em todos os componentes, senao <see cref="ResultadoAluno.Reprovado"/>.
    /// </summary>
    /// <returns>O resultado apurado.</returns>
    /// <exception cref="InvalidOperationException">Se o diario nao estiver Aberto (I-4/I-6).</exception>
    public ResultadoAluno ApurarResultado()
    {
        GarantirAberto();

        ResultadoAluno resultado;
        if (PercentualFrequencia < FrequenciaMinimaAprovacao)
        {
            // Reprovacao por frequencia e fato objetivo (LDB); independe de nota lancada.
            resultado = ResultadoAluno.ReprovadoPorFrequencia;
        }
        else if (!PossuiRendimentoLancado)
        {
            // Frequencia suficiente, mas sem qualquer nota: nao ha base avaliativa para concluir.
            // Fail-closed (I-13): jamais aprovar de forma vacua (.All sobre colecao vazia = true).
            resultado = ResultadoAluno.Cursando;
        }
        else
        {
            resultado = MediasSuficientes() ? ResultadoAluno.Aprovado : ResultadoAluno.Reprovado;
        }

        Resultado = resultado;
        Situacao = SituacaoDiario.Apurado;
        RaiseDomainEvent(new ResultadoApurado(Id, resultado));
        return resultado;
    }

    /// <summary>
    /// Indica se ha ao menos um registro de rendimento (nota) lancado. Pre-condicao para qualquer
    /// apuracao por nota (Aprovado/Reprovado); sem rendimento o diario fica <c>Cursando</c> (I-13).
    /// </summary>
    public bool PossuiRendimentoLancado => _notas.Count > 0;

    /// <summary>Indica se o calendario cumpriu o minimo de 200 dias letivos (I-9).</summary>
    /// <returns><c>true</c> se houver ao menos <see cref="DiasLetivosMinimos"/> dias letivos registrados.</returns>
    public bool CumpriuCalendario() => DiasLetivosRegistrados >= DiasLetivosMinimos;

    /// <summary>
    /// Media suficiente exige media maior ou igual a 6,0 em cada componente lancado. Pressupoe
    /// rendimento lancado (chamado apenas quando <see cref="PossuiRendimentoLancado"/>); o caso de
    /// colecao vazia (que tornaria <c>All</c> vacuamente verdadeiro) e barrado a montante em
    /// <see cref="ApurarResultado"/> via estado <see cref="ResultadoAluno.Cursando"/>.
    /// </summary>
    private bool MediasSuficientes()
        => _notas
            .GroupBy(n => n.Componente)
            .All(grupo => grupo.Average(n => n.Valor) >= MediaMinimaAprovacao);

    private void GarantirAberto()
    {
        if (Situacao != SituacaoDiario.Aberto)
        {
            throw new InvalidOperationException(
                $"Operacao exige diario Aberto. Situacao atual: {Situacao}.");
        }
    }
}
