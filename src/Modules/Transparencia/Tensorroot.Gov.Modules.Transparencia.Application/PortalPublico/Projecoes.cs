using MediatR;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;

/// <summary>
/// Porta de UPSERT idempotente dos read models publicos (projecao I-13). A Onda 2 NAO muda o Outbox; so
/// adiciona consumidores. Cada upsert e idempotente por chave de origem (reprocessar nao duplica).
/// </summary>
public interface IProjecaoPublicaRepository
{
    /// <summary>Upsert idempotente de despesa por <c>OrigemEventoId</c>.</summary>
    Task UpsertDespesaAsync(
        Guid tenantId, Guid origemEventoId, int exercicio, FaseDespesa fase, decimal valor, DateOnly data,
        string? numeroEmpenho, string? credorNome, string? credorDocumento, string? funcaoSubfuncao, string? fonteRecurso,
        CancellationToken cancellationToken);

    /// <summary>Upsert idempotente de receita por <c>OrigemEventoId</c>.</summary>
    Task UpsertReceitaAsync(
        Guid tenantId, Guid origemEventoId, int exercicio, decimal valor, DateOnly data, string? rubrica, string? fonteRecurso,
        CancellationToken cancellationToken);

    /// <summary>Upsert idempotente de contrato por <c>OrigemEventoId</c> (chave do contrato — agrega eventos do mesmo contrato).</summary>
    Task UpsertContratoAsync(
        Guid tenantId, Guid origemEventoId, int exercicio, decimal valor, string? numeroContrato, string? fornecedor,
        string? objeto, string? modalidade, string? numeroContratoPncp, CancellationToken cancellationToken);

    /// <summary>Registra a publicacao PNCP de um contrato ja projetado (por chave de contrato).</summary>
    Task RegistrarPncpContratoAsync(Guid tenantId, Guid contratoId, string? numeroContratoPncp, CancellationToken cancellationToken);

    /// <summary>Upsert idempotente de uma linha de folha nominal (chave = competencia + codigo do servidor).</summary>
    Task UpsertFolhaNominalAsync(
        Guid tenantId, string competencia, string codigoServidor, string servidorNome, string? cargo, string? lotacao,
        decimal remuneracaoBruta, decimal descontos, CancellationToken cancellationToken);
}

/// <summary>
/// ACL de entrada (Financas → portal publico): projeta a despesa EMPENHADA no read model
/// <c>PublicacaoDespesa</c>. Idempotente por <c>EventId</c>. Mascara o documento do credor na projecao.
/// </summary>
public sealed class ProjetarDespesaEmpenhadaHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<DespesaEmpenhadaIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(DespesaEmpenhadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var data = notification.Competencia ?? DateOnly.FromDateTime(notification.OccurredOnUtc);
        return projecoes.UpsertDespesaAsync(
            notification.TenantId, notification.EventId, data.Year, FaseDespesa.Empenhada, notification.Valor, data,
            notification.Numero, notification.CredorNome, notification.CredorDocumento,
            notification.FuncaoSubfuncao, notification.FonteRecurso, cancellationToken);
    }
}

/// <summary>ACL de entrada (Financas → portal publico): projeta a despesa LIQUIDADA. Idempotente.</summary>
public sealed class ProjetarDespesaLiquidadaHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<DespesaLiquidadaIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(DespesaLiquidadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var data = notification.Data;
        return projecoes.UpsertDespesaAsync(
            notification.TenantId, notification.EventId, data.Year, FaseDespesa.Liquidada, notification.Valor, data,
            numeroEmpenho: null, credorNome: null, credorDocumento: null,
            notification.FuncaoSubfuncao, notification.FonteRecurso, cancellationToken);
    }
}

/// <summary>ACL de entrada (Financas → portal publico): projeta o PAGAMENTO. Idempotente.</summary>
public sealed class ProjetarPagamentoEfetuadoHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<Tensorroot.Gov.Modules.Financas.Contracts.PagamentoEfetuadoIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(Tensorroot.Gov.Modules.Financas.Contracts.PagamentoEfetuadoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return projecoes.UpsertDespesaAsync(
            notification.TenantId, notification.EventId, notification.DataPagamento.Year, FaseDespesa.Paga,
            notification.ValorTotal, notification.DataPagamento, notification.Numero,
            credorNome: null, credorDocumento: null, funcaoSubfuncao: null, fonteRecurso: null, cancellationToken);
    }
}

/// <summary>ACL de entrada (Financas → portal publico): projeta a RECEITA (RCL apurada). Idempotente.</summary>
public sealed class ProjetarReceitaApuradaHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<ReceitaCorrenteLiquidaApuradaIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(ReceitaCorrenteLiquidaApuradaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var data = new DateOnly(notification.Exercicio, Math.Clamp(notification.MesReferencia, 1, 12), 1);
        return projecoes.UpsertReceitaAsync(
            notification.TenantId, notification.EventId, notification.Exercicio, notification.ValorRcl, data,
            rubrica: "RCL", fonteRecurso: null, cancellationToken);
    }
}

/// <summary>
/// ACL de entrada (Administracao → portal publico): projeta o CONTRATO assinado. Idempotente por
/// <c>EventId</c>. Nome do fornecedor/objeto/modalidade nao trafegam neste contrato — gravados quando
/// disponiveis; o read model exibe o que ha (degradacao graciosa, honesto com o leiaute do evento).
/// </summary>
public sealed class ProjetarContratoAssinadoHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<ContratoAssinadoIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(ContratoAssinadoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return projecoes.UpsertContratoAsync(
            notification.TenantId, notification.ContratoId, notification.OccurredOnUtc.Year, notification.Valor,
            numeroContrato: notification.ContratoId.ToString(), fornecedor: null, objeto: null, modalidade: null,
            numeroContratoPncp: null, cancellationToken);
    }
}

/// <summary>ACL de entrada (Administracao → portal publico): marca a publicacao PNCP do contrato. Idempotente.</summary>
public sealed class ProjetarContratoPncpHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<ContratoPublicadoPncpIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(ContratoPublicadoPncpIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return projecoes.RegistrarPncpContratoAsync(
            notification.TenantId, notification.ContratoId, notification.NumeroContratoPncp, cancellationToken);
    }
}

/// <summary>ACL de entrada (Administracao → portal publico): projeta a licitacao homologada como linha de contrato. Idempotente.</summary>
public sealed class ProjetarLicitacaoHomologadaHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<LicitacaoHomologadaIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(LicitacaoHomologadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return projecoes.UpsertContratoAsync(
            notification.TenantId, notification.LicitacaoId, notification.OccurredOnUtc.Year, notification.ValorAdjudicado,
            numeroContrato: null, fornecedor: null, objeto: "Licitacao homologada", modalidade: null,
            numeroContratoPncp: null, cancellationToken);
    }
}

/// <summary>
/// ACL de entrada (RH → portal publico): deriva a FOLHA NOMINAL do
/// <see cref="FolhaResumoRemessaTceIntegrationEvent"/>. LGPD: usa nome/cargo (publicos) e SOMA os
/// lancamentos por servidor (Vantagem→bruto, Desconto→descontos); <b>NAO le CPF nem matricula</b>. O
/// codigo do servidor compoe so a chave de idempotencia (nao e exposto). Idempotente por
/// (competencia, codigo do servidor).
/// </summary>
public sealed class ProjetarFolhaNominalHandler(IProjecaoPublicaRepository projecoes)
    : INotificationHandler<FolhaResumoRemessaTceIntegrationEvent>
{
    private const string OperacaoVantagem = "V";
    private const string OperacaoDesconto = "D";

    /// <inheritdoc />
    public async Task Handle(FolhaResumoRemessaTceIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.Servidores.Count == 0)
        {
            return;
        }

        // Agrega os lancamentos por servidor: bruto = soma das vantagens; descontos = soma dos descontos.
        var brutoPorServidor = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var descontoPorServidor = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var lancamento in notification.Lancamentos)
        {
            if (string.Equals(lancamento.Operacao, OperacaoVantagem, StringComparison.OrdinalIgnoreCase))
            {
                Acumular(brutoPorServidor, lancamento.CodigoRegistroServidor, lancamento.Valor);
            }
            else if (string.Equals(lancamento.Operacao, OperacaoDesconto, StringComparison.OrdinalIgnoreCase))
            {
                Acumular(descontoPorServidor, lancamento.CodigoRegistroServidor, lancamento.Valor);
            }
        }

        foreach (var servidor in notification.Servidores)
        {
            var bruto = brutoPorServidor.GetValueOrDefault(servidor.CodigoRegistro);
            var descontos = descontoPorServidor.GetValueOrDefault(servidor.CodigoRegistro);
            await projecoes.UpsertFolhaNominalAsync(
                notification.TenantId,
                notification.Competencia,
                servidor.CodigoRegistro,
                servidor.Nome,
                servidor.NomeCargo,
                lotacao: null,
                bruto,
                descontos,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static void Acumular(Dictionary<string, decimal> mapa, string chave, decimal valor)
        => mapa[chave] = mapa.GetValueOrDefault(chave) + valor;
}
