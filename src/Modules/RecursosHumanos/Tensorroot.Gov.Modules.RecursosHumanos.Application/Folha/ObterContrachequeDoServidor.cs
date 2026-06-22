using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Linha (rubrica) de um contracheque.</summary>
/// <param name="Rubrica">Codigo da rubrica.</param>
/// <param name="Tipo">Provento ou desconto.</param>
/// <param name="Valor">Valor apurado da verba.</param>
public sealed record LinhaContracheque(string Rubrica, string Tipo, decimal Valor);

/// <summary>Contracheque de um servidor numa folha (projecao de leitura).</summary>
/// <param name="ServidorId">Servidor do contracheque.</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="Linhas">Linhas (rubricas) do contracheque.</param>
/// <param name="TotalProventos">Soma dos proventos do servidor.</param>
/// <param name="TotalDescontos">Soma dos descontos do servidor.</param>
/// <param name="LiquidoAPagar">Liquido a pagar do servidor.</param>
public sealed record ContrachequeDto(
    Guid ServidorId,
    string Competencia,
    IReadOnlyList<LinhaContracheque> Linhas,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal LiquidoAPagar);

/// <summary>Obtem o contracheque de um servidor numa folha (tenant-scoped).</summary>
/// <param name="FolhaDePagamentoId">Folha de referencia.</param>
/// <param name="ServidorId">Servidor do contracheque.</param>
public sealed record ObterContrachequeDoServidorQuery(Guid FolhaDePagamentoId, Guid ServidorId) : IQuery<ContrachequeDto?>;

/// <summary>Handler da consulta de contracheque do servidor.</summary>
public sealed class ObterContrachequeDoServidorHandler(IFolhaDePagamentoRepository folhas)
    : IQueryHandler<ObterContrachequeDoServidorQuery, ContrachequeDto?>
{
    /// <inheritdoc />
    public async Task<ContrachequeDto?> Handle(ObterContrachequeDoServidorQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false);
        if (folha is null)
        {
            return null;
        }

        var eventosDoServidor = folha.Eventos
            .Where(evento => evento.ServidorId == request.ServidorId)
            .ToList();

        if (eventosDoServidor.Count == 0)
        {
            return null;
        }

        var linhas = eventosDoServidor
            .Select(evento => new LinhaContracheque(evento.Rubrica.Codigo, evento.Tipo.ToString(), evento.Valor))
            .ToList();

        var totalProventos = eventosDoServidor
            .Where(evento => evento.Tipo == TipoEvento.Provento)
            .Sum(evento => evento.Valor);

        var totalDescontos = eventosDoServidor
            .Where(evento => evento.Tipo == TipoEvento.Desconto)
            .Sum(evento => evento.Valor);

        var liquido = totalProventos - totalDescontos;

        return new ContrachequeDto(
            request.ServidorId,
            folha.Competencia.ToString(),
            linhas,
            totalProventos,
            totalDescontos,
            liquido < 0m ? 0m : liquido);
    }
}
