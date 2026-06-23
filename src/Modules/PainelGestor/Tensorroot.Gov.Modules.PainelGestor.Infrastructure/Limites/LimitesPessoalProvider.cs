using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Limites;

/// <summary>
/// Provedor dos limites de pessoal (LRF) vigentes. Lê o parâmetro do tenant em
/// <see cref="ParametroLimitePessoal"/> (vigência mais recente ≤ 31/12 do exercício — reprodutível, sem
/// relógio); na ausência de parâmetro do tenant, usa o fallback legal de referência do Executivo
/// municipal (54% / 95% / 90%). Os percentuais NUNCA são hardcoded em regra de negócio — o default é só
/// o piso legal de partida, sobreponível por tenant+vigência (a Lei Orgânica/o TCE-RS pode exigir outro;
/// a apuração mudou com a LC 178/2021). // TODO(validar-oficial): limites LRF do ente.
/// </summary>
public sealed class LimitesPessoalProvider(PainelGestorDbContext context) : ILimitesPessoalProvider
{
    /// <inheritdoc />
    public async Task<LimitesPessoalLrf> ObterAsync(int exercicio, CancellationToken cancellationToken)
    {
        var referencia = new DateOnly(exercicio, 12, 31);

        var parametro = await context.ParametrosLimitePessoal
            .Where(item => item.VigenciaInicio <= referencia)
            .OrderByDescending(item => item.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return parametro?.ParaLimites() ?? LimitesPessoalLrf.PadraoExecutivoMunicipal;
    }
}
