using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;

/// <summary>
/// Implementacao read-side dos relatorios gerenciais da folha (ONDA3-DESIGN §4.1, sub-onda 3a): projeta
/// sobre <see cref="FolhaDePagamento"/>/<see cref="EventoFolha"/>/<c>Servidor</c>/<see cref="Cargo"/>
/// existentes, respeitando o Global Query Filter por tenant. Sem mutacao.
/// </summary>
/// <remarks>
/// <para>
/// A <c>Competencia</c> e propriedade convertida (Competencia &lt;-&gt; int ordinal) e os eventos sao uma
/// colecao OWNED — agregacoes que dependem desses campos sao feitas em memoria sobre o conjunto ja
/// filtrado por tenant, no mesmo padrao do <c>TabelasLegaisProvider</c>. Os volumes por tenant/competencia
/// (folhas, servidores, cargos de um municipio) sao modestos.
/// </para>
/// <para>
/// FONTE = regime previdenciario (RPPS = efetivo/proprio do ente; RGPS = INSS). UO/secretaria = unidade
/// de lotacao do cargo do servidor (<c>Lotacao.DenominacaoUnidade</c>, mapeada a S-1020). Servidores sem
/// cargo resolvido (ou cargo de outro tenant) caem na UO "(Sem lotacao)" — nunca descartados em silencio.
/// </para>
/// </remarks>
public sealed class RelatoriosFolhaConsulta(
    RecursosHumanosDbContext context,
    IParametrosFolhaProvider parametrosFolha) : IRelatoriosFolhaConsulta
{
    private const string UnidadeNaoResolvida = "(Sem lotacao)";

    private static int Ordinal(int ano, int mes) => (ano * 100) + mes;

    private static int Ordinal(Competencia competencia) => (competencia.Ano * 100) + competencia.Mes;

    private static string Formatar(int ano, int mes) =>
        string.Create(CultureInfo.InvariantCulture, $"{ano:0000}-{mes:00}");

    /// <inheritdoc />
    public async Task<FolhaPorSecretariaView?> ObterFolhaPorSecretariaAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        var folha = await ObterFolhaMensalAsync(ano, mes, cancellationToken).ConfigureAwait(false);
        if (folha is null)
        {
            return null;
        }

        var unidadePorServidor = await MapearUnidadePorServidorAsync(folha, cancellationToken).ConfigureAwait(false);

        var porSecretaria = folha.Eventos
            .GroupBy(e => unidadePorServidor.GetValueOrDefault(e.ServidorId, UnidadeNaoResolvida))
            .Select(g => new LinhaFolhaSecretaria(
                g.Key,
                g.Select(e => e.ServidorId).Distinct().Count(),
                SomarProventos(g),
                SomarDescontos(g),
                Liquido(g)))
            .OrderByDescending(l => l.TotalProventos)
            .ThenBy(l => l.Unidade, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var porFonte = folha.Eventos
            .GroupBy(e => e.RegimePrevidenciario)
            .Select(g => new LinhaFolhaFonte(
                g.Key.ToString(),
                g.Select(e => e.ServidorId).Distinct().Count(),
                SomarProventos(g),
                SomarDescontos(g),
                Liquido(g)))
            .OrderByDescending(l => l.TotalProventos)
            .ToList();

        return new FolhaPorSecretariaView(
            folha.Competencia.ToString(),
            folha.Situacao.ToString(),
            folha.Eventos.Select(e => e.ServidorId).Distinct().Count(),
            SomarProventos(folha.Eventos),
            SomarDescontos(folha.Eventos),
            Liquido(folha.Eventos),
            porSecretaria,
            porFonte);
    }

    /// <inheritdoc />
    public async Task<EvolucaoDespesaPessoalView> ObterEvolucaoDespesaAsync(
        int anoDe, int mesDe, int anoAte, int mesAte, CancellationToken cancellationToken)
    {
        var inicio = Ordinal(anoDe, mesDe);
        var fim = Ordinal(anoAte, mesAte);
        if (inicio > fim)
        {
            (inicio, fim) = (fim, inicio);
            (anoDe, mesDe, anoAte, mesAte) = (anoAte, mesAte, anoDe, mesDe);
        }

        // So a folha MENSAL e despesa de pessoal recorrente comparavel mes a mes (13o/ferias/rescisao
        // tem folha propria e nao compoem a serie mensal — design §1.1).
        var folhas = await context.FolhasDePagamento
            .AsNoTracking()
            .Where(f => f.Tipo == TipoFolha.Mensal)
            .Include(f => f.Eventos)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var serie = folhas
            .Where(f => Ordinal(f.Competencia) >= inicio && Ordinal(f.Competencia) <= fim)
            .OrderBy(f => Ordinal(f.Competencia))
            .Select(f => new PontoEvolucaoDespesa(
                f.Competencia.Ano,
                f.Competencia.Mes,
                f.Competencia.ToString(),
                f.Situacao.ToString(),
                f.Eventos.Select(e => e.ServidorId).Distinct().Count(),
                SomarProventos(f.Eventos),
                SomarDescontos(f.Eventos),
                Liquido(f.Eventos)))
            .ToList();

        var acumulada = serie.Sum(p => p.DespesaBruta);
        var media = serie.Count > 0 ? decimal.Round(acumulada / serie.Count, 2, MidpointRounding.AwayFromZero) : 0m;

        return new EvolucaoDespesaPessoalView(
            Formatar(anoDe, mesDe),
            Formatar(anoAte, mesAte),
            acumulada,
            media,
            serie.Count,
            serie);
    }

    /// <inheritdoc />
    public async Task<MapaCargosView> ObterMapaCargosAsync(CancellationToken cancellationToken)
    {
        var cargos = await context.Cargos
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var linhas = cargos
            .Select(c => new LinhaMapaCargo(
                c.Id.Value,
                c.Denominacao,
                c.Tipo.ToString(),
                c.Situacao.ToString(),
                c.Lotacao.DenominacaoUnidade,
                c.Vencimento.Valor,
                c.QuantidadeVagas,
                c.VagasOcupadas,
                c.VagasDisponiveis))
            .OrderByDescending(l => l.VagasOcupadas)
            .ThenBy(l => l.Denominacao, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var porTipo = cargos
            .GroupBy(c => c.Tipo)
            .Select(g => new TotalMapaCargoPorTipo(
                g.Key.ToString(),
                g.Sum(c => c.QuantidadeVagas),
                g.Sum(c => c.VagasOcupadas),
                g.Sum(c => c.VagasDisponiveis)))
            .OrderBy(t => t.Tipo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MapaCargosView(
            cargos.Sum(c => c.QuantidadeVagas),
            cargos.Sum(c => c.VagasOcupadas),
            cargos.Sum(c => c.VagasDisponiveis),
            porTipo,
            linhas);
    }

    /// <inheritdoc />
    public async Task<DemonstrativoTceView?> ObterDemonstrativoTceAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        var folha = await ObterFolhaMensalAsync(ano, mes, cancellationToken).ConfigureAwait(false);
        if (folha is null)
        {
            return null;
        }

        var unidadePorServidor = await MapearUnidadePorServidorAsync(folha, cancellationToken).ConfigureAwait(false);
        var parametros = await parametrosFolha.ObterAsync(cancellationToken).ConfigureAwait(false);

        // Contribuicao previdenciaria do SEGURADO = descontos das rubricas previdenciarias parametrizadas
        // (INSS/RPPS e suas variantes do 13o). Codigos vem da config do tenant — nunca hardcoded (CLAUDE.md S7).
        var rubricasPrevidenciarias = new HashSet<string>(
            new[]
            {
                parametros.CodigoRubricaInss,
                parametros.CodigoRubricaRpps,
                parametros.CodigoRubricaInss13,
                parametros.CodigoRubricaRpps13,
            }.Where(c => !string.IsNullOrWhiteSpace(c)),
            StringComparer.OrdinalIgnoreCase);

        var contribuicaoSegurado = folha.Eventos
            .Where(e => e.Tipo == TipoEvento.Desconto && rubricasPrevidenciarias.Contains(e.Rubrica.Codigo))
            .Sum(e => e.Valor);

        var porFonte = folha.Eventos
            .GroupBy(e => e.RegimePrevidenciario)
            .Select(g => new DemonstrativoTceFonte(
                g.Key.ToString(),
                g.Select(e => e.ServidorId).Distinct().Count(),
                SomarProventos(g),
                SomarDescontos(g),
                Liquido(g)))
            .OrderByDescending(l => l.TotalProventos)
            .ToList();

        var porSecretaria = folha.Eventos
            .GroupBy(e => unidadePorServidor.GetValueOrDefault(e.ServidorId, UnidadeNaoResolvida))
            .Select(g => new DemonstrativoTceUnidade(
                g.Key,
                g.Select(e => e.ServidorId).Distinct().Count(),
                SomarProventos(g),
                SomarDescontos(g),
                Liquido(g)))
            .OrderByDescending(l => l.TotalProventos)
            .ThenBy(l => l.Unidade, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DemonstrativoTceView(
            folha.Competencia.ToString(),
            folha.Situacao.ToString(),
            folha.Eventos.Select(e => e.ServidorId).Distinct().Count(),
            SomarProventos(folha.Eventos),
            SomarDescontos(folha.Eventos),
            Liquido(folha.Eventos),
            contribuicaoSegurado,
            porFonte,
            porSecretaria);
    }

    // Carrega a folha MENSAL da competencia (com eventos) no tenant atual. Competencia e convertida
    // para int; filtra em memoria pela ordinal (mesmo padrao do TabelasLegaisProvider).
    private async Task<FolhaDePagamento?> ObterFolhaMensalAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        var alvo = Ordinal(ano, mes);
        var folhas = await context.FolhasDePagamento
            .AsNoTracking()
            .Where(f => f.Tipo == TipoFolha.Mensal)
            .Include(f => f.Eventos)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return folhas.Find(f => Ordinal(f.Competencia) == alvo);
    }

    // Resolve a UO/secretaria de cada servidor da folha: Servidor -> CargoId -> Cargo.Lotacao.
    private async Task<Dictionary<Guid, string>> MapearUnidadePorServidorAsync(
        FolhaDePagamento folha, CancellationToken cancellationToken)
    {
        var servidoresIds = folha.Eventos
            .Select(e => new ServidorId(e.ServidorId))
            .Distinct()
            .ToList();
        if (servidoresIds.Count == 0)
        {
            return [];
        }

        var vinculos = await context.Servidores
            .AsNoTracking()
            .Where(s => servidoresIds.Contains(s.Id))
            .Select(s => new { ServidorId = s.Id.Value, s.CargoId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cargosIds = vinculos.Select(v => v.CargoId).Distinct().ToList();
        var cargos = await context.Cargos
            .AsNoTracking()
            .Where(c => cargosIds.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var lotacaoPorCargo = cargos
            .ToDictionary(c => c.Id, c => c.Lotacao.DenominacaoUnidade);

        var resultado = new Dictionary<Guid, string>(vinculos.Count);
        foreach (var vinculo in vinculos)
        {
            resultado[vinculo.ServidorId] = lotacaoPorCargo.TryGetValue(vinculo.CargoId, out var unidade)
                ? unidade
                : UnidadeNaoResolvida;
        }

        return resultado;
    }

    private static decimal SomarProventos(IEnumerable<EventoFolha> eventos) =>
        eventos.Where(e => e.Tipo == TipoEvento.Provento).Sum(e => e.Valor);

    private static decimal SomarDescontos(IEnumerable<EventoFolha> eventos) =>
        eventos.Where(e => e.Tipo == TipoEvento.Desconto).Sum(e => e.Valor);

    // Liquido por grupo (proventos - descontos), com piso zero (irredutibilidade — espelha LiquidoAPagar).
    private static decimal Liquido(IEnumerable<EventoFolha> eventos)
    {
        var lista = eventos as ICollection<EventoFolha> ?? eventos.ToList();
        var liquido = SomarProventos(lista) - SomarDescontos(lista);
        return liquido < 0m ? 0m : liquido;
    }
}
