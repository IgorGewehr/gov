using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Resolve o <c>Cod.Siconfi</c> do ente (IBGE + <c>"EX"</c>) a partir da configuração parametrizada por
/// tenant/exercício (chave <c>Transparencia:Siconfi:CodigoIbge</c>). Em produção o IBGE vem do cadastro do
/// ente; aqui é dirigido por configuração (nunca <i>hardcoded</i> — CLAUDE.md §7). Maximiliano de Almeida/RS
/// = IBGE 4312104 (default determinístico de dev).
/// </summary>
public sealed class IdentificacaoEnteSiconfiConfiguracao(IConfiguration configuration) : IIdentificacaoEnteSiconfi
{
    // IBGE de Maximiliano de Almeida/RS (default de dev; parametrizavel por tenant em producao).
    private const string CodigoIbgePadrao = "4312104";
    private const string SufixoExecutivo = "EX";

    /// <inheritdoc />
    public Task<string> ObterCodigoSiconfiAsync(CancellationToken cancellationToken)
    {
        var ibge = configuration["Transparencia:Siconfi:CodigoIbge"] ?? CodigoIbgePadrao;
        var codigo = ibge.Trim().EndsWith(SufixoExecutivo, StringComparison.OrdinalIgnoreCase)
            ? ibge.Trim()
            : ibge.Trim() + SufixoExecutivo;
        return Task.FromResult(codigo);
    }
}
