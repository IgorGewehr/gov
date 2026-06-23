using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Igd;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Igd;

/// <summary>
/// 3d.3 — fonte dos fatores LOCAIS da EstimativaIgd, tenant-scoped. DERIVA, das entidades ja existentes
/// (Familia, AcompanhamentoCondicionalidade, Beneficio), os tres fatores [0,1] que alimentam a estimativa.
/// </summary>
public interface IFatoresIgdReadModel
{
    /// <summary>Calcula os fatores locais da estimativa do IGD para um exercicio, tenant-scoped.</summary>
    /// <param name="exercicio">Exercicio (ano) de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Fatores locais (cada um em [0,1]).</returns>
    Task<FatoresIgd> ObterFatoresAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Resultado da consulta de estimativa do IGD (projecao de leitura).</summary>
/// <param name="Exercicio">Exercicio (ano).</param>
/// <param name="Indice">Indice estimado (0 a 1).</param>
/// <param name="FatorAtualizacaoCadastral">Fator de atualizacao cadastral (0 a 1).</param>
/// <param name="FatorCondicionalidades">Fator de cumprimento de condicionalidades (0 a 1).</param>
/// <param name="FatorGestaoBeneficios">Fator de gestao de beneficios (0 a 1).</param>
/// <param name="Rotulo">Rotulo: estimativa local, nao oficial.</param>
public sealed record EstimativaIgdDto(
    int Exercicio,
    decimal Indice,
    decimal FatorAtualizacaoCadastral,
    decimal FatorCondicionalidades,
    decimal FatorGestaoBeneficios,
    string Rotulo);

/// <summary>3d.3: estima o IGD-PBF/IGD-SUAS a partir de indicadores LOCAIS (estimativa gerencial, nao oficial).</summary>
/// <param name="Exercicio">Exercicio (ano) de referencia.</param>
public sealed record EstimarIgdQuery(int Exercicio) : IQuery<EstimativaIgdDto>;

/// <summary>Handler da estimativa do IGD.</summary>
public sealed class EstimarIgdHandler(IFatoresIgdReadModel fatoresReadModel)
    : IQueryHandler<EstimarIgdQuery, EstimativaIgdDto>
{
    /// <inheritdoc />
    public async Task<EstimativaIgdDto> Handle(EstimarIgdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fatores = await fatoresReadModel.ObterFatoresAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        var estimativa = CalculadoraIgd.Estimar(fatores);

        return new EstimativaIgdDto(
            request.Exercicio,
            estimativa.Indice,
            estimativa.Fatores.FatorAtualizacaoCadastral,
            estimativa.Fatores.FatorCondicionalidades,
            estimativa.Fatores.FatorGestaoBeneficios,
            estimativa.Rotulo);
    }
}
