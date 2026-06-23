using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>
/// P0-2: consolidacao fiscal mensal. INSS e IRRF de um servidor sao tributos MENSAIS sobre o conjunto das
/// remuneracoes da competencia — o INSS tem TETO unico e o IRRF e progressivo sobre a SOMA do mes. Quando
/// um servidor recebe em mais de uma folha na MESMA competencia (ex.: Mensal + Ferias), apurar cada folha
/// isolada subtributa (cada parcela fica abaixo do teto INSS e em faixa IRRF menor) -> sub-recolhimento e
/// autuacao. Este caso de uso re-apura INSS/IRRF sobre a base SOMADA das folhas mensais (todas exceto as
/// de BASE SEPARADA — o 13o tem tributacao propria, art. 12-A) e concentra o desconto legal consolidado na
/// folha PRINCIPAL (Mensal, ou a primeira disponivel), removendo as apuracoes isoladas das demais. Assim o
/// total retido na competencia nunca duplica nem subtributa. Deterministico e idempotente.
/// </summary>
/// <param name="Ano">Ano da competencia a consolidar.</param>
/// <param name="Mes">Mes da competencia a consolidar.</param>
public sealed record ConsolidarDescontosLegaisMensaisCommand(int Ano, int Mes) : ICommand;

/// <summary>Validacao da consolidacao fiscal mensal.</summary>
public sealed class ConsolidarDescontosLegaisMensaisValidator : AbstractValidator<ConsolidarDescontosLegaisMensaisCommand>
{
    /// <summary>Define as regras.</summary>
    public ConsolidarDescontosLegaisMensaisValidator()
    {
        RuleFor(c => c.Ano).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
    }
}

/// <summary>Handler da consolidacao fiscal mensal (P0-2).</summary>
public sealed class ConsolidarDescontosLegaisMensaisHandler(
    IFolhaDePagamentoRepository folhas,
    IRubricaFolhaRepository rubricas,
    IServidorRegimeConsulta servidores,
    ITabelasLegaisProvider tabelas,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConsolidarDescontosLegaisMensaisCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConsolidarDescontosLegaisMensaisCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.Mes);
        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);

        var todas = await folhas.ListarPorCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false);

        // Folhas que somam para o INSS/IRRF mensal: tudo MENOS as de BASE SEPARADA (13o, art. 12-A). Estas
        // devem estar ABERTAS para reescrever descontos (re-apuracao idempotente).
        var folhasMensais = todas
            .Where(f => !f.BaseSeparada && f.Situacao == SituacaoFolha.Aberta)
            .ToList();
        if (folhasMensais.Count == 0)
        {
            return;
        }

        var tabelaInss = await tabelas.ObterInssVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaRpps = await tabelas.ObterRppsVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaIrrf = await tabelas.ObterIrrfVigenteAsync(competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new CalculoFolhaException("Tabela IRRF nao carregada para a competencia.");

        var catalogo = await rubricas.ListarVigentesAsync(competencia, cancellationToken).ConfigureAwait(false);
        var incidencias = catalogo.ToDictionary(
            r => r.Codigo.Codigo,
            r => (r.IncideInss, r.IncideRpps, r.IncideIrrf),
            StringComparer.OrdinalIgnoreCase);

        var codigoInss = Rubrica.De(config.CodigoRubricaInss);
        var codigoRpps = Rubrica.De(config.CodigoRubricaRpps);
        var codigoIrrf = Rubrica.De(config.CodigoRubricaIrrf);
        var codigoPensao = Rubrica.De(config.CodigoRubricaPensaoAlimenticia);
        var legais = new[] { codigoInss, codigoRpps, codigoIrrf, codigoPensao };

        // Descontos legais de BASE SEPARADA (13o — art. 12-A) ja lancados (ex.: rescisao com 13o proporcional
        // numa folha Tipo=Rescisao): nao entram na base mensal consolidada nem disparam fail-closed. So ignora.
        var baseSeparada = new[]
        {
            Rubrica.De(config.CodigoRubricaInss13),
            Rubrica.De(config.CodigoRubricaRpps13),
            Rubrica.De(config.CodigoRubricaIrrf13),
        };
        var ignorar = legais.Concat(baseSeparada).ToArray();

        // Folha PRINCIPAL que recebe o desconto legal consolidado: a Mensal, se houver; senao a primeira.
        var principal = folhasMensais.FirstOrDefault(f => f.Tipo == TipoFolha.Mensal) ?? folhasMensais[0];

        var servidoresNaCompetencia = folhasMensais
            .SelectMany(f => f.Eventos.Select(e => e.ServidorId))
            .Distinct()
            .ToList();

        foreach (var servidorId in servidoresNaCompetencia)
        {
            // Idempotente: remove os descontos legais (de qualquer folha mensal) antes de reconsolidar.
            foreach (var folha in folhasMensais)
            {
                folha.RemoverDescontosLegais(servidorId, legais);
            }

            var dados = await servidores.ObterDadosCalculoAsync(servidorId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Servidor {servidorId} nao encontrado.");

            // Verbas SOMADAS de todas as folhas mensais da competencia (exclui rubricas legais), com as
            // incidencias resolvidas pelo catalogo vigente (fail-closed se a rubrica nao estiver vigente).
            var verbas = new List<VerbaCalculo>();
            foreach (var folha in folhasMensais)
            {
                foreach (var evento in folha.Eventos.Where(e => e.ServidorId == servidorId && !Array.Exists(ignorar, c => c == e.Rubrica)))
                {
                    if (!incidencias.TryGetValue(evento.Rubrica.Codigo, out var flags))
                    {
                        throw new CalculoFolhaException(
                            $"Rubrica '{evento.Rubrica.Codigo}' do servidor {servidorId} nao possui vigencia valida " +
                            $"na competencia {competencia}. Consolidacao interrompida (fail-closed): as incidencias " +
                            "de INSS/RPPS/IRRF nao podem ser presumidas zero.");
                    }

                    var (inss, rpps, irrf) = flags;
                    verbas.Add(new VerbaCalculo(evento.Rubrica, evento.Tipo == TipoEvento.Provento, evento.Valor, inss, rpps, irrf));
                }
            }

            if (verbas.Count == 0)
            {
                continue;
            }

            // Pensao alimenticia (P0-1) tambem consolida sobre a base do mes inteiro.
            var insumosPrevidencia = new InsumosCalculoServidor(servidorId, dados.Regime, dados.QuantidadeDependentes, 0m, verbas);
            var previa = MotorDeCalculoFolha.Calcular(insumosPrevidencia, tabelaInss, tabelaIrrf, tabelaRpps);
            var pensaoTotal = dados.PensoesAlimenticias.Sum(p => p.Apurar(previa.TotalProventos, previa.DescontoPrevidenciario));

            var insumos = new InsumosCalculoServidor(servidorId, dados.Regime, dados.QuantidadeDependentes, pensaoTotal, verbas);
            var resultado = MotorDeCalculoFolha.Calcular(insumos, tabelaInss, tabelaIrrf, tabelaRpps);

            // Desconto legal CONSOLIDADO concentrado na folha principal (nao duplica entre folhas).
            LancarSeHouver(principal, servidorId, codigoInss, resultado.BaseInss, resultado.DescontoInss, dados.Regime);
            LancarSeHouver(principal, servidorId, codigoRpps, resultado.BaseRpps, resultado.DescontoRpps, dados.Regime);
            LancarSeHouver(principal, servidorId, codigoIrrf, resultado.BaseIrrf, resultado.DescontoIrrf, dados.Regime);

            foreach (var pensao in dados.PensoesAlimenticias)
            {
                var valor = pensao.Apurar(resultado.TotalProventos, resultado.DescontoPrevidenciario);
                LancarSeHouver(principal, servidorId, codigoPensao, valor, valor, dados.Regime);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void LancarSeHouver(
        FolhaDePagamento folha,
        Guid servidorId,
        Rubrica rubrica,
        decimal baseCalculo,
        decimal valor,
        RegimePrevidenciario regime)
    {
        if (valor <= 0m)
        {
            return;
        }

        folha.AdicionarEvento(servidorId, rubrica, TipoEvento.Desconto, BaseCalculo.De(baseCalculo), valor, regime);
    }
}
