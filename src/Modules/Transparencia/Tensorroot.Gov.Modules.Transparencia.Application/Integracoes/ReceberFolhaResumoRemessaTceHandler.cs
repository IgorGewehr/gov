using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — RecursosHumanos): consome o
/// <see cref="FolhaResumoRemessaTceIntegrationEvent"/> publicado pelo RH ao fechar uma folha e MATERIALIZA
/// o snapshot como <see cref="ResumoFolhaTce"/> (read model), de onde a REMESSA DE FOLHA ao TCE-RS
/// (Res. 1099 / SIAPC Vol. V) e montada. Espelha o <c>ReceberMSCGeradaHandler</c> (ponte Financas ->
/// Transparencia). Idempotente por <c>FolhaDePagamentoId</c> (I-13: reprocessar atualiza o mesmo resumo).
/// </summary>
public sealed class ReceberFolhaResumoRemessaTceHandler(
    IResumoFolhaTceRepository resumos,
    IUnitOfWork unitOfWork,
    ILogger<ReceberFolhaResumoRemessaTceHandler> logger)
    : INotificationHandler<FolhaResumoRemessaTceIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(FolhaResumoRemessaTceIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Degradacao graciosa: folha sem servidores/lancamentos (competencia vazia) nada produz —
        // ack idempotente sem materializar (nao envenena o Outbox). [ponte RH->Transparencia]
        if (notification.Servidores.Count == 0 || notification.Lancamentos.Count == 0)
        {
            logger.LogInformation(
                "Resumo de folha {Folha} ({Competencia}) sem servidores/lancamentos; nada a materializar (evento {EventId} reconhecido).",
                notification.FolhaDePagamentoId,
                notification.Competencia,
                notification.EventId);
            return;
        }

        var (exercicio, mes) = ExtrairCompetencia(notification.Competencia);

        var servidores = notification.Servidores.Select(servidor => ServidorFolhaResumo.Criar(
            servidor.CodigoRegistro,
            servidor.Cpf,
            servidor.Nome,
            servidor.Matricula,
            servidor.DataNascimento,
            servidor.DataAdmissao,
            servidor.DataDemissao,
            servidor.CodigoCargo,
            servidor.NomeCargo,
            servidor.Regime));

        var rubricas = notification.Rubricas.Select(rubrica => RubricaFolhaResumo.Criar(
            rubrica.Codigo,
            rubrica.Descricao,
            rubrica.Operacao,
            rubrica.IncideIrrf,
            rubrica.IncideRpps,
            rubrica.IncideInss,
            rubrica.BaseLegal,
            rubrica.ContaPlanoFolhaTce));

        var lancamentos = notification.Lancamentos.Select(lancamento => LancamentoFolhaResumo.Criar(
            lancamento.CodigoRegistroServidor,
            lancamento.CodigoRubrica,
            lancamento.Operacao,
            lancamento.Valor));

        var existente = await resumos
            .ObterPorFolhaAsync(notification.FolhaDePagamentoId, cancellationToken)
            .ConfigureAwait(false);

        if (existente is null)
        {
            var resumo = ResumoFolhaTce.Criar(
                notification.TenantId,
                notification.FolhaDePagamentoId,
                exercicio,
                mes,
                notification.TipoFolha,
                notification.DataPagamento,
                servidores,
                rubricas,
                lancamentos);
            resumos.Adicionar(resumo);
        }
        else
        {
            // I-13: idempotencia — reprocessamento atualiza o mesmo resumo, nao duplica.
            existente.Substituir(notification.TipoFolha, notification.DataPagamento, servidores, rubricas, lancamentos);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // Competencia em AAAA-MM (a folha mensal e a unica fonte de remessa de folha — periodicidade mensal).
    private static (int Exercicio, int Mes) ExtrairCompetencia(string competencia)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competencia);
        var partes = competencia.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (partes.Length < 2
            || !int.TryParse(partes[0], out var exercicio)
            || !int.TryParse(partes[1], out var mes))
        {
            throw new InvalidOperationException($"Competencia de folha invalida para remessa TCE: '{competencia}'.");
        }

        return (exercicio, mes);
    }
}
