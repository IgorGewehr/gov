using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;

/// <summary>Item publico de despesa (DTO de saida — read-only, sem PII alem do permitido).</summary>
public sealed record DespesaPublicaDto(
    int Exercicio, string Fase, string? NumeroEmpenho, string? Credor, string? CredorDocMascarado,
    string? FuncaoSubfuncao, string? FonteRecurso, decimal Valor, DateOnly Data);

/// <summary>Item publico de receita.</summary>
public sealed record ReceitaPublicaDto(int Exercicio, string? Rubrica, string? FonteRecurso, decimal Valor, DateOnly Data);

/// <summary>Item publico de contrato.</summary>
public sealed record ContratoPublicoDto(
    int Exercicio, string? NumeroContrato, string? Fornecedor, string? Objeto, decimal Valor,
    string? Modalidade, string? NumeroContratoPncp);

/// <summary>Item publico de folha nominal (SEM CPF/matricula por construcao).</summary>
public sealed record FolhaNominalPublicaDto(
    string Competencia, string ServidorNome, string? Cargo, string? Lotacao,
    decimal RemuneracaoBruta, decimal Descontos, decimal Liquido);

/// <summary>PUBLICA: pagina despesas (transparencia ativa). Tenant ja fixado pelo endpoint (slug).</summary>
public sealed record ConsultarDespesasPublicasQuery(
    int? Exercicio, FaseDespesa? Fase, string? FuncaoSubfuncao, string? FonteRecurso, string? Credor,
    int Pagina, int Tamanho) : IQuery<PaginaResultado<DespesaPublicaDto>>;

/// <summary>Handler das despesas publicas.</summary>
public sealed class ConsultarDespesasPublicasHandler(IConsultaPublicaRepository repositorio)
    : IQueryHandler<ConsultarDespesasPublicasQuery, PaginaResultado<DespesaPublicaDto>>
{
    /// <inheritdoc />
    public async Task<PaginaResultado<DespesaPublicaDto>> Handle(ConsultarDespesasPublicasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var paginacao = PaginacaoPublicaHelper.Normalizar(request.Pagina, request.Tamanho);
        var pagina = await repositorio.ConsultarDespesasAsync(
            request.Exercicio, request.Fase, request.FuncaoSubfuncao, request.FonteRecurso, request.Credor, paginacao, cancellationToken)
            .ConfigureAwait(false);

        var itens = pagina.Itens.Select(d => new DespesaPublicaDto(
            d.Exercicio, d.Fase.ToString(), d.NumeroEmpenho, d.CredorNomeOuRazao, d.CredorDocMascarado,
            d.FuncaoSubfuncao, d.FonteRecurso, d.Valor, d.Data)).ToList();
        return new PaginaResultado<DespesaPublicaDto>(itens, pagina.Pagina, pagina.Tamanho, pagina.TemProxima);
    }
}

/// <summary>PUBLICA: pagina receitas.</summary>
public sealed record ConsultarReceitasPublicasQuery(int? Exercicio, string? Rubrica, string? FonteRecurso, int Pagina, int Tamanho)
    : IQuery<PaginaResultado<ReceitaPublicaDto>>;

/// <summary>Handler das receitas publicas.</summary>
public sealed class ConsultarReceitasPublicasHandler(IConsultaPublicaRepository repositorio)
    : IQueryHandler<ConsultarReceitasPublicasQuery, PaginaResultado<ReceitaPublicaDto>>
{
    /// <inheritdoc />
    public async Task<PaginaResultado<ReceitaPublicaDto>> Handle(ConsultarReceitasPublicasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var paginacao = PaginacaoPublicaHelper.Normalizar(request.Pagina, request.Tamanho);
        var pagina = await repositorio.ConsultarReceitasAsync(request.Exercicio, request.Rubrica, request.FonteRecurso, paginacao, cancellationToken).ConfigureAwait(false);
        var itens = pagina.Itens.Select(r => new ReceitaPublicaDto(r.Exercicio, r.RubricaReceita, r.FonteRecurso, r.Valor, r.Data)).ToList();
        return new PaginaResultado<ReceitaPublicaDto>(itens, pagina.Pagina, pagina.Tamanho, pagina.TemProxima);
    }
}

/// <summary>PUBLICA: pagina contratos.</summary>
public sealed record ConsultarContratosPublicosQuery(int? Exercicio, string? Fornecedor, int Pagina, int Tamanho)
    : IQuery<PaginaResultado<ContratoPublicoDto>>;

/// <summary>Handler dos contratos publicos.</summary>
public sealed class ConsultarContratosPublicosHandler(IConsultaPublicaRepository repositorio)
    : IQueryHandler<ConsultarContratosPublicosQuery, PaginaResultado<ContratoPublicoDto>>
{
    /// <inheritdoc />
    public async Task<PaginaResultado<ContratoPublicoDto>> Handle(ConsultarContratosPublicosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var paginacao = PaginacaoPublicaHelper.Normalizar(request.Pagina, request.Tamanho);
        var pagina = await repositorio.ConsultarContratosAsync(request.Exercicio, request.Fornecedor, paginacao, cancellationToken).ConfigureAwait(false);
        var itens = pagina.Itens.Select(c => new ContratoPublicoDto(
            c.Exercicio, c.NumeroContrato, c.Fornecedor, c.Objeto, c.Valor, c.Modalidade, c.NumeroContratoPncp)).ToList();
        return new PaginaResultado<ContratoPublicoDto>(itens, pagina.Pagina, pagina.Tamanho, pagina.TemProxima);
    }
}

/// <summary>PUBLICA: pagina a folha nominal (SEM CPF/matricula).</summary>
public sealed record ConsultarFolhaPublicaQuery(string? Competencia, string? Lotacao, int Pagina, int Tamanho)
    : IQuery<PaginaResultado<FolhaNominalPublicaDto>>;

/// <summary>Handler da folha nominal publica.</summary>
public sealed class ConsultarFolhaPublicaHandler(IConsultaPublicaRepository repositorio)
    : IQueryHandler<ConsultarFolhaPublicaQuery, PaginaResultado<FolhaNominalPublicaDto>>
{
    /// <inheritdoc />
    public async Task<PaginaResultado<FolhaNominalPublicaDto>> Handle(ConsultarFolhaPublicaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var paginacao = PaginacaoPublicaHelper.Normalizar(request.Pagina, request.Tamanho);
        var pagina = await repositorio.ConsultarFolhaAsync(request.Competencia, request.Lotacao, paginacao, cancellationToken).ConfigureAwait(false);
        // Projecao publica: nao ha campo de CPF/matricula no read model — impossivel vazar por aqui.
        var itens = pagina.Itens.Select(f => new FolhaNominalPublicaDto(
            f.Competencia, f.ServidorNome, f.CargoDescricao, f.Lotacao, f.RemuneracaoBruta, f.Descontos, f.Liquido)).ToList();
        return new PaginaResultado<FolhaNominalPublicaDto>(itens, pagina.Pagina, pagina.Tamanho, pagina.TemProxima);
    }
}

/// <summary>PUBLICA: resumo fiscal do exercicio (receita x despesa por fase) — consulta em tempo real.</summary>
public sealed record ConsultarResumoFiscalPublicoQuery(int Exercicio) : IQuery<ResumoFiscalPublico>;

/// <summary>Handler do resumo fiscal.</summary>
public sealed class ConsultarResumoFiscalPublicoHandler(IConsultaPublicaRepository repositorio)
    : IQueryHandler<ConsultarResumoFiscalPublicoQuery, ResumoFiscalPublico>
{
    /// <inheritdoc />
    public Task<ResumoFiscalPublico> Handle(ConsultarResumoFiscalPublicoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return repositorio.ConsultarResumoFiscalAsync(request.Exercicio, cancellationToken);
    }
}

/// <summary>Normaliza a paginacao publica com limites defensivos (anti-abuso).</summary>
internal static class PaginacaoPublicaHelper
{
    private const int TamanhoMaximo = 200;
    private const int TamanhoPadrao = 50;

    /// <summary>Clampa pagina (&gt;= 1) e tamanho (1..200).</summary>
    /// <param name="pagina">Pagina solicitada.</param>
    /// <param name="tamanho">Tamanho solicitado.</param>
    /// <returns>Paginacao normalizada.</returns>
    public static PaginacaoPublica Normalizar(int pagina, int tamanho)
    {
        var p = pagina < 1 ? 1 : pagina;
        var t = tamanho <= 0 ? TamanhoPadrao : Math.Min(tamanho, TamanhoMaximo);
        return new PaginacaoPublica(p, t);
    }
}
