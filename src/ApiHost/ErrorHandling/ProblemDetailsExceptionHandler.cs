using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.ApiHost.ErrorHandling;

/// <summary>
/// Manipulador GLOBAL de exceções (idiomático .NET 8: <see cref="IExceptionHandler"/>). Captura
/// TODA exceção não tratada, traduz para a borda HTTP RFC 7807 (<c>application/problem+json</c>) via
/// <see cref="MapaExcecaoStatus"/> e garante:
/// <list type="bullet">
///   <item>A resposta NUNCA vaza stack trace nem detalhe sensível (nem em Development).</item>
///   <item>Todo erro carrega um <c>traceId</c> correlacionável ao log (extensão <c>traceId</c>).</item>
///   <item>O detalhe técnico completo (stack incluso) vai SÓ para o log interno (Serilog), enriquecido
///         com <c>CorrelationId</c>/<c>TraceId</c> e <c>TenantId</c> (constituição §11).</item>
/// </list>
/// Vive no ApiHost (Composition Root) — única camada autorizada a conhecer a borda HTTP — sem acoplar
/// módulos entre si.
/// <para>
/// O <see cref="IExceptionHandler"/> é resolvido como SINGLETON pelo ASP.NET Core. Por isso este tipo
/// só pode injetar dependências singleton (<see cref="IProblemDetailsService"/>, <see cref="ILogger{T}"/>).
/// O <see cref="ITenantContext"/> é SCOPED (resolvido por requisição do JWT) e NÃO pode ser injetado no
/// construtor — seria um *captive dependency* e o container (com ValidateScopes em Development) recusaria
/// o boot. Ele é resolvido SOB DEMANDA de <c>HttpContext.RequestServices</c> (escopo da requisição
/// corrente) dentro de <see cref="TryHandleAsync"/>.
/// </para>
/// </summary>
internal sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var mapeamento = MapaExcecaoStatus.Mapear(exception);

        // TraceId correlacionável: o do Activity corrente (W3C) quando houver, senão o do request.
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // LOG INTERNO: detalhe COMPLETO (incl. stack) — nunca vai para o corpo da resposta. Enriquecido
        // com TenantId quando resolvível (não falha o pipeline de erro se o tenant não estiver no escopo).
        // ITenantContext é SCOPED: resolvido do escopo da requisição corrente (HttpContext.RequestServices),
        // não do construtor singleton.
        var tenantId = TentarResolverTenant(httpContext);
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("StatusCode", mapeamento.StatusCode))
        {
            if (mapeamento.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    exception,
                    "Exceção não tratada respondida como {StatusCode} (traceId {TraceId}).",
                    mapeamento.StatusCode,
                    traceId);
            }
            else
            {
                // 4xx esperado (negócio/autorização/validação): WARNING, sem ruído de stack em produção
                // — mas a exceção segue anexada para diagnóstico quando o nível permitir.
                logger.LogWarning(
                    exception,
                    "Exceção de negócio mapeada para {StatusCode} (traceId {TraceId}).",
                    mapeamento.StatusCode,
                    traceId);
            }
        }

        httpContext.Response.StatusCode = mapeamento.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = mapeamento.StatusCode,
            Title = mapeamento.Titulo,
            Detail = mapeamento.Detalhe,
            Type = $"https://httpstatuses.io/{mapeamento.StatusCode}",
            Instance = httpContext.Request.Path,
        };

        // traceId SEMPRE presente no corpo → ponte entre a resposta e o log interno.
        problemDetails.Extensions["traceId"] = traceId;

        if (mapeamento.ErrosPorCampo is { Count: > 0 } erros)
        {
            problemDetails.Extensions["errors"] = erros;
        }

        // IProblemDetailsService garante o content-type application/problem+json e os enriquecedores
        // registrados em AddProblemDetails (ver Program.cs).
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        }).ConfigureAwait(false);
    }

    private static string TentarResolverTenant(HttpContext httpContext)
    {
        try
        {
            var tenantContext = httpContext.RequestServices.GetService<ITenantContext>();
            if (tenantContext is null)
            {
                return "(indisponivel)";
            }

            return tenantContext.HasTenant
                ? tenantContext.TenantId.ToString()
                : "(sem-tenant)";
        }
#pragma warning disable CA1031 // resolver o tenant NUNCA pode derrubar o tratamento de erro.
        catch (Exception)
#pragma warning restore CA1031
        {
            return "(indisponivel)";
        }
    }
}
