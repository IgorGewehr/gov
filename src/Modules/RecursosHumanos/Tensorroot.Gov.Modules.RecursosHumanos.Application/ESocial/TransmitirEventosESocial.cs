using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Empacota TODOS os eventos <c>Assinado</c> do tenant em lotes validos (&lt;=50 eventos e &lt;=5 MB),
/// envia cada lote via <see cref="IESocialGateway"/> (impl. SIMULADA ou SOAP real), guarda o protocolo
/// e transita os eventos do lote para <c>Transmitido</c>. ESOCIAL-SPEC §2.3/§4.4. Idempotente:
/// re-rodar so reempacota o que ainda esta Assinado.
/// </summary>
public sealed record TransmitirEventosAssinadosCommand : ICommand<int>;

/// <summary>Handler da transmissao de lotes.</summary>
public sealed class TransmitirEventosAssinadosHandler(
    IEmpregadorESocialProvider empregador,
    IEventoESocialRepository eventos,
    IESocialGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<TransmitirEventosAssinadosCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(TransmitirEventosAssinadosCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(p.CnpjEnte))
        {
            throw new InvalidOperationException("CNPJ do ente nao configurado (RecursosHumanos:ESocial:CnpjEnte).");
        }

        var assinados = await eventos.ListarPorEstadoAsync(EstadoEventoESocial.Assinado, cancellationToken).ConfigureAwait(false);
        if (assinados.Count == 0)
        {
            return 0;
        }

        // Indexa por identificador FORTE do agregado (sempre unico). O atributo de negocio IdEvento
        // NAO e garantidamente unico (carimbo com resolucao de segundo), entao usa-lo como chave de
        // dicionario poderia colidir; o casamento de volta usa o EventoId do item do lote.
        var porEventoId = assinados.ToDictionary(e => e.Id);
        var itens = assinados
            .Where(e => e.XmlAssinado is { Length: > 0 })
            .Select(e => new ItemLoteEvento(e.Id, e.IdEvento, e.XmlAssinado!))
            .ToList();

        var lotes = LoteEventosESocial.Particionar(TipoInscricao.Cnpj, p.CnpjEnte, p.Ambiente, itens);
        var transmitidos = 0;

        foreach (var lote in lotes)
        {
            var resposta = await gateway.EnviarLoteAsync(lote, cancellationToken).ConfigureAwait(false);
            if (!resposta.Aceito || string.IsNullOrWhiteSpace(resposta.ProtocoloLote))
            {
                // Rejeicao estrutural do lote (ex.: 612/613/607): nao transita os eventos; serao
                // reempacotados/corrigidos. O erro fica visivel para o worker mapear a acao.
                continue;
            }

            var agora = timeProvider.GetUtcNow();
            foreach (var item in lote.Itens)
            {
                if (porEventoId.TryGetValue(item.EventoId, out var evento))
                {
                    evento.RegistrarTransmissao(resposta.ProtocoloLote, agora);
                    transmitidos++;
                }
            }
        }

        if (transmitidos > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return transmitidos;
    }
}

/// <summary>
/// Consulta o processamento dos eventos <c>Transmitido</c> (por protocolo) via
/// <see cref="IESocialGateway"/> e processa o retorno: persiste <c>nrRecibo</c> por evento aceito
/// (-&gt; <c>Processado</c>) ou os erros (-&gt; <c>Rejeitado</c>). ESOCIAL-SPEC §2.5.
/// </summary>
public sealed record ConsultarRetornosESocialCommand : ICommand<int>;

/// <summary>Handler da consulta/processamento dos retornos.</summary>
public sealed class ConsultarRetornosESocialHandler(
    IEventoESocialRepository eventos,
    IESocialGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ConsultarRetornosESocialCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(ConsultarRetornosESocialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transmitidos = await eventos.ListarPorEstadoAsync(EstadoEventoESocial.Transmitido, cancellationToken).ConfigureAwait(false);
        if (transmitidos.Count == 0)
        {
            return 0;
        }

        var processados = 0;
        // Agrupa por protocolo: uma consulta por lote (respeitando o teto BX no worker).
        foreach (var grupo in transmitidos.Where(e => e.ProtocoloLote is not null).GroupBy(e => e.ProtocoloLote!, StringComparer.Ordinal))
        {
            var consulta = await gateway.ConsultarLoteAsync(grupo.Key, cancellationToken).ConfigureAwait(false);
            if (!consulta.Processado)
            {
                continue; // Ainda em processamento; tentar de novo com backoff.
            }

            // Casa o retorno (chaveado por IdEvento) com os eventos do lote. Agrupa de forma TOLERANTE a
            // eventuais IdEvento repetidos (resolucao de segundo do carimbo): cada retorno e aplicado a
            // todos os eventos do lote com aquele IdEvento, sem nunca lancar por chave duplicada.
            var porId = grupo
                .GroupBy(e => e.IdEvento, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var agora = timeProvider.GetUtcNow();
            foreach (var retorno in consulta.Eventos)
            {
                if (!porId.TryGetValue(retorno.IdEvento, out var eventosDoRetorno))
                {
                    continue;
                }

                foreach (var evento in eventosDoRetorno)
                {
                    if (retorno.Aceito && !string.IsNullOrWhiteSpace(retorno.NumeroRecibo))
                    {
                        evento.RegistrarRecibo(retorno.NumeroRecibo, agora);
                    }
                    else
                    {
                        evento.RegistrarRejeicao(retorno.CodigoErro ?? "ERRO", retorno.DescricaoErro ?? "Rejeitado pelo eSocial.", agora);
                    }

                    processados++;
                }
            }
        }

        if (processados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return processados;
    }
}
