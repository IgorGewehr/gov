using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Gera os eventos de remuneracao (S-1200 RGPS / S-1202 RPPS) de TODOS os servidores de uma folha
/// FECHADA, roteando por regime e somando as verbas por rubrica (a folha E a fonte de verdade do
/// <c>detVerbas</c> — ESOCIAL-SPEC §1.6). Idempotente por (tipo, servidor, competencia): reexecutar
/// nao duplica. // TODO(validar-oficial): grupos dmDev/infoPerApur e base RPPS do XSD.
/// </summary>
/// <param name="FolhaId">Folha de pagamento (deve estar Fechada).</param>
public sealed record GerarRemuneracaoFolhaCommand(Guid FolhaId) : ICommand<IReadOnlyList<Guid>>;

/// <summary>Handler da geracao de S-1200/S-1202 da folha.</summary>
public sealed class GerarRemuneracaoFolhaHandler(
    IEmpregadorESocialProvider empregador,
    IFolhaDePagamentoRepository folhas,
    IServidorRepository servidores,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarRemuneracaoFolhaCommand, IReadOnlyList<Guid>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> Handle(GerarRemuneracaoFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        if (folha.Situacao is not (SituacaoFolha.Fechada or SituacaoFolha.Paga))
        {
            throw new InvalidOperationException($"Remuneracao eSocial exige folha Fechada. Situacao atual: {folha.Situacao}.");
        }

        var perApur = folha.Competencia.ToString();
        var ids = new List<Guid>();

        foreach (var grupoServidor in folha.Eventos.GroupBy(e => e.ServidorId))
        {
            var servidor = await servidores.ObterPorIdAsync(new ServidorId(grupoServidor.Key), cancellationToken).ConfigureAwait(false);
            if (servidor is null)
            {
                continue;
            }

            // itensRemun (S-1.3): a folha e a fonte de verdade. Agrupa por (codigo da rubrica, TIPO), nunca
            // so por codigo: um provento e um desconto JAMAIS colapsam num mesmo vrRubr com sinais somados
            // (correcao P1-4 da AUDITORIA-FINAL — "nao somar desconto como provento"). No leiaute, vrRubr e
            // SEMPRE positivo; o sinal (provento +/desconto −) vem do tpRubr da rubrica na tabela S-1010
            // (codRubr+ideTabRubr), nao de um campo de itensRemun. indApurIR=0 (REGRA do MOS: IR apurado no
            // proprio eSocial); o valor 1 e a EXCECAO (IR apurado fora, via EFD-Reinf) — nao se aplica aqui.
            const int IndApurIrNoESocial = 0;
            var verbas = grupoServidor
                .GroupBy(e => new { e.Rubrica.Codigo, e.Tipo })
                .Select(g => new ItemVerba(
                    CodRubr: g.Key.Codigo,
                    IdeTabRubr: p.IdeTabRubricas,
                    QtdRubr: 1m,
                    VrRubr: g.Sum(e => e.Valor),
                    IndApurIr: IndApurIrNoESocial))
                .ToList();

            var insumo = new InsumoS1200(
                servidor.Cpf.ToString(),
                servidor.Matricula.Valor,
                // // TODO(validar-oficial): codCateg real do servidor (GAP — ESOCIAL-SPEC §1.4); placeholder estrutural.
                CodCateg: RoteadorRemuneracao.EhRpps(servidor.Regime) ? "301" : "101",
                perApur,
                verbas,
                InscricaoEmpregadorFactory.De(p),
                InscricaoEmpregadorFactory.EstabLotacao(p));

            var rpps = RoteadorRemuneracao.EhRpps(servidor.Regime);
            var tipo = RoteadorRemuneracao.TipoRemuneracao(servidor.Regime);
            var chave = ChaveIdempotenciaEvento.Criar(tipo, servidor.Id.Value.ToString(), perApur);
            var xml = GeradorEventosESocial.GerarS1200(insumo, idEvento: "PENDENTE", rpps);
            var evento = await gerador.MaterializarAsync(p, tipo, chave, xml, cancellationToken).ConfigureAwait(false);
            ids.Add(evento.Id.Value);
        }

        return ids;
    }
}

/// <summary>
/// Gera os eventos S-1210 (pagamentos) de TODOS os servidores de uma folha PAGA. Idempotente por
/// (tipo, servidor, competencia). // TODO(validar-oficial): grupos infoPgto/detPgtoFl do XSD S-1210.
/// </summary>
/// <param name="FolhaId">Folha de pagamento (deve estar Paga).</param>
public sealed record GerarPagamentosFolhaCommand(Guid FolhaId) : ICommand<IReadOnlyList<Guid>>;

/// <summary>Handler da geracao de S-1210 da folha.</summary>
public sealed class GerarPagamentosFolhaHandler(
    IEmpregadorESocialProvider empregador,
    IFolhaDePagamentoRepository folhas,
    IServidorRepository servidores,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarPagamentosFolhaCommand, IReadOnlyList<Guid>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> Handle(GerarPagamentosFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        if (folha.Situacao != SituacaoFolha.Paga || folha.DataPagamento is not { } dataPagamento)
        {
            throw new InvalidOperationException($"S-1210 exige folha Paga. Situacao atual: {folha.Situacao}.");
        }

        var perRef = folha.Competencia.ToString();
        var ids = new List<Guid>();

        foreach (var grupoServidor in folha.Eventos.GroupBy(e => e.ServidorId))
        {
            var servidor = await servidores.ObterPorIdAsync(new ServidorId(grupoServidor.Key), cancellationToken).ConfigureAwait(false);
            if (servidor is null)
            {
                continue;
            }

            // Liquido por servidor = proventos - descontos (a folha ja arredonda cada evento).
            var proventos = grupoServidor.Where(e => e.Tipo == TipoEvento.Provento).Sum(e => e.Valor);
            var descontos = grupoServidor.Where(e => e.Tipo == TipoEvento.Desconto).Sum(e => e.Valor);
            var liquido = proventos - descontos;
            if (liquido < 0m)
            {
                liquido = 0m;
            }

            var insumo = new InsumoS1210(servidor.Cpf.ToString(), perRef, dataPagamento, liquido);
            var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S1210Pagamentos, servidor.Id.Value.ToString(), perRef);
            var xml = GeradorEventosESocial.GerarS1210(insumo, idEvento: "PENDENTE");
            var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S1210Pagamentos, chave, xml, cancellationToken).ConfigureAwait(false);
            ids.Add(evento.Id.Value);
        }

        return ids;
    }
}

/// <summary>
/// Gera o S-1299 (fechamento da competencia). Emitido quando a folha esta Fechada/Paga e os periodicos
/// foram gerados. Idempotente por (tipo, competencia). // TODO(validar-oficial): grupos infoFech do XSD.
/// </summary>
/// <param name="FolhaId">Folha de pagamento (Fechada/Paga).</param>
public sealed record GerarFechamentoFolhaCommand(Guid FolhaId) : ICommand<Guid>;

/// <summary>Handler da geracao de S-1299.</summary>
public sealed class GerarFechamentoFolhaHandler(
    IEmpregadorESocialProvider empregador,
    IFolhaDePagamentoRepository folhas,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarFechamentoFolhaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarFechamentoFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        if (folha.Situacao is not (SituacaoFolha.Fechada or SituacaoFolha.Paga))
        {
            throw new InvalidOperationException($"S-1299 exige folha Fechada. Situacao atual: {folha.Situacao}.");
        }

        var perApur = folha.Competencia.ToString();
        var houveRemun = folha.Eventos.Count > 0;
        var houvePgto = folha.Situacao == SituacaoFolha.Paga;
        var insumo = new InsumoS1299(perApur, houveRemun, houvePgto);
        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S1299Fechamento, request.FolhaId.ToString(), perApur);
        var xml = GeradorEventosESocial.GerarS1299(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S1299Fechamento, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}
