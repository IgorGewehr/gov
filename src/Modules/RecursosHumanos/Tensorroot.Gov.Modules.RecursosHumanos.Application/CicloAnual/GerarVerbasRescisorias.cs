using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;

/// <summary>
/// Gera a folha de RESCISAO/desligamento de um servidor (design §4): compoe as verbas devidas a partir
/// da matriz parametrizavel <see cref="CompositorRescisao"/> (tipo de desligamento x regime) e calcula
/// cada verba reusando as calculadoras puras (saldo de salario, 13o proporcional por avos, ferias
/// vencidas/proporcionais + 1/3). As verbas indenizatorias (ferias na rescisao, abono) terao incidencias
/// DESLIGADAS na <c>RubricaFolha</c> (Sumula 386 STJ); aviso/multa so para celetista. Datas (desligamento)
/// sao ENTRADA — sem relogio. // TODO(validar-oficial): valores de aviso (Lei 12.506) e multa de 40% do FGTS
/// dependem de base FGTS/tempo de servico nao modelados — entram como valor informado pelo operador.
/// </summary>
/// <param name="ServidorId">Servidor desligado.</param>
/// <param name="DataDesligamento">Data de encerramento do vinculo (entrada).</param>
/// <param name="TipoDesligamento">Tipo de desligamento (matriz §4.2).</param>
/// <param name="Regime">Regime juridico do vinculo (estatutario/celetista).</param>
/// <param name="Vencimento">Vencimento mensal-base.</param>
/// <param name="DiasTrabalhadosNoMes">Dias trabalhados no mes do desligamento (saldo de salario).</param>
/// <param name="Admissao">Admissao/exercicio (entrada): inicio da contagem de avos do 13o no ano.</param>
/// <param name="InicioPeriodoAquisitivoFerias">Inicio do periodo aquisitivo de ferias em curso (entrada).</param>
/// <param name="DiasFeriasVencidas">Dias de ferias vencidas (periodos completos nao gozados).</param>
/// <param name="ValorAvisoPrevio">Valor do aviso previo (so celetista); informado pelo operador.</param>
/// <param name="ValorMultaFgts">Valor da multa de 40% do FGTS (so celetista); informado pelo operador.</param>
public sealed record GerarVerbasRescisoriasCommand(
    Guid ServidorId,
    DateOnly DataDesligamento,
    TipoDesligamento TipoDesligamento,
    RegimeVinculo Regime,
    decimal Vencimento,
    int DiasTrabalhadosNoMes,
    DateOnly Admissao,
    DateOnly InicioPeriodoAquisitivoFerias,
    int DiasFeriasVencidas,
    decimal ValorAvisoPrevio,
    decimal ValorMultaFgts) : ICommand<Guid>;

/// <summary>Validacao da geracao de verbas rescisorias.</summary>
public sealed class GerarVerbasRescisoriasValidator : AbstractValidator<GerarVerbasRescisoriasCommand>
{
    /// <summary>Define as regras (Enum.IsDefined para nao aceitar valor de enum fora do contrato).</summary>
    public GerarVerbasRescisoriasValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty();
        RuleFor(c => c.DataDesligamento).NotEmpty();
        RuleFor(c => c.TipoDesligamento).Must(t => Enum.IsDefined(t)).WithMessage("Tipo de desligamento invalido.");
        RuleFor(c => c.Regime).Must(r => Enum.IsDefined(r)).WithMessage("Regime de vinculo invalido.");
        RuleFor(c => c.Vencimento).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.DiasTrabalhadosNoMes).InclusiveBetween(0, 31);
        RuleFor(c => c.DiasFeriasVencidas).GreaterThanOrEqualTo(0);
        RuleFor(c => c.ValorAvisoPrevio).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.ValorMultaFgts).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da geracao de verbas rescisorias.</summary>
public sealed class GerarVerbasRescisoriasHandler(
    IFolhaDePagamentoRepository folhas,
    IServidorRegimeConsulta servidores,
    IRubricaFolhaRepository rubricas,
    ITabelasLegaisProvider tabelas,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<GerarVerbasRescisoriasCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarVerbasRescisoriasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.DataDesligamento.Year, request.DataDesligamento.Month);
        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);

        var dados = await servidores.ObterDadosCalculoAsync(request.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Servidor {request.ServidorId} nao encontrado.");

        // Matriz parametrizavel: quais verbas sao devidas para (tipo, regime) — sem if magico.
        var verbasDevidas = CompositorRescisao.Compor(request.TipoDesligamento, request.Regime);

        var folha = await folhas.ObterPorCompetenciaAsync(competencia, cancellationToken, TipoFolha.Rescisao).ConfigureAwait(false);
        if (folha is null)
        {
            folha = FolhaDePagamento.Abrir(tenant.TenantId, competencia, TipoFolha.Rescisao);
            folhas.Adicionar(folha);
        }

        var diasDoMes = DateTime.DaysInMonth(request.DataDesligamento.Year, request.DataDesligamento.Month);
        var fracaoTerco = config.FracaoTercoConstitucional;

        var valor13Proporcional = 0m;
        foreach (var verba in verbasDevidas)
        {
            var (codigo, valor) = CalcularVerba(verba, request, config, diasDoMes, fracaoTerco);
            LancarSeHouver(folha, request.ServidorId, codigo, valor, dados.Regime);
            if (verba == VerbaRescisoria.DecimoTerceiroProporcional)
            {
                valor13Proporcional = valor;
            }
        }

        // P0-4: o 13o proporcional da rescisao tem IRRF/INSS em BASE PROPRIA (Lei 7.713/88 art. 12-A),
        // SEPARADA das demais verbas do mes, com o desconto simplificado VEDADO — exatamente como o 13o
        // anual (reusa MotorDeCalculoFolha.CalcularBaseSeparada). As rubricas INSS-13/IRRF-13 sao lancadas
        // na PROPRIA folha de rescisao; a verba 13-PROP tem incidencia mensal DESLIGADA na RubricaFolha,
        // de modo que ApurarDescontosLegais (motor mensal) NUNCA a some com saldo/ferias (nao mistura).
        if (valor13Proporcional > 0m)
        {
            await ApurarDescontos13ProporcionalAsync(folha, request.ServidorId, valor13Proporcional, dados, competencia, config, cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return folha.Id.Value;
    }

    private async Task ApurarDescontos13ProporcionalAsync(
        FolhaDePagamento folha,
        Guid servidorId,
        decimal valor13,
        Abstractions.DadosCalculoServidor dados,
        Competencia competencia,
        ParametrosFolha config,
        CancellationToken cancellationToken)
    {
        var codigo13Prop = Rubrica.De(config.CodigoRubrica13Proporcional);
        var (incideInss, incideRpps, incideIrrf) = await ResolverIncidencias13PropAsync(competencia, codigo13Prop, cancellationToken).ConfigureAwait(false);

        var tabelaInss = await tabelas.ObterInssVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaRpps = await tabelas.ObterRppsVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaIrrf = await tabelas.ObterIrrfVigenteAsync(competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new CalculoFolhaException("Tabela IRRF nao carregada para a competencia da rescisao.");

        var verba = new VerbaCalculo(codigo13Prop, EhProvento: true, valor13, incideInss, incideRpps, incideIrrf);
        var insumos = new InsumosCalculoServidor(servidorId, dados.Regime, dados.QuantidadeDependentes, 0m, [verba]);

        // Base separada do 13o: simplificado VEDADO (art. 12-A), INSS/RPPS/IRRF isolados das demais verbas.
        var resultado = MotorDeCalculoFolha.CalcularBaseSeparada(insumos, tabelaInss, tabelaIrrf, tabelaRpps, OpcoesIrrf.DecimoTerceiro);

        LancarDescontoSeHouver(folha, servidorId, Rubrica.De(config.CodigoRubricaInss13), resultado.BaseInss, resultado.DescontoInss, dados.Regime);
        LancarDescontoSeHouver(folha, servidorId, Rubrica.De(config.CodigoRubricaRpps13), resultado.BaseRpps, resultado.DescontoRpps, dados.Regime);
        LancarDescontoSeHouver(folha, servidorId, Rubrica.De(config.CodigoRubricaIrrf13), resultado.BaseIrrf, resultado.DescontoIrrf, dados.Regime);
    }

    private async Task<(bool Inss, bool Rpps, bool Irrf)> ResolverIncidencias13PropAsync(
        Competencia competencia, Rubrica codigo13Prop, CancellationToken cancellationToken)
    {
        var catalogo = await rubricas.ListarVigentesAsync(competencia, cancellationToken).ConfigureAwait(false);
        var rubrica = catalogo.FirstOrDefault(r => string.Equals(r.Codigo.Codigo, codigo13Prop.Codigo, StringComparison.OrdinalIgnoreCase))
            // FAIL-CLOSED (CLAUDE.md S16): sem a rubrica 13-PROP vigente nao se presume incidencia.
            ?? throw new CalculoFolhaException(
                $"Rubrica '{codigo13Prop.Codigo}' do 13o proporcional nao possui vigencia valida na competencia {competencia}. " +
                "Apuracao interrompida (fail-closed): incidencias de INSS/RPPS/IRRF nao podem ser presumidas.");
        return (rubrica.IncideInss, rubrica.IncideRpps, rubrica.IncideIrrf);
    }

    private static void LancarDescontoSeHouver(FolhaDePagamento folha, Guid servidorId, Rubrica rubrica, decimal baseCalculo, decimal valor, Domain.Cargos.RegimePrevidenciario regime)
    {
        if (valor <= 0m)
        {
            return;
        }

        folha.AdicionarEvento(servidorId, rubrica, TipoEvento.Desconto, BaseCalculo.De(baseCalculo), valor, regime);
    }

    private static (Rubrica Codigo, decimal Valor) CalcularVerba(
        VerbaRescisoria verba,
        GerarVerbasRescisoriasCommand request,
        ParametrosFolha config,
        int diasDoMes,
        decimal fracaoTerco)
        => verba switch
        {
            VerbaRescisoria.SaldoSalario => (
                Rubrica.De(config.CodigoRubricaSaldoSalario),
                CompositorRescisao.CalcularSaldoSalario(request.Vencimento, request.DiasTrabalhadosNoMes, diasDoMes)),

            // 13o proporcional: avos do ano (01/01..desligamento), valor por CalculadoraDecimoTerceiro.
            VerbaRescisoria.DecimoTerceiroProporcional => (
                Rubrica.De(config.CodigoRubrica13Proporcional),
                CalculadoraDecimoTerceiro.CalcularIntegral(
                    request.Vencimento,
                    Avos.Apurar(InicioAno(request), request.DataDesligamento, []))),

            // Ferias vencidas + 1/3 (indenizadas): dias informados.
            VerbaRescisoria.FeriasVencidas => (
                Rubrica.De(config.CodigoRubricaFeriasVencidas),
                ValorFeriasComTerco(request.Vencimento, request.DiasFeriasVencidas, fracaoTerco)),

            // Ferias proporcionais + 1/3 (indenizadas): avos do periodo aquisitivo em curso => dias = avos x 2,5.
            VerbaRescisoria.FeriasProporcionais => (
                Rubrica.De(config.CodigoRubricaFeriasProporcionais),
                ValorFeriasProporcionais(request, fracaoTerco)),

            VerbaRescisoria.AvisoPrevio => (Rubrica.De(config.CodigoRubricaAvisoPrevio), request.ValorAvisoPrevio),

            VerbaRescisoria.MultaFgts => (Rubrica.De(config.CodigoRubricaMultaFgts), request.ValorMultaFgts),

            _ => (Rubrica.De(config.CodigoRubricaSaldoSalario), 0m),
        };

    private static DateOnly InicioAno(GerarVerbasRescisoriasCommand request)
    {
        var inicioAno = new DateOnly(request.DataDesligamento.Year, 1, 1);
        return request.Admissao > inicioAno ? request.Admissao : inicioAno;
    }

    private static decimal ValorFeriasComTerco(decimal vencimento, int dias, decimal fracaoTerco)
    {
        var ferias = CalculadoraFerias.Calcular(vencimento, dias, 0, fracaoTerco);
        return ferias.RemuneracaoFerias + ferias.TercoConstitucional;
    }

    private static decimal ValorFeriasProporcionais(GerarVerbasRescisoriasCommand request, decimal fracaoTerco)
    {
        // Avos do periodo aquisitivo em curso ate o desligamento (regra dos 15 dias, sem relogio).
        var avos = Avos.Apurar(request.InicioPeriodoAquisitivoFerias, request.DataDesligamento, []);
        // 30 dias / 12 avos = 2,5 dias por avo (CLT art. 130 — escala simplificada).
        var dias = (int)Math.Round(avos.Quantidade * (CalculadoraFerias.DiasBaseMes / 12m), MidpointRounding.AwayFromZero);
        if (dias > CalculadoraFerias.DiasBaseMes)
        {
            dias = CalculadoraFerias.DiasBaseMes;
        }

        return ValorFeriasComTerco(request.Vencimento, dias, fracaoTerco);
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
