using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;

/// <summary>Uma folha de ferias do PROPRIO servidor numa competencia.</summary>
/// <param name="Competencia">Competencia da folha de ferias (<c>AAAA-MM</c>).</param>
/// <param name="TotalProventos">Total de proventos do servidor nessa folha (remuneracao + 1/3 + abono).</param>
/// <param name="TotalDescontos">Total de descontos do servidor nessa folha.</param>
/// <param name="LiquidoAPagar">Liquido a pagar do servidor nessa folha.</param>
/// <param name="Situacao">Situacao da folha (aberta/calculada/fechada/paga).</param>
public sealed record MinhasFeriasItemDto(
    string Competencia,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal LiquidoAPagar,
    string Situacao);

/// <summary>Ferias do PROPRIO servidor num ano (uma linha por folha de ferias com verbas do servidor).</summary>
/// <param name="ServidorId">Servidor (o proprio usuario).</param>
/// <param name="Ano">Ano de referencia.</param>
/// <param name="Folhas">Folhas de ferias do servidor no ano.</param>
public sealed record MinhasFeriasDto(Guid ServidorId, int Ano, IReadOnlyList<MinhasFeriasItemDto> Folhas);

/// <summary>
/// AUTOSSERVICO: lista as ferias (folhas de <see cref="TipoFolha.Ferias"/>) do PROPRIO usuario
/// autenticado num ano. O ServidorId NUNCA vem do cliente — e resolvido do vinculo do usuario
/// autenticado. Dado pessoal (LGPD): implementa <see cref="ISensivelLgpd"/> e gera trilha (LG-2).
/// </summary>
/// <param name="Ano">Ano de referencia.</param>
public sealed record ObterMinhasFeriasQuery(int Ano)
    : IQuery<MinhasFeriasDto>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MinhasFerias";

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisAutosservico.Aplicaveis;
}

/// <summary>Handler das ferias proprias.</summary>
public sealed class ObterMinhasFeriasHandler(
    IResolvedorServidorDoUsuarioAutenticado resolvedor,
    IFolhaDePagamentoRepository folhas)
    : IQueryHandler<ObterMinhasFeriasQuery, MinhasFeriasDto>
{
    /// <inheritdoc />
    public async Task<MinhasFeriasDto> Handle(ObterMinhasFeriasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = await resolvedor.ResolverServidorAtualAsync(cancellationToken).ConfigureAwait(false);

        var folhasFerias = await folhas
            .ListarPorTipoEAnoAsync(request.Ano, TipoFolha.Ferias, cancellationToken)
            .ConfigureAwait(false);

        var itens = new List<MinhasFeriasItemDto>();
        foreach (var folha in folhasFerias)
        {
            // SO os eventos do PROPRIO servidor nesta folha de ferias — dado-proprio.
            var eventosDoServidor = folha.Eventos
                .Where(evento => evento.ServidorId == servidorId.Value)
                .ToList();
            if (eventosDoServidor.Count == 0)
            {
                continue;
            }

            var proventos = eventosDoServidor
                .Where(evento => evento.Tipo == TipoEvento.Provento)
                .Sum(evento => evento.Valor);
            var descontos = eventosDoServidor
                .Where(evento => evento.Tipo == TipoEvento.Desconto)
                .Sum(evento => evento.Valor);
            var liquido = proventos - descontos;

            itens.Add(new MinhasFeriasItemDto(
                folha.Competencia.ToString(),
                proventos,
                descontos,
                liquido < 0m ? 0m : liquido,
                folha.Situacao.ToString()));
        }

        return new MinhasFeriasDto(servidorId.Value, request.Ano, itens);
    }
}
