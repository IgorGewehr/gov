using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Encerramento;

/// <summary>Status do encerramento de um exercício.</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Status">Fase corrente.</param>
/// <param name="Congelado">Indica se o exercício está congelado (encerrado).</param>
/// <param name="IniciadoEmUtc">Início do processo (UTC), se iniciado.</param>
/// <param name="EncerradoEmUtc">Conclusão do encerramento (UTC), se já encerrado.</param>
/// <param name="AberturaConcluidaEmUtc">Conclusão da abertura (UTC), se já aberta.</param>
public sealed record EncerramentoStatusDto(
    int Exercicio,
    StatusEncerramento Status,
    bool Congelado,
    DateTime? IniciadoEmUtc,
    DateTime? EncerradoEmUtc,
    DateTime? AberturaConcluidaEmUtc);

/// <summary>Consulta o status do encerramento de um exercício (RBAC <c>financas.ver</c>).</summary>
/// <param name="Exercicio">Exercício.</param>
public sealed record ConsultarEncerramentoQuery(int Exercicio) : IQuery<EncerramentoStatusDto?>;

/// <summary>Handler da consulta de status do encerramento.</summary>
public sealed class ConsultarEncerramentoHandler(IEncerramentoExercicioRepository encerramentos)
    : IQueryHandler<ConsultarEncerramentoQuery, EncerramentoStatusDto?>
{
    /// <inheritdoc />
    public async Task<EncerramentoStatusDto?> Handle(ConsultarEncerramentoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var encerramento = await encerramentos.ObterPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        if (encerramento is null)
        {
            return null;
        }

        return new EncerramentoStatusDto(
            encerramento.Exercicio,
            encerramento.Status,
            encerramento.Congelado,
            encerramento.IniciadoEmUtc,
            encerramento.EncerradoEmUtc,
            encerramento.AberturaConcluidaEmUtc);
    }
}
