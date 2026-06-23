using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;

/// <summary>Espelho de ponto do PROPRIO servidor numa competencia (apuracao da jornada — PTRP).</summary>
/// <param name="ServidorId">Servidor do espelho (o proprio usuario).</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="MinutosTrabalhados">Minutos efetivamente trabalhados.</param>
/// <param name="MinutosDevidos">Minutos devidos (jornada contratada).</param>
/// <param name="MinutosExtras">Minutos de hora extra apurados.</param>
/// <param name="MinutosFalta">Minutos de falta/atraso apurados.</param>
/// <param name="SaldoBancoHorasMinutos">Saldo acumulado do banco de horas apos a competencia.</param>
/// <param name="Fechada">Indica se a apuracao (espelho) ja esta fechada/congelada.</param>
public sealed record MeuEspelhoDePontoDto(
    Guid ServidorId,
    string Competencia,
    int MinutosTrabalhados,
    int MinutosDevidos,
    int MinutosExtras,
    int MinutosFalta,
    int SaldoBancoHorasMinutos,
    bool Fechada);

/// <summary>
/// AUTOSSERVICO: obtem o espelho de ponto (apuracao de jornada) do PROPRIO usuario autenticado numa
/// competencia. O ServidorId NUNCA vem do cliente — e resolvido do vinculo do usuario autenticado.
/// Dado pessoal (LGPD): implementa <see cref="ISensivelLgpd"/> e gera trilha de acesso (LG-2).
/// </summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record ObterMeuEspelhoDePontoQuery(int Ano, int Mes)
    : IQuery<MeuEspelhoDePontoDto?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MeuEspelhoDePonto";

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisAutosservico.Aplicaveis;
}

/// <summary>Handler do espelho de ponto proprio.</summary>
public sealed class ObterMeuEspelhoDePontoHandler(
    IResolvedorServidorDoUsuarioAutenticado resolvedor,
    IApuracaoPontoRepository apuracoes)
    : IQueryHandler<ObterMeuEspelhoDePontoQuery, MeuEspelhoDePontoDto?>
{
    /// <inheritdoc />
    public async Task<MeuEspelhoDePontoDto?> Handle(ObterMeuEspelhoDePontoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = await resolvedor.ResolverServidorAtualAsync(cancellationToken).ConfigureAwait(false);

        var competencia = Competencia.De(request.Ano, request.Mes);

        // Consulta a apuracao DO PROPRIO servidor (chave servidor+competencia) — nunca de outro.
        var apuracao = await apuracoes
            .ObterPorServidorCompetenciaAsync(servidorId.Value, competencia, cancellationToken)
            .ConfigureAwait(false);
        if (apuracao is null)
        {
            return null;
        }

        return new MeuEspelhoDePontoDto(
            servidorId.Value,
            apuracao.Competencia.ToString(),
            apuracao.MinutosTrabalhados,
            apuracao.MinutosDevidos,
            apuracao.MinutosExtras,
            apuracao.MinutosFalta,
            apuracao.SaldoBancoHorasAtualMinutos,
            apuracao.Situacao == SituacaoApuracaoPonto.Fechada);
    }
}
