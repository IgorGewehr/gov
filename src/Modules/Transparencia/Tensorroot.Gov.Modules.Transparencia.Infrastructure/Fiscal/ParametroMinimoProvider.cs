using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;

/// <summary>
/// <b>M7.0.3.</b> Provedor dos percentuais mínimos vigentes por setor. Lê o parâmetro do tenant em
/// <see cref="ParametroFiscalVigente"/> (vigência mais recente ≤ 31/12 do exercício); na ausência de
/// parâmetro do tenant, usa o <b>default legal</b> da lei vigente (Saúde 15% LC 141/2012; Educação 25%
/// CF art. 212). O percentual NUNCA é hardcoded em regra de negócio — o default é só o piso legal de
/// partida e pode ser sobreposto por tenant+vigência (a Lei Orgânica pode exigir mais).
/// </summary>
public sealed class ParametroMinimoProvider(TransparenciaDbContext context) : IParametroMinimoProvider
{
    /// <summary>Chave do percentual mínimo da Saúde (ASPS).</summary>
    public const string ChaveMinimoSaude = "MinimoSaudeAsps";

    /// <summary>Chave do percentual mínimo da Educação (MDE).</summary>
    public const string ChaveMinimoEducacao = "MinimoEducacaoMde";

    // Defaults legais (piso de partida — LC 141/2012 art. 6º; CF art. 212). Sobreponíveis por tenant.
    private const decimal DefaultMinimoSaude = 0.15m;
    private const decimal DefaultMinimoEducacao = 0.25m;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParametroMinimo>> ObterParametrosAsync(int exercicio, CancellationToken cancellationToken)
    {
        var referencia = new DateOnly(exercicio, 12, 31);

        var saude = await ObterPercentualAsync(ChaveMinimoSaude, referencia, DefaultMinimoSaude, cancellationToken).ConfigureAwait(false);
        var educacao = await ObterPercentualAsync(ChaveMinimoEducacao, referencia, DefaultMinimoEducacao, cancellationToken).ConfigureAwait(false);

        return
        [
            new ParametroMinimo(SetorMinimo.Saude, saude),
            new ParametroMinimo(SetorMinimo.Educacao, educacao),
        ];
    }

    private async Task<decimal> ObterPercentualAsync(string chave, DateOnly referencia, decimal padraoLegal, CancellationToken cancellationToken)
    {
        var parametro = await context.ParametrosFiscaisVigentes
            .Where(item => item.Chave == chave && item.VigenciaInicio <= referencia)
            .OrderByDescending(item => item.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return parametro?.Valor ?? padraoLegal;
    }
}
