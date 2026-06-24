using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

/// <summary>
/// Carteira de vacinacao do paciente (1-1 por <see cref="PacienteId"/>): historico de doses aplicadas,
/// motor de aprazamento da proxima dose e a situacao vacinal por imunobiologico. Raiz de agregado e
/// fronteira de consistencia do esquema: impede duplicidade de numero de dose por imunobiologico
/// (I-IMUN-1) e progride o esquema em ordem (I-IMUN-2). Trata dado pessoal SENSIVEL (LGPD art. 11).
/// </summary>
public sealed class CarteiraVacinacao : AggregateRoot<CarteiraVacinacaoId>, IMustHaveTenant
{
    private readonly List<DoseAplicada> _doses = [];

    private CarteiraVacinacao()
    {
    }

    private CarteiraVacinacao(CarteiraVacinacaoId id, Guid tenantId, PacienteId pacienteId)
        : base(id)
    {
        TenantId = tenantId;
        PacienteId = pacienteId;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Paciente titular da carteira (referencia por Id) — dado sensivel (LGPD).</summary>
    public PacienteId PacienteId { get; private set; }

    /// <summary>Doses aplicadas (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<DoseAplicada> Doses => _doses;

    /// <summary>Abre a carteira de vacinacao de um paciente (vazia).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="pacienteId">Paciente titular.</param>
    /// <returns>Nova <see cref="CarteiraVacinacao"/> vazia.</returns>
    /// <exception cref="ArgumentException">Se o paciente for vazio.</exception>
    public static CarteiraVacinacao Abrir(Guid tenantId, PacienteId pacienteId)
    {
        if (pacienteId.Value == Guid.Empty)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(pacienteId));
        }

        return new CarteiraVacinacao(CarteiraVacinacaoId.New(), tenantId, pacienteId);
    }

    /// <summary>
    /// Registra a aplicacao de uma dose, calculando o aprazamento da proxima a partir do esquema do
    /// imunobiologico (intervalo parametrizado). Valida que a dose nao foi aplicada antes (I-IMUN-1),
    /// que o numero esta dentro do esquema (I-IMUN-2), que o esquema progride em ordem — a dose N exige
    /// que a dose N-1 ja exista (I-IMUN-3) — e que a data de aplicacao nao e futura (I-IMUN-4).
    /// Emite <see cref="DoseAplicadaRegistrada"/>.
    /// </summary>
    /// <param name="imunobiologico">Imunobiologico aplicado (define total de doses e intervalo).</param>
    /// <param name="tipoDose">Tipo da dose aplicada.</param>
    /// <param name="numeroDose">Numero da dose no esquema (1..TotalDoses).</param>
    /// <param name="lote">Lote aplicado.</param>
    /// <param name="aplicadorId">Profissional aplicador.</param>
    /// <param name="dataAplicacao">Data de aplicacao (nao pode ser futura em relacao a <paramref name="hoje"/>).</param>
    /// <param name="hoje">Data de referencia (relogio) para barrar aplicacao com data futura — I-IMUN-4.</param>
    /// <returns>A <see cref="DoseAplicada"/> registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se a dose ja foi aplicada, o numero excede o esquema, a dose anterior do esquema nao existe
    /// (fora de ordem) ou a data de aplicacao e futura.
    /// </exception>
    public DoseAplicada RegistrarDose(
        Imunobiologico imunobiologico,
        TipoDose tipoDose,
        int numeroDose,
        string lote,
        ProfissionalId aplicadorId,
        DateOnly dataAplicacao,
        DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(imunobiologico);
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroDose, 1);

        // I-IMUN-4: data de aplicacao nao pode ser futura (registro clinico com data impossivel — fail-closed).
        if (dataAplicacao > hoje)
        {
            throw new InvalidOperationException(
                $"Data de aplicacao ({dataAplicacao:yyyy-MM-dd}) nao pode ser futura (referencia {hoje:yyyy-MM-dd}).");
        }

        // I-IMUN-2: numero da dose dentro do esquema.
        if (numeroDose > imunobiologico.TotalDoses)
        {
            throw new InvalidOperationException(
                $"Numero da dose ({numeroDose}) excede o esquema do imunobiologico ({imunobiologico.TotalDoses} dose(s)).");
        }

        // I-IMUN-1: nao reaplicar a mesma dose do mesmo imunobiologico.
        if (_doses.Any(d => d.ImunobiologicoId == imunobiologico.Id && d.NumeroDose == numeroDose))
        {
            throw new InvalidOperationException("Dose ja aplicada para este imunobiologico.");
        }

        // I-IMUN-3: esquema progride em ordem — a dose N so e aceita se a dose N-1 ja foi aplicada.
        if (numeroDose > 1
            && !_doses.Any(d => d.ImunobiologicoId == imunobiologico.Id && d.NumeroDose == numeroDose - 1))
        {
            throw new InvalidOperationException(
                $"Dose {numeroDose} fora de ordem: a dose {numeroDose - 1} do esquema ainda nao foi aplicada.");
        }

        // Aprazamento: ha proxima dose se nao for unica e ainda faltarem doses no esquema.
        DateOnly? aprazada = null;
        if (!imunobiologico.DoseUnica && numeroDose < imunobiologico.TotalDoses)
        {
            aprazada = dataAplicacao.AddDays(imunobiologico.IntervaloDiasProximaDose);
        }

        var dose = DoseAplicada.Registrar(imunobiologico.Id, tipoDose, numeroDose, lote, aplicadorId, dataAplicacao, aprazada);
        _doses.Add(dose);
        RaiseDomainEvent(new DoseAplicadaRegistrada(Id, PacienteId, imunobiologico.Id, numeroDose, dataAplicacao));
        return dose;
    }

    /// <summary>
    /// Situacao vacinal do paciente para um imunobiologico na data de referencia: Completo (todas as
    /// doses), Atrasado (proxima dose aprazada vencida) ou EmDia (aprazada no futuro).
    /// </summary>
    /// <param name="imunobiologico">Imunobiologico a avaliar.</param>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns>A situacao vacinal consolidada.</returns>
    public SituacaoVacinal SituacaoPara(Imunobiologico imunobiologico, DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(imunobiologico);

        var aplicadas = _doses.Where(d => d.ImunobiologicoId == imunobiologico.Id).ToList();
        if (aplicadas.Count >= imunobiologico.TotalDoses)
        {
            return SituacaoVacinal.Completo;
        }

        var ultima = aplicadas.OrderByDescending(d => d.NumeroDose).FirstOrDefault();
        if (ultima?.ProximaDoseAprazada is { } aprazada && aprazada < hoje)
        {
            return SituacaoVacinal.Atrasado;
        }

        return SituacaoVacinal.EmDia;
    }

    /// <summary>
    /// Aprazamentos vencidos (proxima dose com data anterior a referencia e nao cumprida) — alvo da
    /// busca ativa. Considera vencida a ultima dose de um imunobiologico cuja proxima nunca foi aplicada.
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns>As doses cujo aprazamento esta vencido e sem dose subsequente registrada.</returns>
    public IReadOnlyList<DoseAplicada> AprazamentosVencidos(DateOnly hoje)
        => _doses
            .Where(d => d.ProximaDoseAprazada is { } p && p < hoje
                && !_doses.Any(s => s.ImunobiologicoId == d.ImunobiologicoId && s.NumeroDose > d.NumeroDose))
            .ToList();
}
