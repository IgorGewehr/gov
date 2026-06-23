using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;

/// <summary>
/// Gera a folha de FERIAS de um servidor (design §3): remuneracao proporcional aos dias gozados +
/// 1/3 constitucional (fracao parametrizavel) e, opcionalmente, o abono pecuniario (venda de ate 1/3)
/// com seu terco. Lanca uma <see cref="FolhaDePagamento"/> Tipo=Ferias (sem abate-teto). As INCIDENCIAS
/// (ferias gozadas + terco tributam; abono e indenizatorio, isento — Sumula 386 STJ) vivem na
/// <c>RubricaFolha</c> e sao apuradas depois por <c>ApurarDescontosLegais</c> (motor mensal normal,
/// difere do 13o). Dias e remuneracao sao entrada — sem relogio.
/// </summary>
/// <param name="ServidorId">Servidor das ferias.</param>
/// <param name="Ano">Ano da competencia de pagamento.</param>
/// <param name="Mes">Mes da competencia de pagamento.</param>
/// <param name="RemuneracaoMensal">Remuneracao mensal-base das ferias (salario + medias habituais).</param>
/// <param name="DiasGozados">Dias de ferias gozados (0..30).</param>
/// <param name="DiasVendidos">Dias convertidos em abono pecuniario (0..10).</param>
/// <param name="InicioPeriodoAquisitivo">
/// P0-6: inicio do periodo aquisitivo das ferias (entrada, sem relogio). Com <see cref="DataConcessao"/>,
/// determina se a concessao ocorreu APOS o periodo concessivo (dobra — CLT art. 137). Nulo: sem dobra.
/// </param>
/// <param name="DataConcessao">Data de concessao/inicio do gozo das ferias (entrada). Nulo: sem dobra.</param>
public sealed record GerarFeriasCommand(
    Guid ServidorId,
    int Ano,
    int Mes,
    decimal RemuneracaoMensal,
    int DiasGozados,
    int DiasVendidos,
    DateOnly? InicioPeriodoAquisitivo = null,
    DateOnly? DataConcessao = null) : ICommand<Guid>;

/// <summary>Validacao da geracao de ferias.</summary>
public sealed class GerarFeriasValidator : AbstractValidator<GerarFeriasCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarFeriasValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty();
        RuleFor(c => c.Ano).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
        RuleFor(c => c.RemuneracaoMensal).GreaterThan(0m);
        RuleFor(c => c.DiasGozados).InclusiveBetween(0, 30);
        RuleFor(c => c.DiasVendidos).InclusiveBetween(0, 10).WithMessage("Abono pecuniario: venda de ate 1/3 (max. 10 dias).");
        RuleFor(c => c.DiasGozados + c.DiasVendidos)
            .LessThanOrEqualTo(30)
            .WithMessage("Soma de dias gozados e vendidos nao pode exceder o periodo (30 dias).");
    }
}

/// <summary>Handler da geracao de ferias.</summary>
public sealed class GerarFeriasHandler(
    IFolhaDePagamentoRepository folhas,
    IServidorRegimeConsulta servidores,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<GerarFeriasCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarFeriasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.Mes);
        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);

        var dados = await servidores.ObterDadosCalculoAsync(request.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Servidor {request.ServidorId} nao encontrado.");

        // P0-6: dobra (CLT art. 137) — ferias concedidas APOS o periodo concessivo (12 meses apos o fim
        // do aquisitivo) sao pagas em dobro. Datas sao ENTRADA (sem relogio); na ausencia, nao ha dobra.
        var emDobra = DeterminarDobra(request);

        var resultado = CalculadoraFerias.Calcular(
            request.RemuneracaoMensal,
            request.DiasGozados,
            request.DiasVendidos,
            config.FracaoTercoConstitucional,
            emDobra);

        var folha = await folhas.ObterPorCompetenciaAsync(competencia, cancellationToken, TipoFolha.Ferias).ConfigureAwait(false);
        if (folha is null)
        {
            folha = FolhaDePagamento.Abrir(tenant.TenantId, competencia, TipoFolha.Ferias);
            folhas.Adicionar(folha);
        }

        // Reapuracao idempotente do servidor nesta folha de ferias.
        var rubricasFerias = new[]
        {
            Rubrica.De(config.CodigoRubricaFerias),
            Rubrica.De(config.CodigoRubricaTercoFerias),
            Rubrica.De(config.CodigoRubricaAbonoPecuniario),
            Rubrica.De(config.CodigoRubricaTercoAbono),
        };
        foreach (var evento in folha.Eventos
            .Where(e => e.ServidorId == request.ServidorId && Array.Exists(rubricasFerias, r => r == e.Rubrica))
            .ToList())
        {
            folha.RemoverEvento(evento.Id);
        }

        LancarSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaFerias), resultado.RemuneracaoFerias, dados.Regime);
        LancarSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaTercoFerias), resultado.TercoConstitucional, dados.Regime);
        LancarSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaAbonoPecuniario), resultado.AbonoPecuniario, dados.Regime);
        LancarSeHouver(folha, request.ServidorId, Rubrica.De(config.CodigoRubricaTercoAbono), resultado.TercoAbono, dados.Regime);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return folha.Id.Value;
    }

    private static bool DeterminarDobra(GerarFeriasCommand request)
    {
        if (request.InicioPeriodoAquisitivo is not { } inicio || request.DataConcessao is not { } concessao)
        {
            return false;
        }

        // Fim do periodo aquisitivo = inicio + 12 meses - 1 dia (regra geral); a partir dai corre o
        // periodo concessivo de 12 meses. EmDobra confere se a concessao caiu apos esse prazo.
        var fimAquisitivo = inicio.AddYears(1).AddDays(-1);
        var periodo = new PeriodoAquisitivoFerias(
            inicio,
            fimAquisitivo,
            PeriodoAquisitivoFerias.DiasDireitoPadrao,
            DiasJaGozados: 0,
            DiasVendidosAbono: 0);
        return periodo.EmDobra(concessao);
    }

    private static void LancarSeHouver(FolhaDePagamento folha, Guid servidorId, Rubrica rubrica, decimal valor, Domain.Cargos.RegimePrevidenciario regime)
    {
        if (valor <= 0m)
        {
            return;
        }

        folha.AdicionarEvento(servidorId, rubrica, TipoEvento.Provento, BaseCalculo.De(valor), valor, regime);
    }
}
