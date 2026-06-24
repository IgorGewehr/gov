using System.Globalization;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Dados pessoais da ficha funcional (CPF mascarado — LGPD).</summary>
/// <param name="Cpf">CPF mascarado.</param>
/// <param name="Nome">Nome civil.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
public sealed record FichaDadosPessoais(string Cpf, string Nome, DateOnly DataNascimento);

/// <summary>Vinculo e cargo da ficha funcional.</summary>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="Regime">Regime previdenciario (texto).</param>
/// <param name="Situacao">Situacao atual do vinculo (texto).</param>
/// <param name="CargoId">Cargo provido.</param>
/// <param name="Cargo">Denominacao do cargo (nulo se o cargo nao for resolvido).</param>
/// <param name="TipoCargo">Tipo (natureza) do cargo (texto; nulo se nao resolvido).</param>
/// <param name="Vencimento">Vencimento-base do cargo (nulo se nao resolvido).</param>
/// <param name="Lotacao">Unidade de lotacao do cargo (nulo se nao resolvido).</param>
public sealed record FichaVinculo(
    string Matricula,
    string Regime,
    string Situacao,
    Guid CargoId,
    string? Cargo,
    string? TipoCargo,
    decimal? Vencimento,
    string? Lotacao);

/// <summary>Evento do ciclo de vida do vinculo (timeline da ficha funcional).</summary>
/// <param name="Evento">Nome do marco (Nomeacao/Posse/Exercicio/Estabilidade/Desligamento).</param>
/// <param name="Data">Data do marco.</param>
public sealed record FichaEventoTimeline(string Evento, DateOnly Data);

/// <summary>Linha de historico de folha do servidor na ficha funcional.</summary>
/// <param name="FolhaId">Identificador da folha.</param>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="Tipo">Tipo da folha (Mensal/13o/Ferias/Rescisao).</param>
/// <param name="Situacao">Situacao da folha.</param>
/// <param name="TotalProventos">Soma dos proventos do servidor na folha.</param>
/// <param name="TotalDescontos">Soma dos descontos do servidor na folha.</param>
/// <param name="Liquido">Liquido do servidor na folha (proventos - descontos).</param>
public sealed record FichaFolha(
    Guid FolhaId,
    int Ano,
    int Mes,
    string Tipo,
    string Situacao,
    decimal TotalProventos,
    decimal TotalDescontos,
    decimal Liquido);

/// <summary>Linha de historico de ponto do servidor na ficha funcional.</summary>
/// <param name="ApuracaoId">Identificador da apuracao.</param>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
/// <param name="Situacao">Situacao da apuracao (Aberta/Fechada).</param>
/// <param name="MinutosExtras">Minutos extras apurados.</param>
/// <param name="MinutosFalta">Minutos de falta apurados.</param>
/// <param name="SaldoBancoHorasMinutos">Saldo do banco de horas apos a apuracao.</param>
public sealed record FichaPonto(
    Guid ApuracaoId,
    int Ano,
    int Mes,
    string Situacao,
    int MinutosExtras,
    int MinutosFalta,
    int SaldoBancoHorasMinutos);

/// <summary>Dependente listado na ficha funcional.</summary>
/// <param name="Nome">Nome civil do dependente.</param>
/// <param name="Parentesco">Grau de parentesco.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="ElegivelIrrf">Indica se o dependente e dedutivel do IRRF (Lei 9.250/1995 art. 35).</param>
public sealed record FichaDependente(string Nome, string Parentesco, DateOnly DataNascimento, bool ElegivelIrrf);

/// <summary>
/// FICHA FUNCIONAL completa do servidor (navegabilidade — Onda 0): dados pessoais + vinculo/cargo +
/// timeline do ciclo de vida + dependentes + historico do que existe (folhas e ponto). CPF mascarado (LGPD).
/// </summary>
/// <param name="Id">Identificador do servidor.</param>
/// <param name="DadosPessoais">Dados civis (CPF mascarado).</param>
/// <param name="Vinculo">Vinculo e cargo.</param>
/// <param name="Timeline">Marcos do ciclo de vida do vinculo (ordem cronologica).</param>
/// <param name="Dependentes">Dependentes registrados.</param>
/// <param name="Folhas">Historico de folhas em que o servidor aparece (mais recente primeiro).</param>
/// <param name="Ponto">Historico de apuracoes de ponto (mais recente primeiro).</param>
public sealed record FichaFuncional(
    Guid Id,
    FichaDadosPessoais DadosPessoais,
    FichaVinculo Vinculo,
    IReadOnlyList<FichaEventoTimeline> Timeline,
    IReadOnlyList<FichaDependente> Dependentes,
    IReadOnlyList<FichaFolha> Folhas,
    IReadOnlyList<FichaPonto> Ponto);

/// <summary>
/// Obtem a ficha funcional completa de um servidor (tenant-scoped via Global Query Filter; read-only).
/// </summary>
/// <param name="ServidorId">Servidor a consultar.</param>
public sealed record ObterFichaFuncionalQuery(Guid ServidorId) : IQuery<FichaFuncional?>;

/// <summary>Handler da ficha funcional: agrega servidor, cargo, timeline e historicos de folha/ponto.</summary>
public sealed class ObterFichaFuncionalHandler(
    IServidorRepository servidores,
    ICargoRepository cargos,
    IFolhaDePagamentoRepository folhas,
    IApuracaoPontoRepository apuracoes)
    : IQueryHandler<ObterFichaFuncionalQuery, FichaFuncional?>
{
    /// <inheritdoc />
    public async Task<FichaFuncional?> Handle(ObterFichaFuncionalQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores
            .ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken)
            .ConfigureAwait(false);
        if (servidor is null)
        {
            return null;
        }

        var cargo = await cargos.ObterPorIdAsync(servidor.CargoId, cancellationToken).ConfigureAwait(false);
        var historicoFolhas = await folhas.ListarPorServidorAsync(servidor.Id.Value, cancellationToken).ConfigureAwait(false);
        var historicoPonto = await apuracoes.ListarPorServidorAsync(servidor.Id.Value, cancellationToken).ConfigureAwait(false);

        var dadosPessoais = new FichaDadosPessoais(
            MascararCpf(servidor.Cpf),
            servidor.DadosPessoais.Nome,
            servidor.DadosPessoais.DataNascimento);

        var vinculo = new FichaVinculo(
            servidor.Matricula.Valor,
            servidor.Regime.ToString(),
            servidor.Situacao.ToString(),
            servidor.CargoId.Value,
            cargo?.Denominacao,
            cargo?.Tipo.ToString(),
            cargo?.Vencimento.Valor,
            cargo?.Lotacao.DenominacaoUnidade);

        var timeline = MontarTimeline(servidor);
        var dependentes = servidor.Dependentes
            .Select(d => new FichaDependente(d.Nome, d.Parentesco, d.DataNascimento, d.ElegivelIrrf))
            .ToList();

        var fichaFolhas = historicoFolhas
            .Select(folha => ProjetarFolha(folha, servidor.Id.Value))
            .ToList();

        var fichaPonto = historicoPonto
            .Select(apuracao => new FichaPonto(
                apuracao.Id.Value,
                apuracao.Competencia.Ano,
                apuracao.Competencia.Mes,
                apuracao.Situacao.ToString(),
                apuracao.MinutosExtras,
                apuracao.MinutosFalta,
                apuracao.SaldoBancoHorasAtualMinutos))
            .ToList();

        return new FichaFuncional(
            servidor.Id.Value,
            dadosPessoais,
            vinculo,
            timeline,
            dependentes,
            fichaFolhas,
            fichaPonto);
    }

    private static List<FichaEventoTimeline> MontarTimeline(Servidor servidor)
    {
        var marcos = new List<FichaEventoTimeline> { new("Nomeacao", servidor.DataNomeacao) };
        if (servidor.DataPosse is { } posse)
        {
            marcos.Add(new FichaEventoTimeline("Posse", posse));
        }

        if (servidor.DataExercicio is { } exercicio)
        {
            marcos.Add(new FichaEventoTimeline("Exercicio", exercicio));
        }

        if (servidor.DataEstabilidade is { } estabilidade)
        {
            marcos.Add(new FichaEventoTimeline("Estabilidade", estabilidade));
        }

        if (servidor.DataDesligamento is { } desligamento)
        {
            marcos.Add(new FichaEventoTimeline("Desligamento", desligamento));
        }

        return marcos.OrderBy(marco => marco.Data).ToList();
    }

    private static FichaFolha ProjetarFolha(FolhaDePagamento folha, Guid servidorId)
    {
        var eventos = folha.Eventos.Where(evento => evento.ServidorId == servidorId).ToList();
        var proventos = eventos.Where(e => e.Tipo == TipoEvento.Provento).Sum(e => e.Valor);
        var descontos = eventos.Where(e => e.Tipo == TipoEvento.Desconto).Sum(e => e.Valor);
        return new FichaFolha(
            folha.Id.Value,
            folha.Competencia.Ano,
            folha.Competencia.Mes,
            folha.Tipo.ToString(),
            folha.Situacao.ToString(),
            proventos,
            descontos,
            proventos - descontos);
    }

    // Mascara o CPF preservando apenas os tres digitos centrais (LGPD): ***.NNN.***-**.
    private static string MascararCpf(Cpf cpf)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        var digitos = cpf.Digitos;
        return string.Create(CultureInfo.InvariantCulture, $"***.{digitos[3..6]}.***-**");
    }
}
