using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;

/// <summary>
/// Informe de rendimentos (Comprovante Anual de Rendimentos — IN RFB) do PROPRIO servidor num
/// ano-calendario: rendimentos tributaveis, previdencia oficial e IRRF retido, somados sobre as
/// folhas mensais e de 13o do ano. E uma PROJECAO de leitura para o autosservico — nao substitui a
/// DIRF/eSocial oficiais (// TODO).
/// </summary>
/// <param name="ServidorId">Servidor (o proprio usuario).</param>
/// <param name="AnoCalendario">Ano-calendario de referencia.</param>
/// <param name="RendimentosTributaveis">Soma dos proventos (exclui as proprias retencoes legais).</param>
/// <param name="PrevidenciaOficial">Soma de INSS/RPPS retidos (deducao da base — previdencia oficial).</param>
/// <param name="ImpostoRetidoNaFonte">Soma do IRRF retido no ano (mensal + 13o).</param>
public sealed record MeuInformeRendimentosDto(
    Guid ServidorId,
    int AnoCalendario,
    decimal RendimentosTributaveis,
    decimal PrevidenciaOficial,
    decimal ImpostoRetidoNaFonte);

/// <summary>
/// AUTOSSERVICO: obtem o informe de rendimentos anual do PROPRIO usuario autenticado. O ServidorId
/// NUNCA vem do cliente — e resolvido do vinculo do usuario autenticado. Dado pessoal (LGPD):
/// implementa <see cref="ISensivelLgpd"/> e gera trilha de acesso (LG-2). Base legal predominante:
/// obrigacao legal (o empregador fornecer o comprovante de rendimentos).
/// </summary>
/// <param name="AnoCalendario">Ano-calendario de referencia.</param>
public sealed record ObterMeuInformeDeRendimentosQuery(int AnoCalendario)
    : IQuery<MeuInformeRendimentosDto>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MeuInformeDeRendimentos";

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ObrigacaoLegal;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisAutosservico.Aplicaveis;
}

/// <summary>Handler do informe de rendimentos proprio.</summary>
public sealed class ObterMeuInformeDeRendimentosHandler(
    IResolvedorServidorDoUsuarioAutenticado resolvedor,
    IFolhaDePagamentoRepository folhas,
    IParametrosFolhaProvider parametros)
    : IQueryHandler<ObterMeuInformeDeRendimentosQuery, MeuInformeRendimentosDto>
{
    /// <inheritdoc />
    public async Task<MeuInformeRendimentosDto> Handle(ObterMeuInformeDeRendimentosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = await resolvedor.ResolverServidorAtualAsync(cancellationToken).ConfigureAwait(false);

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);

        // Rubricas que sao PREVIDENCIA OFICIAL (INSS/RPPS, mensal e 13o) e IRRF (mensal e 13o),
        // parametrizadas por tenant (nada hardcoded).
        var rubricasPrevidencia = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            config.CodigoRubricaInss,
            config.CodigoRubricaRpps,
            config.CodigoRubricaInss13,
            config.CodigoRubricaRpps13,
        };
        var rubricasIrrf = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            config.CodigoRubricaIrrf,
            config.CodigoRubricaIrrf13,
        };

        // Informe consolida folha MENSAL + 13o do ano (ferias/rescisao tem informe proprio; // TODO).
        var mensais = await folhas.ListarPorTipoEAnoAsync(request.AnoCalendario, TipoFolha.Mensal, cancellationToken).ConfigureAwait(false);
        var decimos = await folhas.ListarPorTipoEAnoAsync(request.AnoCalendario, TipoFolha.DecimoTerceiro, cancellationToken).ConfigureAwait(false);

        decimal rendimentos = 0m;
        decimal previdencia = 0m;
        decimal irrf = 0m;

        foreach (var folha in mensais.Concat(decimos))
        {
            // SO os eventos do PROPRIO servidor — dado-proprio.
            foreach (var evento in folha.Eventos.Where(e => e.ServidorId == servidorId.Value))
            {
                var codigo = evento.Rubrica.Codigo;
                if (evento.Tipo == TipoEvento.Provento)
                {
                    rendimentos += evento.Valor;
                }
                else if (rubricasIrrf.Contains(codigo))
                {
                    irrf += evento.Valor;
                }
                else if (rubricasPrevidencia.Contains(codigo))
                {
                    previdencia += evento.Valor;
                }
            }
        }

        return new MeuInformeRendimentosDto(
            servidorId.Value,
            request.AnoCalendario,
            rendimentos,
            previdencia,
            irrf);
    }
}
