using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.Events;

/// <summary>Restos a Pagar do exercício corrente inscritos (mês 12) — entram na MSC agregada de dezembro.</summary>
/// <param name="EncerramentoId">Identificador do agregado de encerramento.</param>
/// <param name="Exercicio">Exercício encerrado.</param>
public sealed record RestosAPagarInscritos(
    Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.EncerramentoExercicioId EncerramentoId,
    int Exercicio) : IDomainEvent;

/// <summary>Resultado patrimonial apurado (VPA/VPD encerradas contra 2.3.7.1.1.01.00 — mês 13).</summary>
/// <param name="EncerramentoId">Identificador do agregado de encerramento.</param>
/// <param name="Exercicio">Exercício encerrado.</param>
public sealed record ResultadoPatrimonialApurado(
    Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.EncerramentoExercicioId EncerramentoId,
    int Exercicio) : IDomainEvent;

/// <summary>Resultado orçamentário apurado (classes 5/6 de execução zeradas — mês 13).</summary>
/// <param name="EncerramentoId">Identificador do agregado de encerramento.</param>
/// <param name="Exercicio">Exercício encerrado.</param>
public sealed record ResultadoOrcamentarioApurado(
    Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.EncerramentoExercicioId EncerramentoId,
    int Exercicio) : IDomainEvent;

/// <summary>Exercício encerrado e congelado — base da MSC de encerramento/DCA.</summary>
/// <param name="EncerramentoId">Identificador do agregado de encerramento.</param>
/// <param name="Exercicio">Exercício encerrado.</param>
public sealed record ExercicioEncerrado(
    Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.EncerramentoExercicioId EncerramentoId,
    int Exercicio) : IDomainEvent;

/// <summary>Abertura do exercício seguinte concluída (transposição + resultado — mês 0).</summary>
/// <param name="EncerramentoId">Identificador do agregado de encerramento.</param>
/// <param name="ExercicioAberto">Exercício seguinte aberto.</param>
public sealed record ExercicioSeguinteAberto(
    Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento.EncerramentoExercicioId EncerramentoId,
    int ExercicioAberto) : IDomainEvent;
