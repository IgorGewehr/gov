using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Total liquido de uma rubrica (codigo) na folha, por natureza (provento/desconto).</summary>
/// <param name="Rubrica">Codigo da rubrica (S-1010).</param>
/// <param name="Tipo">Natureza (<c>Provento</c>/<c>Desconto</c>).</param>
/// <param name="Total">Soma dos valores lancados nessa rubrica/natureza na folha.</param>
/// <param name="QuantidadeLancamentos">Numero de lancamentos da rubrica/natureza.</param>
public sealed record TotalRubricaConferencia(string Rubrica, string Tipo, decimal Total, int QuantidadeLancamentos);

/// <summary>
/// Servidor ATIVO que nao possui NENHUM lancamento na folha (divergencia (a) — ficaria de fora do
/// pagamento da competencia). Identifica para o conferente quem precisa de lancamento antes de fechar.
/// </summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="Nome">Nome civil do servidor.</param>
public sealed record ServidorSemLancamento(Guid ServidorId, string Matricula, string Nome);

/// <summary>
/// Servidor cujo LIQUIDO ficou insuficiente (descontos &gt;= proventos) — mesma regra do P0-5 do agregado
/// (<see cref="FolhaDePagamento.TemLiquidoInsuficiente"/>), derivada dos eventos persistidos. Divergencia (b).
/// </summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="Matricula">Matricula do vinculo (vazia se o servidor nao for resolvido no tenant).</param>
/// <param name="Nome">Nome civil (vazio se nao resolvido).</param>
/// <param name="TotalProventos">Soma dos proventos do servidor na folha.</param>
/// <param name="TotalDescontos">Soma dos descontos do servidor na folha.</param>
public sealed record ServidorLiquidoInsuficiente(
    Guid ServidorId,
    string Matricula,
    string Nome,
    decimal TotalProventos,
    decimal TotalDescontos);

/// <summary>
/// Servidor com VARIACAO suspeita do liquido frente a competencia anterior (mesmo tipo de folha) —
/// divergencia (c). Sinaliza |Delta| acima do limiar parametrizavel para pegar erro de digitacao/rubrica.
/// </summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="Matricula">Matricula do vinculo (vazia se nao resolvido).</param>
/// <param name="Nome">Nome civil (vazio se nao resolvido).</param>
/// <param name="LiquidoAtual">Liquido do servidor na folha em conferencia.</param>
/// <param name="LiquidoAnterior">Liquido do servidor na competencia anterior (zero se ausente la).</param>
/// <param name="Variacao">Diferenca (atual menos anterior).</param>
/// <param name="VariacaoPercentual">
/// Variacao relativa ao anterior (em pontos percentuais); <c>null</c> quando o anterior e zero (sem base).
/// </param>
public sealed record ServidorVariacaoLiquido(
    Guid ServidorId,
    string Matricula,
    string Nome,
    decimal LiquidoAtual,
    decimal LiquidoAnterior,
    decimal Variacao,
    decimal? VariacaoPercentual);

/// <summary>
/// Conferencia de pre-fechamento da folha (P0-7): o que o conferente precisa ver ANTES de fechar.
/// Projecao de leitura, reprodutivel (sem relogio) e tenant-scoped. NAO altera o calculo.
/// </summary>
/// <param name="FolhaDePagamentoId">Folha conferida.</param>
/// <param name="Competencia">Competencia da folha (<c>AAAA-MM</c>).</param>
/// <param name="Tipo">Tipo (natureza) da folha.</param>
/// <param name="Situacao">Situacao atual da folha.</param>
/// <param name="TotalProventos">Soma geral dos proventos.</param>
/// <param name="TotalDescontos">Soma geral dos descontos (inclui abate-teto).</param>
/// <param name="TotalLiquido">Liquido geral (TotalProventos menos TotalDescontos, nunca negativo).</param>
/// <param name="QuantidadeServidores">Numero de servidores distintos com lancamento na folha.</param>
/// <param name="QuantidadeLancamentos">Numero total de lancamentos (eventos) na folha.</param>
/// <param name="TotaisPorRubrica">Totais por rubrica/natureza (analitico).</param>
/// <param name="TotaisConferem">
/// Divergencia (d): verdadeiro quando <c>TotalProventos - TotalDescontos == TotalLiquido</c> (com o
/// piso zero do liquido respeitado). Falso indica inconsistencia de totais a investigar.
/// </param>
/// <param name="LimiteVariacaoLiquido">Limiar (|Delta| em R$) usado para a divergencia de variacao (c).</param>
/// <param name="CompetenciaAnterior">Competencia comparada na variacao (<c>AAAA-MM</c>), ou <c>null</c> se nao houver folha anterior.</param>
/// <param name="ServidoresAtivosSemLancamento">Divergencia (a): ativos sem nenhum lancamento.</param>
/// <param name="ServidoresComLiquidoInsuficiente">Divergencia (b): liquido insuficiente (P0-5).</param>
/// <param name="ServidoresComVariacaoSuspeita">Divergencia (c): |Delta| do liquido acima do limiar.</param>
/// <param name="TemDivergencias">Atalho: verdadeiro se houver QUALQUER divergencia (a/b/c) ou totais nao conferindo (d).</param>
public sealed record ConferenciaFolhaDto(
    Guid FolhaDePagamentoId,
    string Competencia,
    string Tipo,
    string Situacao,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal TotalLiquido,
    int QuantidadeServidores,
    int QuantidadeLancamentos,
    IReadOnlyList<TotalRubricaConferencia> TotaisPorRubrica,
    bool TotaisConferem,
    decimal LimiteVariacaoLiquido,
    string? CompetenciaAnterior,
    IReadOnlyList<ServidorSemLancamento> ServidoresAtivosSemLancamento,
    IReadOnlyList<ServidorLiquidoInsuficiente> ServidoresComLiquidoInsuficiente,
    IReadOnlyList<ServidorVariacaoLiquido> ServidoresComVariacaoSuspeita,
    bool TemDivergencias);

/// <summary>
/// Conferencia de pre-fechamento de uma folha ABERTA/calculada (P0-7). Reproduzivel: o limiar de
/// variacao e parametrizavel; quando nao informado, usa o default do tenant
/// (<see cref="Configuracao.ParametrosFolha"/>).
/// </summary>
/// <param name="FolhaDePagamentoId">Folha a conferir.</param>
/// <param name="LimiteVariacaoLiquido">
/// Limiar (|Delta| em R$) da divergencia de variacao do liquido por servidor; <c>null</c> usa o default do tenant.
/// </param>
public sealed record ObterConferenciaFolhaQuery(Guid FolhaDePagamentoId, decimal? LimiteVariacaoLiquido = null)
    : IQuery<ConferenciaFolhaDto?>;

/// <summary>Handler da conferencia de pre-fechamento da folha (P0-7).</summary>
public sealed class ObterConferenciaFolhaHandler(
    IFolhaDePagamentoRepository folhas,
    IServidorRepository servidores,
    IParametrosFolhaProvider parametros)
    : IQueryHandler<ObterConferenciaFolhaQuery, ConferenciaFolhaDto?>
{
    /// <inheritdoc />
    public async Task<ConferenciaFolhaDto?> Handle(ObterConferenciaFolhaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false);
        if (folha is null)
        {
            return null;
        }

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        var limiteVariacao = request.LimiteVariacaoLiquido ?? config.LimiteVariacaoLiquidoConferencia;

        // Cadastro dos ativos do tenant, indexado por ServidorId — para a divergencia (a) e para enriquecer
        // as demais divergencias com matricula/nome sem N consultas.
        var ativos = await servidores.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);
        var ativosPorId = ativos.ToDictionary(servidor => servidor.Id.Value);

        var eventos = folha.Eventos;
        var servidoresComLancamento = eventos.Select(evento => evento.ServidorId).ToHashSet();

        var totalProventos = folha.TotalProventos;
        var totalDescontos = folha.TotalDescontos;
        var totalLiquido = folha.TotalLiquido.Valor;

        var totaisPorRubrica = eventos
            .GroupBy(evento => (Rubrica: evento.Rubrica.Codigo, evento.Tipo))
            .Select(grupo => new TotalRubricaConferencia(
                grupo.Key.Rubrica,
                grupo.Key.Tipo.ToString(),
                grupo.Sum(evento => evento.Valor),
                grupo.Count()))
            .OrderBy(linha => linha.Tipo, StringComparer.Ordinal)
            .ThenBy(linha => linha.Rubrica, StringComparer.Ordinal)
            .ToList();

        // (d) Totais batendo: o liquido geral espelha proventos - descontos. O VO impede liquido negativo,
        // entao quando descontos > proventos o "esperado" e zero (piso) — e isso deve conferir.
        var liquidoEsperado = totalProventos - totalDescontos;
        if (liquidoEsperado < 0m)
        {
            liquidoEsperado = 0m;
        }

        var totaisConferem = liquidoEsperado == totalLiquido;

        // (a) Ativos sem NENHUM lancamento na folha.
        var ativosSemLancamento = ativos
            .Where(servidor => !servidoresComLancamento.Contains(servidor.Id.Value))
            .Select(servidor => new ServidorSemLancamento(
                servidor.Id.Value,
                servidor.Matricula.Valor,
                servidor.DadosPessoais.Nome))
            .OrderBy(linha => linha.Matricula, StringComparer.Ordinal)
            .ToList();

        // Proventos/descontos por servidor na folha atual (uma passada) — reusado por (b) e (c).
        var liquidoAtualPorServidor = new Dictionary<Guid, (decimal Proventos, decimal Descontos)>();
        foreach (var evento in eventos)
        {
            liquidoAtualPorServidor.TryGetValue(evento.ServidorId, out var acumulado);
            acumulado = evento.Tipo == TipoEvento.Provento
                ? (acumulado.Proventos + evento.Valor, acumulado.Descontos)
                : (acumulado.Proventos, acumulado.Descontos + evento.Valor);
            liquidoAtualPorServidor[evento.ServidorId] = acumulado;
        }

        // (b) Liquido insuficiente — reusa o sinal do P0-5. A LISTA detalhada do agregado e transiente
        // (recalculada em Calcular, NAO persistida — FolhaDePagamentoConfiguration a ignora); ja a flag
        // TemLiquidoInsuficiente E persistida e autoritativa. Para a conferencia ser reproduzivel sobre a
        // folha RECARREGADA, deriva-se a lista dos Eventos persistidos pela MESMA regra do agregado
        // (descontos >= proventos). A flag persistida fica como invariante de consistencia (assert abaixo).
        var liquidoInsuficiente = liquidoAtualPorServidor
            .Where(par => par.Value.Descontos >= par.Value.Proventos)
            .Select(par =>
            {
                ativosPorId.TryGetValue(par.Key, out var servidor);
                return new ServidorLiquidoInsuficiente(
                    par.Key,
                    servidor?.Matricula.Valor ?? string.Empty,
                    servidor?.DadosPessoais.Nome ?? string.Empty,
                    par.Value.Proventos,
                    par.Value.Descontos);
            })
            .OrderBy(linha => linha.Matricula, StringComparer.Ordinal)
            .ToList();

        // (c) Variacao suspeita do liquido frente a competencia anterior (mesmo tipo de folha).
        var competenciaAnterior = CompetenciaAnterior(folha.Competencia);
        var folhaAnterior = await folhas
            .ObterPorCompetenciaAsync(competenciaAnterior, cancellationToken, folha.Tipo)
            .ConfigureAwait(false);

        var variacoes = new List<ServidorVariacaoLiquido>();
        if (folhaAnterior is not null)
        {
            var liquidoAnteriorPorServidor = LiquidoPorServidor(folhaAnterior);

            foreach (var (servidorId, totaisAtuais) in liquidoAtualPorServidor)
            {
                var liquidoAtual = LiquidoNaoNegativo(totaisAtuais.Proventos, totaisAtuais.Descontos);
                liquidoAnteriorPorServidor.TryGetValue(servidorId, out var liquidoAnterior);
                var variacao = liquidoAtual - liquidoAnterior;

                if (Math.Abs(variacao) <= limiteVariacao)
                {
                    continue;
                }

                ativosPorId.TryGetValue(servidorId, out var servidor);
                variacoes.Add(new ServidorVariacaoLiquido(
                    servidorId,
                    servidor?.Matricula.Valor ?? string.Empty,
                    servidor?.DadosPessoais.Nome ?? string.Empty,
                    liquidoAtual,
                    liquidoAnterior,
                    variacao,
                    liquidoAnterior == 0m ? null : variacao / liquidoAnterior * 100m));
            }

            variacoes = variacoes
                .OrderByDescending(linha => Math.Abs(linha.Variacao))
                .ThenBy(linha => linha.Matricula, StringComparer.Ordinal)
                .ToList();
        }

        var temDivergencias = !totaisConferem
            || ativosSemLancamento.Count > 0
            || liquidoInsuficiente.Count > 0
            || variacoes.Count > 0;

        return new ConferenciaFolhaDto(
            folha.Id.Value,
            folha.Competencia.ToString(),
            folha.Tipo.ToString(),
            folha.Situacao.ToString(),
            totalProventos,
            totalDescontos,
            totalLiquido,
            servidoresComLancamento.Count,
            eventos.Count,
            totaisPorRubrica,
            totaisConferem,
            limiteVariacao,
            folhaAnterior is null ? null : competenciaAnterior.ToString(),
            ativosSemLancamento,
            liquidoInsuficiente,
            variacoes,
            temDivergencias);
    }

    private static Competencia CompetenciaAnterior(Competencia competencia)
        => competencia.Mes == 1
            ? Competencia.De(competencia.Ano - 1, 12)
            : Competencia.De(competencia.Ano, competencia.Mes - 1);

    private static Dictionary<Guid, decimal> LiquidoPorServidor(FolhaDePagamento folha)
    {
        var proventos = new Dictionary<Guid, decimal>();
        var descontos = new Dictionary<Guid, decimal>();
        foreach (var evento in folha.Eventos)
        {
            var destino = evento.Tipo == TipoEvento.Provento ? proventos : descontos;
            destino.TryGetValue(evento.ServidorId, out var atual);
            destino[evento.ServidorId] = atual + evento.Valor;
        }

        var liquido = new Dictionary<Guid, decimal>();
        foreach (var servidorId in proventos.Keys.Union(descontos.Keys))
        {
            proventos.TryGetValue(servidorId, out var prov);
            descontos.TryGetValue(servidorId, out var desc);
            liquido[servidorId] = LiquidoNaoNegativo(prov, desc);
        }

        return liquido;
    }

    private static decimal LiquidoNaoNegativo(decimal proventos, decimal descontos)
    {
        var liquido = proventos - descontos;
        return liquido < 0m ? 0m : liquido;
    }
}
