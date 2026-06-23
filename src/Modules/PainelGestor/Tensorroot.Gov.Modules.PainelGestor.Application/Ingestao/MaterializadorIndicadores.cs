using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;

/// <summary>
/// Colaborador da ingestão: obtém (ou cria) o <see cref="IndicadorMunicipioSnapshot"/> do exercício do
/// tenant e centraliza o get-or-create, evitando duplicar a lógica em cada handler de evento. O upsert
/// do snapshot por <c>(TenantId, Exercicio)</c> é garantido por índice único na persistência; aqui só
/// orquestramos. Não chama SaveChanges — quem dispara confirma a transação.
/// </summary>
public sealed class MaterializadorIndicadores(IIndicadorMunicipioRepository repositorio)
{
    /// <summary>Obtém o snapshot do exercício ou cria um novo (adicionado ao repositório).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O snapshot pronto para mutação.</returns>
    public async Task<IndicadorMunicipioSnapshot> ObterOuCriarAsync(
        Guid tenantId,
        int exercicio,
        CancellationToken cancellationToken)
    {
        var snapshot = await repositorio.ObterPorExercicioAsync(exercicio, cancellationToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            return snapshot;
        }

        snapshot = IndicadorMunicipioSnapshot.Criar(tenantId, exercicio);
        repositorio.Adicionar(snapshot);
        return snapshot;
    }

    /// <summary>Deriva o exercício de uma data (ano).</summary>
    /// <param name="data">Data do fato.</param>
    /// <returns>Ano da data.</returns>
    public static int ExercicioDe(DateOnly data) => data.Year;
}
