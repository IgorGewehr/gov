using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

/// <summary>
/// Entrada na fila de espera quando nao ha vaga: o paciente aguarda por um profissional especifico ou
/// por uma especialidade (CBO), num estabelecimento. Raiz de agregado: nasce <see cref="SituacaoFilaEspera.Aguardando"/>
/// via <see cref="Entrar"/>. Ao liberar vaga, a orquestracao convoca por prioridade e ordem de chegada
/// (FIFO dentro da mesma prioridade). Referencia Paciente/Profissional por Id (Onda 1).
/// Trata dado pessoal sensivel (LGPD art. 11).
/// </summary>
public sealed class FilaEspera : AggregateRoot<FilaEsperaId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do codigo de especialidade (CBO).</summary>
    public const int ComprimentoEspecialidade = 6;

    private FilaEspera()
    {
    }

    private FilaEspera(
        FilaEsperaId id,
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoId,
        ProfissionalId? profissionalId,
        string? especialidade,
        TipoAtendimentoAgenda tipo,
        PrioridadeAgendamento prioridade,
        DateTimeOffset dataEntrada)
        : base(id)
    {
        TenantId = tenantId;
        PacienteId = pacienteId;
        EstabelecimentoId = estabelecimentoId;
        ProfissionalId = profissionalId;
        Especialidade = especialidade;
        Tipo = tipo;
        Prioridade = prioridade;
        DataEntrada = dataEntrada;
        Situacao = SituacaoFilaEspera.Aguardando;
        RaiseDomainEvent(new PacienteIncluidoNaFilaDeEspera(id, pacienteId, profissionalId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Paciente que aguarda (referencia por Id — Onda 1).</summary>
    public PacienteId PacienteId { get; private set; }

    /// <summary>Estabelecimento (CNES) desejado (referencia por Id — Onda 1).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Profissional desejado (opcional; nulo quando a espera e por especialidade).</summary>
    public ProfissionalId? ProfissionalId { get; private set; }

    /// <summary>Especialidade (CBO) desejada (opcional; nula quando a espera e por profissional).</summary>
    public string? Especialidade { get; private set; }

    /// <summary>Natureza (consulta/exame).</summary>
    public TipoAtendimentoAgenda Tipo { get; private set; }

    /// <summary>Prioridade (ordena a convocacao).</summary>
    public PrioridadeAgendamento Prioridade { get; private set; }

    /// <summary>Data/hora de entrada na fila (ordem FIFO dentro da prioridade).</summary>
    public DateTimeOffset DataEntrada { get; private set; }

    /// <summary>Data/hora da convocacao (nula ate convocar).</summary>
    public DateTimeOffset? DataConvocacao { get; private set; }

    /// <summary>Situacao atual na fila.</summary>
    public SituacaoFilaEspera Situacao { get; private set; }

    /// <summary>
    /// Inclui o paciente na fila de espera (estado inicial <see cref="SituacaoFilaEspera.Aguardando"/>).
    /// Exige ao menos um alvo: profissional OU especialidade. Emite <see cref="PacienteIncluidoNaFilaDeEspera"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="pacienteId">Paciente.</param>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="profissionalId">Profissional desejado (opcional).</param>
    /// <param name="especialidade">Especialidade/CBO desejada (opcional).</param>
    /// <param name="tipo">Natureza (consulta/exame).</param>
    /// <param name="prioridade">Prioridade.</param>
    /// <param name="dataEntrada">Data/hora de entrada.</param>
    /// <returns>Nova <see cref="FilaEspera"/> aguardando.</returns>
    /// <exception cref="ArgumentException">Se faltarem alvos (profissional/especialidade) ou identificadores obrigatorios forem vazios.</exception>
    public static FilaEspera Entrar(
        Guid tenantId,
        PacienteId pacienteId,
        EstabelecimentoId estabelecimentoId,
        ProfissionalId? profissionalId,
        string? especialidade,
        TipoAtendimentoAgenda tipo,
        PrioridadeAgendamento prioridade,
        DateTimeOffset dataEntrada)
    {
        if (pacienteId.Value == Guid.Empty)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(pacienteId));
        }

        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento (CNES) e obrigatorio.", nameof(estabelecimentoId));
        }

        var especialidadeNormalizada = NormalizarEspecialidade(especialidade);
        var temProfissional = profissionalId is { } p && p.Value != Guid.Empty;
        if (!temProfissional && especialidadeNormalizada is null)
        {
            throw new ArgumentException("Informe profissional ou especialidade para a fila de espera.", nameof(especialidade));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException($"Tipo de atendimento invalido: {tipo}.", nameof(tipo));
        }

        if (!Enum.IsDefined(prioridade))
        {
            throw new ArgumentException($"Prioridade invalida: {prioridade}.", nameof(prioridade));
        }

        return new FilaEspera(
            FilaEsperaId.New(),
            tenantId,
            pacienteId,
            estabelecimentoId,
            temProfissional ? profissionalId : null,
            especialidadeNormalizada,
            tipo,
            prioridade,
            dataEntrada);
    }

    /// <summary>Convoca o paciente (Aguardando → Convocado) ao liberar vaga. Emite <see cref="PacienteConvocadoDaFilaDeEspera"/>.</summary>
    /// <param name="momento">Momento da convocacao.</param>
    /// <exception cref="InvalidOperationException">Se a entrada nao estiver Aguardando.</exception>
    public void Convocar(DateTimeOffset momento)
    {
        if (Situacao != SituacaoFilaEspera.Aguardando)
        {
            throw new InvalidOperationException($"Convocacao exige situacao Aguardando. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoFilaEspera.Convocado;
        DataConvocacao = momento;
        RaiseDomainEvent(new PacienteConvocadoDaFilaDeEspera(Id));
    }

    /// <summary>Marca como atendido (Convocado → Atendido), quando a marcacao da convocacao se efetiva — terminal.</summary>
    /// <exception cref="InvalidOperationException">Se a entrada nao estiver Convocado.</exception>
    public void MarcarAtendido()
    {
        if (Situacao != SituacaoFilaEspera.Convocado)
        {
            throw new InvalidOperationException($"So e possivel atender uma entrada Convocada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoFilaEspera.Atendido;
    }

    /// <summary>Remove a entrada da fila (desistencia/obsoleto) — terminal. Emite <see cref="EntradaFilaDeEsperaRemovida"/>.</summary>
    /// <param name="motivo">Motivo da remocao (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a entrada ja estiver terminal (atendida/removida).</exception>
    public void Remover(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is SituacaoFilaEspera.Atendido or SituacaoFilaEspera.Removido)
        {
            throw new InvalidOperationException($"Entrada ja encerrada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoFilaEspera.Removido;
        RaiseDomainEvent(new EntradaFilaDeEsperaRemovida(Id, motivo.Trim()));
    }

    private static string? NormalizarEspecialidade(string? especialidade)
    {
        if (string.IsNullOrWhiteSpace(especialidade))
        {
            return null;
        }

        var digitos = new string(especialidade.Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length != ComprimentoEspecialidade)
        {
            throw new ArgumentException($"Especialidade (CBO) deve ter {ComprimentoEspecialidade} digitos.", nameof(especialidade));
        }

        return digitos;
    }
}
