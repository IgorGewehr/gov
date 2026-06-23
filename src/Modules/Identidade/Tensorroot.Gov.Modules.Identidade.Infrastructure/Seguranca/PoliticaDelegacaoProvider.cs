using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;

/// <summary>
/// Fornece a <see cref="PoliticaDelegacao"/> do tenant atual (AA-5/D3). O teto de profundidade de
/// subdelegacao e PARAMETRIZAVEL por tenant: busca primeiro uma chave especifica do tenant
/// (<c>Identidade:Delegacao:PorTenant:&lt;tenantId&gt;:ProfundidadeMaxima</c>), depois o padrao do
/// ambiente (<c>Identidade:Delegacao:ProfundidadeMaxima</c>) e, na ausencia, o padrao conservador do
/// dominio (<see cref="PoliticaDelegacao.ProfundidadeMaximaPadrao"/>). Centraliza a leitura para que
/// a evolucao futura (tabela por tenant) troque apenas esta implementacao, sem tocar a regra I4/D3.
/// </summary>
public sealed class PoliticaDelegacaoProvider(
    IConfiguration configuration,
    ITenantContext tenantContext) : IPoliticaDelegacaoProvider
{
    /// <summary>Secao base da configuracao de delegacao.</summary>
    public const string SecaoBase = "Identidade:Delegacao";

    private readonly IConfiguration _configuration = configuration;
    private readonly ITenantContext _tenantContext = tenantContext;

    /// <inheritdoc />
    public Task<PoliticaDelegacao> ObterAsync(CancellationToken cancellationToken)
    {
        int? especifico = null;
        if (_tenantContext.HasTenant)
        {
            especifico = _configuration.GetValue<int?>(
                $"{SecaoBase}:PorTenant:{_tenantContext.TenantId:D}:ProfundidadeMaxima");
        }

        var profundidade = especifico
            ?? _configuration.GetValue<int?>($"{SecaoBase}:ProfundidadeMaxima")
            ?? PoliticaDelegacao.ProfundidadeMaximaPadrao;

        var politica = profundidade < 0
            ? PoliticaDelegacao.Padrao
            : PoliticaDelegacao.Com(profundidade);

        return Task.FromResult(politica);
    }
}
