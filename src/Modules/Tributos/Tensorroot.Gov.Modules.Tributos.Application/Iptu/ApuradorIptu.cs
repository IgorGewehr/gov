using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

namespace Tensorroot.Gov.Modules.Tributos.Application.Iptu;

/// <summary>
/// Orquestra a apuração do IPTU: carrega imóvel + PGV vigente + tabela de alíquotas vigente
/// (predial × territorial conforme o imóvel ser edificado) e delega ao motor de domínio. Reutilizado
/// pela query de preview e pelo comando de lançamento — fonte única de verdade da apuração.
/// </summary>
internal static class ApuradorIptu
{
    /// <summary>Apura o IPTU de um imóvel num exercício, carregando os parâmetros vigentes.</summary>
    /// <param name="imovelId">Imóvel a apurar.</param>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="isencao">Parâmetros de isenção/desconto (lei municipal).</param>
    /// <param name="imoveis">Repositório de imóveis.</param>
    /// <param name="plantas">Repositório de PGV.</param>
    /// <param name="tabelas">Repositório de tabelas de alíquota.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A memória de cálculo do IPTU.</returns>
    /// <exception cref="InvalidOperationException">Se o imóvel, a PGV ou a tabela vigente não existirem.</exception>
    public static async Task<MemoriaIptu> ApurarAsync(
        ImovelId imovelId,
        int exercicio,
        ParametrosIsencaoIptu isencao,
        IImovelRepository imoveis,
        IPlantaValoresRepository plantas,
        ITabelaAliquotaIptuRepository tabelas,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(isencao);

        var imovel = await imoveis.ObterPorIdAsync(imovelId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        if (!imovel.Ativo)
        {
            throw new InvalidOperationException("O imóvel está inativo no cadastro.");
        }

        var planta = await plantas.ObterVigenteAsync(exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há PGV vigente para o exercício {exercicio}.");

        var edificado = imovel.Caracteristicas.Edificado;
        var tabela = await tabelas.ObterVigenteAsync(exercicio, edificado, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Não há tabela de alíquotas {(edificado ? "predial" : "territorial")} vigente para o exercício {exercicio}.");

        return CalculadoraIptu.Calcular(imovel, planta, tabela, isencao);
    }
}
