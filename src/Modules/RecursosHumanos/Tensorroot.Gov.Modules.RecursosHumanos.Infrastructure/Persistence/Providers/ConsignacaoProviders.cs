using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;

/// <summary>
/// Resolve os <see cref="PercentuaisMargem"/> vigentes na competencia: consulta primeiro o catalogo
/// PERSISTIDO de <see cref="ParametrosMargemVigente"/> do tenant (a vigencia mais recente &lt;= competencia);
/// na ausencia, cai para os DEFAULTS LEGAIS parametrizados (<see cref="ParametrosMargem"/> — Lei 14.131/2021),
/// nunca <em>hardcoded</em> no dominio (CLAUDE.md S7/S16). Respeita o Global Query Filter por tenant.
/// </summary>
public sealed class ParametrosMargemProvider(
    RecursosHumanosDbContext context,
    IConfiguration configuration)
    : IParametrosMargemProvider
{
    private static int Ordinal(Competencia competencia) => (competencia.Ano * 100) + competencia.Mes;

    /// <inheritdoc />
    public async Task<PercentuaisMargem> ObterVigenteAsync(Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = Ordinal(competencia);

        // Poucas linhas por tenant: carrega (ja filtrado pelo tenant) e seleciona a vigencia mais recente.
        var cadastradas = await context.ParametrosMargem.ToListAsync(cancellationToken).ConfigureAwait(false);
        var vigente = cadastradas
            .Where(p => Ordinal(p.VigenciaInicio) <= alvo)
            .OrderByDescending(p => Ordinal(p.VigenciaInicio))
            .FirstOrDefault();
        if (vigente is not null)
        {
            return new PercentuaisMargem(
                vigente.PercentualGeral,
                vigente.PercentualCartaoConsignado,
                vigente.PercentualCartaoBeneficio);
        }

        var parametros = configuration
            .GetSection(ParametrosMargem.SecaoConfiguracao)
            .Get<ParametrosMargem>() ?? new ParametrosMargem();
        return new PercentuaisMargem(
            parametros.PercentualGeral,
            parametros.PercentualCartaoConsignado,
            parametros.PercentualCartaoBeneficio);
    }
}

/// <summary>
/// Apura a BASE consignavel de um servidor na competencia (design RH §2.2): soma dos PROVENTOS da folha
/// Mensal da competencia cujas rubricas tem <see cref="RubricaConsignavel.ContaParaMargem"/> — base APURADA
/// DA FOLHA (ja reflete o ajuste de afastamento, pois os proventos foram lancados ajustados). Quando ainda
/// nao ha folha Mensal da competencia, retorna o vencimento do CARGO como estimativa para a averbacao
/// (fail-safe: estimativa conservadora pela remuneracao-base). Respeita o Global Query Filter por tenant.
/// </summary>
public sealed class BaseConsignavelProvider(RecursosHumanosDbContext context) : IBaseConsignavelProvider
{
    /// <inheritdoc />
    public async Task<decimal> ApurarBaseAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);

        // Rubricas (S-1010) que compoem a base da margem (parametrizacao do tenant). Sem cadastro, nenhuma
        // rubrica conta — a base apurada da folha sera zero e a averbacao caira para o vencimento do cargo.
        var codigosQueContam = await context.RubricasConsignaveis
            .Where(r => r.Ativa && r.ContaParaMargem)
            .Select(r => r.Codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (codigosQueContam.Count > 0)
        {
            var folha = await context.FolhasDePagamento
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Competencia == competencia && f.Tipo == TipoFolha.Mensal, cancellationToken)
                .ConfigureAwait(false);

            if (folha is not null)
            {
                var contam = new HashSet<string>(codigosQueContam, StringComparer.OrdinalIgnoreCase);
                var baseFolha = folha.Eventos
                    .Where(e => e.ServidorId == servidorId
                        && e.Tipo == TipoEvento.Provento
                        && contam.Contains(e.Rubrica.Codigo))
                    .Sum(e => e.Valor);
                if (baseFolha > 0m)
                {
                    return decimal.Round(baseFolha, 2, MidpointRounding.AwayFromZero);
                }
            }
        }

        // Fallback (sem folha da competencia ou sem proventos consignaveis lancados): vencimento do cargo.
        var id = new ServidorId(servidorId);
        var servidor = await context.Servidores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (servidor is null)
        {
            return 0m;
        }

        var cargo = await context.Cargos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == servidor.CargoId, cancellationToken)
            .ConfigureAwait(false);
        return cargo is null ? 0m : decimal.Round(cargo.Vencimento.Valor, 2, MidpointRounding.AwayFromZero);
    }
}
