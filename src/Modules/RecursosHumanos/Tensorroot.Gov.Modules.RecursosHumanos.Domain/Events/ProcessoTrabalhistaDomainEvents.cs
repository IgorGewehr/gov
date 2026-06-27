using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Processo trabalhista cadastrado (situacao inicial <c>EmAndamento</c>).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="NumeroProcesso">Numero do processo (CNJ).</param>
/// <param name="ValorCausa">Valor da causa.</param>
/// <param name="ValorProvisionado">Valor provisionado inicial (NBC TG 25).</param>
public sealed record ProcessoTrabalhistaCadastrado(
    ProcessoTrabalhistaId ProcessoId,
    string NumeroProcesso,
    decimal ValorCausa,
    decimal ValorProvisionado) : IDomainEvent;

/// <summary>Provisao do processo reavaliada (mudanca de prognostico/valor provisionado).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Prognostico">Novo prognostico de perda.</param>
/// <param name="ValorProvisionado">Novo valor provisionado.</param>
public sealed record ProvisaoProcessoAtualizada(
    ProcessoTrabalhistaId ProcessoId,
    PrognosticoPerda Prognostico,
    decimal ValorProvisionado) : IDomainEvent;

/// <summary>Processo trabalhista encerrado (acordo/condenacao/improcedencia).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Situacao">Situacao terminal de encerramento.</param>
/// <param name="ValorEfetivo">Valor efetivo da saida de recursos (zero na improcedencia).</param>
public sealed record ProcessoTrabalhistaEncerrado(
    ProcessoTrabalhistaId ProcessoId,
    SituacaoProcessoTrabalhista Situacao,
    decimal ValorEfetivo) : IDomainEvent;
