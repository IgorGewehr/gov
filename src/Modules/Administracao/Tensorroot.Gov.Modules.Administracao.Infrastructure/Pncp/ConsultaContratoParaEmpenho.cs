using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Pncp;

/// <summary>
/// Implementacao da porta de leitura cross-module <see cref="IConsultaContratoParaEmpenho"/>: traduz o
/// estado do agregado <see cref="Contrato"/> (no tenant atual, Global Query Filter) no veredito chapado
/// que o modulo Financas consome ANTES de empenhar (invariante de bloqueio W9.1 — Lei 14.133/2021, art. 94).
/// A regra de aptidao e do dominio (<see cref="Contrato.PodeEmpenhar"/>); aqui apenas mapeia o motivo do
/// bloqueio para o consumidor. Read-only.
/// </summary>
public sealed class ConsultaContratoParaEmpenho(AdministracaoDbContext context) : IConsultaContratoParaEmpenho
{
    /// <inheritdoc />
    public async Task<StatusContratoParaEmpenho> ConsultarAsync(Guid contratoId, CancellationToken cancellationToken)
    {
        var contrato = await context.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == new ContratoId(contratoId), cancellationToken)
            .ConfigureAwait(false);

        if (contrato is null)
        {
            return StatusContratoParaEmpenho.NaoEncontrado;
        }

        if (contrato.Situacao is SituacaoContrato.Encerrado or SituacaoContrato.Rescindido)
        {
            return StatusContratoParaEmpenho.BloqueadoContratoExtinto;
        }

        // Invariante de bloqueio: sem numero de controle PNCP, o contrato e ineficaz e NAO empenha.
        return contrato.PodeEmpenhar()
            ? StatusContratoParaEmpenho.AptoParaEmpenho
            : StatusContratoParaEmpenho.BloqueadoSemPncp;
    }
}
