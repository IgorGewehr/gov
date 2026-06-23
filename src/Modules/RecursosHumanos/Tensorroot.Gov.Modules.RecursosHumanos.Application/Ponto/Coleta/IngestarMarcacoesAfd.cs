using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

/// <summary>Resultado da ingestao de um AFD: quantas marcacoes foram novas, duplicadas, pendentes ou suspeitas.</summary>
/// <param name="Novas">Marcacoes novas ingeridas (vinculadas a um servidor).</param>
/// <param name="Duplicadas">Marcacoes ja existentes (chave natural (REP, NSR)) — ignoradas, idempotencia.</param>
/// <param name="PendentesDeVinculo">Marcacoes cujo CPF nao tem servidor no tenant — nao ingeridas, auditavel.</param>
/// <param name="IntegridadeOk">Veredicto de integridade do arquivo (CRC + NSR contiguo + contador).</param>
/// <param name="MaiorNsrEquipamentoIngerido">Maior NSR de equipamento ingerido (avanca o cursor do REP).</param>
public sealed record ResultadoIngestaoAfd(
    int Novas,
    int Duplicadas,
    int PendentesDeVinculo,
    bool IntegridadeOk,
    long? MaiorNsrEquipamentoIngerido);

/// <summary>
/// Ingere as marcacoes de um AFD coletado/importado de um equipamento REP no nosso dominio de ponto,
/// de forma IDEMPOTENTE (dedup por chave natural <c>(TenantId, RepId, NsrEquipamento)</c>): reimportar
/// o mesmo AFD nao duplica marcacoes. Valida CRC-16, continuidade de NSR e o contador do trailer
/// (FAIL-CLOSED — CLAUDE.md §16). O AFD nao carrega sentido (entrada/saida): a batida e ingerida e o
/// sentido e definido por pareamento na apuracao (<c>TratamentoJornada</c>).
/// </summary>
/// <param name="RepId">Equipamento de origem (deve estar cadastrado no tenant).</param>
/// <param name="ConteudoAfd">Bytes do AFD posicional 671 (ISO-8859-1).</param>
/// <param name="AssinaturaCades">Assinatura CAdES detached (.p7s) do AFD, quando disponivel; opcional.</param>
public sealed record IngestarMarcacoesAfdCommand(
    Guid RepId,
    byte[] ConteudoAfd,
    byte[]? AssinaturaCades) : ICommand<ResultadoIngestaoAfd>;

/// <summary>Validacao da ingestao de AFD.</summary>
public sealed class IngestarMarcacoesAfdValidator : AbstractValidator<IngestarMarcacoesAfdCommand>
{
    /// <summary>Define as regras.</summary>
    public IngestarMarcacoesAfdValidator()
    {
        RuleFor(c => c.RepId).NotEmpty().WithMessage("REP de origem e obrigatorio.");
        RuleFor(c => c.ConteudoAfd).NotNull().Must(c => c.Length > 0)
            .WithMessage("Conteudo do AFD vazio.");
    }
}

/// <summary>Handler idempotente de ingestao de AFD.</summary>
public sealed class IngestarMarcacoesAfdHandler(
    IParserAfd parser,
    IMarcacaoPontoRepository marcacoes,
    IRepRepository reps,
    IServidorPontoConsulta servidores,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<IngestarMarcacoesAfdCommand, ResultadoIngestaoAfd>
{
    /// <inheritdoc />
    public async Task<ResultadoIngestaoAfd> Handle(IngestarMarcacoesAfdCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rep = await reps.ObterPorIdAsync(new RepConfiguradoId(request.RepId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("REP nao cadastrado no tenant.");

        // 1) Parse + integridade. // TODO(validar-oficial): leiaute/CRC integrais do Anexo 671.
        var parsed = parser.Parse(request.ConteudoAfd);

        // FAIL-CLOSED (CLAUDE.md §16): CRC quebrado ou contador divergente -> recusa o lote inteiro.
        // A continuidade de NSR (lacuna) e SUSPEITA mas nao bloqueia: a lacuna pode ser real (coleta
        // incremental a partir de um NSR > 1); registramos o veredicto e ingerimos o que for novo.
        if (!parsed.CrcOk || !parsed.ContadorTrailerOk)
        {
            throw new InvalidOperationException(
                "AFD reprovado na integridade (CRC/contador): ingestao recusada (fail-closed).");
        }

        // // TODO(prod): validar a assinatura CAdES (.p7s) destacada do AFD com o A1/ICP-Brasil do REP
        // (reusar IAssinaturaEmEscopoDedicado/Cofre) ANTES de tratar, quando o fabricante a fornecer.
        _ = request.AssinaturaCades;

        if (parsed.Marcacoes.Count == 0)
        {
            return new ResultadoIngestaoAfd(0, 0, 0, parsed.IntegridadeOk, null);
        }

        // 2) Dedup EM LOTE por (REP, NSR-equipamento) — nucleo da idempotencia.
        var nsrsLote = parsed.Marcacoes.Select(m => m.NsrEquipamento).ToList();
        var jaVistos = await marcacoes
            .NsrsEquipamentoExistentesAsync(request.RepId, nsrsLote, cancellationToken)
            .ConfigureAwait(false);

        // NSR interno do NOSSO REP-P: proximo da sequencia sem lacunas do tenant (espaco distinto do
        // NSR do equipamento). Avancamos em memoria conforme inserimos.
        var ultimoNsrInterno = await marcacoes.ObterUltimoNsrAsync(cancellationToken).ConfigureAwait(false);
        var proximoNsrInterno = ultimoNsrInterno is { } v ? Nsr.De(v).Proximo() : Nsr.Primeiro();

        var novas = 0;
        var duplicadas = 0;
        var pendentes = 0;
        long? maiorNsrIngerido = null;

        // Ordena por NSR de equipamento (cronologia do REP) para sequenciar o nosso NSR de forma estavel.
        foreach (var registro in parsed.Marcacoes.OrderBy(m => m.NsrEquipamento))
        {
            if (jaVistos.Contains(registro.NsrEquipamento))
            {
                duplicadas++;
                maiorNsrIngerido = Maior(maiorNsrIngerido, registro.NsrEquipamento);
                continue;
            }

            var servidorId = await servidores
                .ResolverServidorPorCpfAsync(registro.Cpf.Digitos, cancellationToken)
                .ConfigureAwait(false);

            if (servidorId is null)
            {
                // CPF sem servidor no tenant: NAO descarta (auditavel); aguarda vinculo posterior.
                pendentes++;
                continue;
            }

            // O AFD nao carrega sentido: ingerimos como Entrada (placeholder); o pareamento real (E/S)
            // ocorre na apuracao (TratamentoJornada), que ordena por data/hora e alterna. O sentido
            // gravado aqui nao e usado no calculo de jornada. // TODO(validar-oficial).
            var marcacao = MarcacaoPonto.RegistrarDeEquipamento(
                tenantContext.TenantId,
                servidorId.Value,
                registro.Cpf,
                proximoNsrInterno,
                registro.DataHora,
                SentidoMarcacao.Entrada,
                rep.Tipo,
                request.RepId,
                registro.NsrEquipamento);

            marcacoes.Adicionar(marcacao);
            proximoNsrInterno = proximoNsrInterno.Proximo();
            novas++;
            maiorNsrIngerido = Maior(maiorNsrIngerido, registro.NsrEquipamento);
        }

        // 3) Avanca o cursor do REP (coleta incremental) e persiste tudo numa transacao (Outbox + auditoria).
        if (maiorNsrIngerido is { } maior)
        {
            rep.AvancarUltimoNsrColetado(maior);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoIngestaoAfd(novas, duplicadas, pendentes, parsed.IntegridadeOk, maiorNsrIngerido);
    }

    private static long Maior(long? atual, long candidato)
        => atual is { } a && a >= candidato ? a : candidato;
}
