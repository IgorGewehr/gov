using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;

/// <summary>
/// Resolve a <see cref="RegraAfastamento"/> vigente do tipo na competencia: consulta primeiro o catalogo
/// PERSISTIDO do tenant (versionado por vigencia, selecionando a mais recente &lt;= competencia); na
/// ausencia, materializa a regra a partir dos DEFAULTS LEGAIS parametrizados (<see cref="ParametrosAfastamento"/>),
/// nunca <em>hardcoded</em> no dominio (CLAUDE.md S7/S16). Respeita o Global Query Filter por tenant.
/// </summary>
public sealed class RegraAfastamentoProvider(
    RecursosHumanosDbContext context,
    IConfiguration configuration,
    ITenantContext tenant)
    : IRegraAfastamentoProvider
{
    private static int Ordinal(Competencia competencia) => (competencia.Ano * 100) + competencia.Mes;

    /// <inheritdoc />
    public async Task<RegraAfastamento> ObterVigenteAsync(TipoAfastamento tipo, Competencia competencia, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        var alvo = Ordinal(competencia);

        // Catalogo persistido do tenant: a vigencia mais recente <= competencia (poucas linhas por tenant).
        var cadastradas = await context.RegrasAfastamento
            .Where(r => r.Tipo == tipo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var vigente = cadastradas
            .Where(r => Ordinal(r.VigenciaInicio) <= alvo)
            .OrderByDescending(r => Ordinal(r.VigenciaInicio))
            .FirstOrDefault();
        if (vigente is not null)
        {
            return vigente;
        }

        // Default legal parametrizado (nao persistido) — fonte do efeito quando o tenant nao cadastrou regra.
        var parametros = configuration
            .GetSection(ParametrosAfastamento.SecaoConfiguracao)
            .Get<ParametrosAfastamento>() ?? new ParametrosAfastamento();
        return ConstruirDefault(tipo, competencia, parametros);
    }

    private RegraAfastamento ConstruirDefault(TipoAfastamento tipo, Competencia competencia, ParametrosAfastamento p)
    {
        // Cada tipo mapeia para (suspende?, % do ente, dias pagos pelo ente, conta tempo?, duracao padrao).
        // Os prazos/percentuais vem dos parametros do tenant (defaults documentais espelham a lei federal).
        return tipo switch
        {
            TipoAfastamento.LicencaMaternidade => Regra(
                suspende: false, percentual: 100m, diasEnte: 0, contaTempo: true,
                duracao: p.AderiuEmpresaCidada ? p.DiasLicencaMaternidadeAmpliada : p.DiasLicencaMaternidade),

            TipoAfastamento.LicencaPaternidade => Regra(
                suspende: false, percentual: 100m, diasEnte: 0, contaTempo: true,
                duracao: p.AderiuEmpresaCidada ? p.DiasLicencaPaternidadeAmpliada : p.DiasLicencaPaternidade),

            // Ate 15 dias: o ENTE paga integral todo o periodo (RGPS — primeiros 15d).
            TipoAfastamento.DoencaAte15Dias => Regra(
                suspende: false, percentual: 100m, diasEnte: p.DiasPagosPeloEnteDoenca, contaTempo: true, duracao: null),

            // Acima de 15 dias / auxilio-doenca: ente paga os primeiros 15d e SUSPENDE o restante (INSS).
            // No RPPS, o ente pode manter por lei propria (parametrizavel: RppsMantemDoencaAlem15Dias).
            TipoAfastamento.DoencaInss => Regra(
                suspende: !p.RppsMantemDoencaAlem15Dias, percentual: 100m, diasEnte: p.DiasPagosPeloEnteDoenca, contaTempo: true, duracao: null),

            TipoAfastamento.AcidenteTrabalho => Regra(
                suspende: !p.RppsMantemDoencaAlem15Dias, percentual: 100m, diasEnte: p.DiasPagosPeloEnteDoenca, contaTempo: true, duracao: null),

            TipoAfastamento.LicencaPremio => Regra(
                suspende: false, percentual: 100m, diasEnte: 0, contaTempo: true, duracao: p.DiasLicencaPremio),

            // Interesse particular: SUSPENDE 100% e NAO conta tempo.
            TipoAfastamento.LicencaSemVencimento => Regra(
                suspende: true, percentual: 0m, diasEnte: 0, contaTempo: false, duracao: p.DiasMaximosLicencaSemVencimento),

            // Cessao COM onus: o cedente continua pagando integral.
            TipoAfastamento.CessaoComOnus => Regra(
                suspende: false, percentual: 100m, diasEnte: 0, contaTempo: true, duracao: null),

            // Cessao SEM onus: o cedente suspende o provento.
            TipoAfastamento.CessaoSemOnus => Regra(
                suspende: true, percentual: 0m, diasEnte: 0, contaTempo: true, duracao: null),

            // Mandato eletivo: por opcao remuneratoria; default mantem provento (conta tempo).
            TipoAfastamento.MandatoEletivo => Regra(
                suspende: false, percentual: 100m, diasEnte: 0, contaTempo: true, duracao: null),

            _ => Regra(suspende: false, percentual: 100m, diasEnte: 0, contaTempo: true, duracao: null),
        };

        RegraAfastamento Regra(bool suspende, decimal percentual, int diasEnte, bool contaTempo, int? duracao)
            => RegraAfastamento.Definir(
                tenant.TenantId,
                tipo,
                competencia,
                suspende,
                percentual,
                diasEnte,
                contaTempo,
                duracao,
                tipo == TipoAfastamento.CessaoComOnus || tipo == TipoAfastamento.CessaoSemOnus
                    ? "S-2231"
                    : RegraAfastamento.CodigoEventoESocialPadrao);
    }
}
