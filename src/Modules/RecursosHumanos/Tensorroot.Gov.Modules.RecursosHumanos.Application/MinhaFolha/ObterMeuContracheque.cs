using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;

/// <summary>Linha (rubrica) de um contracheque do autosservico.</summary>
/// <param name="Rubrica">Codigo da rubrica.</param>
/// <param name="Tipo">Provento ou desconto.</param>
/// <param name="Valor">Valor apurado da verba.</param>
public sealed record LinhaMeuContracheque(string Rubrica, string Tipo, decimal Valor);

/// <summary>Contracheque do PROPRIO servidor numa competencia/tipo (projecao de leitura).</summary>
/// <param name="ServidorId">Servidor do contracheque (o proprio usuario).</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="Tipo">Natureza da folha (mensal/13o/ferias/rescisao).</param>
/// <param name="Linhas">Linhas (rubricas) do contracheque.</param>
/// <param name="TotalProventos">Soma dos proventos.</param>
/// <param name="TotalDescontos">Soma dos descontos.</param>
/// <param name="LiquidoAPagar">Liquido a pagar.</param>
public sealed record MeuContrachequeDto(
    Guid ServidorId,
    string Competencia,
    string Tipo,
    IReadOnlyList<LinhaMeuContracheque> Linhas,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal LiquidoAPagar);

/// <summary>
/// AUTOSSERVICO: obtem o contracheque do PROPRIO usuario autenticado numa competencia e tipo de
/// folha (mensal, 13o, ferias, rescisao). O ServidorId NUNCA vem do cliente — e resolvido do
/// vinculo do usuario autenticado no handler (ABAC dado-proprio). Dado pessoal (LGPD): implementa
/// <see cref="ISensivelLgpd"/> e gera trilha de acesso (LG-2).
/// </summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="Tipo">Natureza da folha (default mensal).</param>
public sealed record ObterMeuContrachequeQuery(int Ano, int Mes, TipoFolha Tipo = TipoFolha.Mensal)
    : IQuery<MeuContrachequeDto?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MeuContracheque";

    /// <summary>
    /// Resolvido server-side a partir do usuario autenticado (nao vem do cliente) — por isso nulo
    /// aqui: a trilha registra o recurso e o tenant/usuario/IP do principal.
    /// </summary>
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisAutosservico.Aplicaveis;
}

/// <summary>Handler do contracheque proprio.</summary>
public sealed class ObterMeuContrachequeHandler(
    IResolvedorServidorDoUsuarioAutenticado resolvedor,
    IFolhaDePagamentoRepository folhas)
    : IQueryHandler<ObterMeuContrachequeQuery, MeuContrachequeDto?>
{
    /// <inheritdoc />
    public async Task<MeuContrachequeDto?> Handle(ObterMeuContrachequeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ANCORA: servidor do PROPRIO usuario autenticado — nunca um id do cliente.
        var servidorId = await resolvedor.ResolverServidorAtualAsync(cancellationToken).ConfigureAwait(false);

        var competencia = Competencia.De(request.Ano, request.Mes);
        var folha = await folhas
            .ObterPorCompetenciaAsync(competencia, cancellationToken, request.Tipo)
            .ConfigureAwait(false);
        if (folha is null)
        {
            return null;
        }

        // SO os eventos do PROPRIO servidor — o filtro por servidorId resolvido garante o isolamento
        // dado-proprio mesmo que a folha contenha eventos de toda a competencia.
        var eventosDoServidor = folha.Eventos
            .Where(evento => evento.ServidorId == servidorId.Value)
            .ToList();

        if (eventosDoServidor.Count == 0)
        {
            return null;
        }

        var linhas = eventosDoServidor
            .Select(evento => new LinhaMeuContracheque(evento.Rubrica.Codigo, evento.Tipo.ToString(), evento.Valor))
            .ToList();

        var totalProventos = eventosDoServidor
            .Where(evento => evento.Tipo == TipoEvento.Provento)
            .Sum(evento => evento.Valor);

        var totalDescontos = eventosDoServidor
            .Where(evento => evento.Tipo == TipoEvento.Desconto)
            .Sum(evento => evento.Valor);

        var liquido = totalProventos - totalDescontos;

        return new MeuContrachequeDto(
            servidorId.Value,
            folha.Competencia.ToString(),
            folha.Tipo.ToString(),
            linhas,
            totalProventos,
            totalDescontos,
            liquido < 0m ? 0m : liquido);
    }
}
