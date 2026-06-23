using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;

/// <summary>
/// Gera a folha de 13o salario (gratificacao natalina) de um servidor numa competencia/ano, em DUAS
/// parcelas (design §2.3):
/// <list type="bullet">
/// <item><b>1a parcela</b> (<c>Parcela=1</c>): adiantamento = percentual (param) do 13o integral, SEM
/// descontos (Lei 4.749/65). Lanca apenas o provento <c>13-SAL</c> x percentual.</item>
/// <item><b>2a parcela</b> (<c>Parcela=2</c>): lanca o <c>13-SAL</c> INTEGRAL, apura INSS/RPPS/IRRF
/// PROPRIOS do 13o em BASE SEPARADA (motor <see cref="MotorDeCalculoFolha.CalcularBaseSeparada"/>; IRRF
/// sem simplificado — art. 12-A) e abate a 1a parcela ja paga como rubrica informativa-dedutora.</item>
/// </list>
/// Reusa o motor (descontos) e o catalogo de rubricas (incidencias). Avos por datas — sem relogio.
/// </summary>
/// <param name="ServidorId">Servidor do 13o.</param>
/// <param name="Ano">Ano-calendario do 13o.</param>
/// <param name="MesCompetencia">Mes da competencia de pagamento da parcela (ex.: 11 ou 12).</param>
/// <param name="Parcela">Ordem da parcela (1 = adiantamento sem desconto; 2 = integral com descontos).</param>
/// <param name="RemuneracaoBase">Remuneracao-base do 13o (salario + medias habituais) — entrada do operador.</param>
/// <param name="Admissao">Data de admissao/exercicio (entrada): inicio efetivo da contagem de avos no ano.</param>
public sealed record GerarDecimoTerceiroCommand(
    Guid ServidorId,
    int Ano,
    int MesCompetencia,
    int Parcela,
    decimal RemuneracaoBase,
    DateOnly Admissao) : ICommand<Guid>;

/// <summary>Validacao da geracao do 13o.</summary>
public sealed class GerarDecimoTerceiroValidator : AbstractValidator<GerarDecimoTerceiroCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarDecimoTerceiroValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty();
        RuleFor(c => c.Ano).InclusiveBetween(2000, 2100);
        RuleFor(c => c.MesCompetencia).InclusiveBetween(1, 12);
        RuleFor(c => c.Parcela).InclusiveBetween(1, 2).WithMessage("Parcela do 13o deve ser 1 ou 2.");
        RuleFor(c => c.RemuneracaoBase).GreaterThan(0m).WithMessage("Remuneracao-base do 13o deve ser positiva.");
    }
}

/// <summary>Handler da geracao do 13o salario.</summary>
public sealed class GerarDecimoTerceiroHandler(
    IFolhaDePagamentoRepository folhas,
    IRubricaFolhaRepository rubricas,
    IServidorRegimeConsulta servidores,
    ITabelasLegaisProvider tabelas,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<GerarDecimoTerceiroCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarDecimoTerceiroCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.MesCompetencia);
        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);

        var dados = await servidores.ObterDadosCalculoAsync(request.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Servidor {request.ServidorId} nao encontrado.");

        // Avos do ano: 01/01..31/12, com inicio efetivo na admissao (entrada). Sem relogio (design §1.2).
        var inicioAno = new DateOnly(request.Ano, 1, 1);
        var fimAno = new DateOnly(request.Ano, 12, 31);
        var inicioContagem = request.Admissao > inicioAno ? request.Admissao : inicioAno;
        var avos = Avos.Apurar(inicioContagem, fimAno, []);

        var valorIntegral = CalculadoraDecimoTerceiro.CalcularIntegral(request.RemuneracaoBase, avos);

        // Reabre/abre a folha de 13o da competencia (uma por (tenant, competencia, Tipo)).
        var folha = await folhas.ObterPorCompetenciaAsync(competencia, cancellationToken, TipoFolha.DecimoTerceiro).ConfigureAwait(false);
        if (folha is null)
        {
            folha = FolhaDePagamento.Abrir(tenant.TenantId, competencia, TipoFolha.DecimoTerceiro);
            folhas.Adicionar(folha);
        }

        var codigo13 = Rubrica.De(config.CodigoRubrica13Salario);

        if (request.Parcela == 1)
        {
            // 1a parcela: adiantamento = percentual (param) do integral, SEM descontos (Lei 4.749/65).
            var valorParcela = CalculadoraDecimoTerceiro.CalcularParcela(valorIntegral, config.PercentualPrimeiraParcela13);
            LancarProventoSeHouver(folha, request.ServidorId, codigo13, valorParcela, dados.Regime);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return folha.Id.Value;
        }

        // 2a parcela: 13o INTEGRAL como provento, descontos PROPRIOS do 13o (base separada) e abatimento
        // da 1a parcela ja paga (rubrica informativa-dedutora). Reapura idempotente: limpa lancamentos do
        // servidor antes de relancar.
        var rubricasDoServidor = new[]
        {
            codigo13,
            Rubrica.De(config.CodigoRubricaInss13),
            Rubrica.De(config.CodigoRubricaRpps13),
            Rubrica.De(config.CodigoRubricaIrrf13),
            Rubrica.De(config.CodigoRubrica13Adiantamento),
        };
        folha.RemoverDescontosLegais(request.ServidorId, rubricasDoServidor);
        RemoverProventosDoServidor(folha, request.ServidorId, codigo13);

        LancarProventoSeHouver(folha, request.ServidorId, codigo13, valorIntegral, dados.Regime);

        // Incidencias do 13o (catalogo de rubricas vigente na competencia).
        var (incideInss, incideRpps, incideIrrf) = await ResolverIncidencias13Async(rubricas, competencia, codigo13, cancellationToken).ConfigureAwait(false);

        var tabelaInss = await tabelas.ObterInssVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaRpps = await tabelas.ObterRppsVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaIrrf = await tabelas.ObterIrrfVigenteAsync(competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new CalculoFolhaException("Tabela IRRF nao carregada para a competencia do 13o.");

        var verba = new VerbaCalculo(codigo13, EhProvento: true, valorIntegral, incideInss, incideRpps, incideIrrf);
        var insumos = new InsumosCalculoServidor(request.ServidorId, dados.Regime, dados.QuantidadeDependentes, 0m, [verba]);

        // BASE SEPARADA: IRRF do 13o sem desconto simplificado (art. 12-A); INSS/RPPS isolados do mes.
        var resultado = MotorDeCalculoFolha.CalcularBaseSeparada(insumos, tabelaInss, tabelaIrrf, tabelaRpps, OpcoesIrrf.DecimoTerceiro);

        LancarDescontoSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaInss13), resultado.BaseInss, resultado.DescontoInss, dados.Regime);
        LancarDescontoSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaRpps13), resultado.BaseRpps, resultado.DescontoRpps, dados.Regime);
        LancarDescontoSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaIrrf13), resultado.BaseIrrf, resultado.DescontoIrrf, dados.Regime);

        // Abate a 1a parcela ja paga (adiantamento) como desconto informativo-dedutor.
        var adiantamento = CalculadoraDecimoTerceiro.CalcularParcela(valorIntegral, config.PercentualPrimeiraParcela13);
        LancarDescontoSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubrica13Adiantamento), valorIntegral, adiantamento, dados.Regime);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return folha.Id.Value;
    }

    private static async Task<(bool Inss, bool Rpps, bool Irrf)> ResolverIncidencias13Async(
        IRubricaFolhaRepository rubricas, Competencia competencia, Rubrica codigo13, CancellationToken ct)
    {
        var catalogo = await rubricas.ListarVigentesAsync(competencia, ct).ConfigureAwait(false);
        var rubrica = catalogo.FirstOrDefault(r => string.Equals(r.Codigo.Codigo, codigo13.Codigo, StringComparison.OrdinalIgnoreCase))
            // FAIL-CLOSED (CLAUDE.md S16): sem a rubrica 13-SAL vigente nao se pode presumir incidencia.
            ?? throw new CalculoFolhaException(
                $"Rubrica '{codigo13.Codigo}' do 13o nao possui vigencia valida na competencia {competencia}. " +
                "Geracao interrompida (fail-closed): incidencias de INSS/RPPS/IRRF nao podem ser presumidas.");
        return (rubrica.IncideInss, rubrica.IncideRpps, rubrica.IncideIrrf);
    }

    private static void RemoverProventosDoServidor(FolhaDePagamento folha, Guid servidorId, Rubrica rubrica)
    {
        foreach (var evento in folha.Eventos
            .Where(e => e.ServidorId == servidorId && e.Tipo == TipoEvento.Provento && e.Rubrica == rubrica)
            .ToList())
        {
            folha.RemoverEvento(evento.Id);
        }
    }

    private static void LancarProventoSeHouver(FolhaDePagamento folha, Guid servidorId, Rubrica rubrica, decimal valor, Domain.Cargos.RegimePrevidenciario regime)
    {
        if (valor <= 0m)
        {
            return;
        }

        folha.AdicionarEvento(servidorId, rubrica, TipoEvento.Provento, BaseCalculo.De(valor), valor, regime);
    }

    private static void LancarDescontoSeHouver(FolhaDePagamento folha, Guid servidorId, Rubrica rubrica, decimal baseCalculo, decimal valor, Domain.Cargos.RegimePrevidenciario regime)
    {
        if (valor <= 0m)
        {
            return;
        }

        folha.AdicionarEvento(servidorId, rubrica, TipoEvento.Desconto, BaseCalculo.De(baseCalculo), valor, regime);
    }
}
