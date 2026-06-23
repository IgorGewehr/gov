using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>
/// Apura os descontos legais (INSS/RPPS/IRRF) de uma folha ABERTA, por servidor, com o
/// <see cref="MotorDeCalculoFolha"/> e as tabelas parametrizadas por competencia. Resolve as
/// incidencias de cada provento lancado contra o catalogo de <c>RubricaFolha</c>, calcula previdencia
/// e IRRF deterministicamente e lanca os eventos de desconto correspondentes (substituindo apuracoes
/// previas). Apos esta apuracao, o calculo de totais/abate-teto e feito por <c>CalcularFolha</c>.
/// </summary>
/// <param name="FolhaDePagamentoId">Folha a apurar.</param>
public sealed record ApurarDescontosLegaisCommand(Guid FolhaDePagamentoId) : ICommand;

/// <summary>Regras de validacao da apuracao de descontos legais.</summary>
public sealed class ApurarDescontosLegaisValidator : AbstractValidator<ApurarDescontosLegaisCommand>
{
    /// <summary>Define as regras.</summary>
    public ApurarDescontosLegaisValidator()
        => RuleFor(c => c.FolhaDePagamentoId).NotEmpty().WithMessage("Folha e obrigatoria.");
}

/// <summary>Handler da apuracao dos descontos legais da folha.</summary>
public sealed class ApurarDescontosLegaisHandler(
    IFolhaDePagamentoRepository folhas,
    IRubricaFolhaRepository rubricas,
    IServidorRegimeConsulta servidores,
    ITabelasLegaisProvider tabelas,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ApurarDescontosLegaisCommand>
{
    /// <inheritdoc />
    public async Task Handle(ApurarDescontosLegaisCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        var competencia = folha.Competencia;
        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);

        var tabelaInss = await tabelas.ObterInssVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaRpps = await tabelas.ObterRppsVigenteAsync(competencia, cancellationToken).ConfigureAwait(false);
        var tabelaIrrf = await tabelas.ObterIrrfVigenteAsync(competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new CalculoFolhaException("Tabela IRRF nao carregada para a competencia.");

        // Mapa de incidencias por codigo de rubrica vigente (somar base por incidencia, nao por nome).
        var catalogo = await rubricas.ListarVigentesAsync(competencia, cancellationToken).ConfigureAwait(false);
        var incidencias = catalogo.ToDictionary(
            r => r.Codigo.Codigo,
            r => (r.IncideInss, r.IncideRpps, r.IncideIrrf),
            StringComparer.OrdinalIgnoreCase);

        // Codigos das rubricas de desconto legal apuradas pelo motor (parametrizaveis por tenant).
        var codigoInss = Rubrica.De(config.CodigoRubricaInss);
        var codigoRpps = Rubrica.De(config.CodigoRubricaRpps);
        var codigoIrrf = Rubrica.De(config.CodigoRubricaIrrf);
        var legais = new[] { codigoInss, codigoRpps, codigoIrrf };

        foreach (var servidorId in folha.Eventos.Select(e => e.ServidorId).Distinct().ToList())
        {
            // Remove apuracoes legais anteriores (re-apuracao idempotente).
            folha.RemoverDescontosLegais(servidorId, legais);

            var dados = await servidores.ObterDadosCalculoAsync(servidorId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Servidor {servidorId} nao encontrado.");

            var verbas = folha.Eventos
                .Where(e => e.ServidorId == servidorId && !Array.Exists(legais, c => c == e.Rubrica))
                .Select(e =>
                {
                    // FAIL-CLOSED (CLAUDE.md S16): se a rubrica lancada nao tem vigencia valida na
                    // competencia, NAO presumir base zero — isso subtributaria INSS/IRRF silenciosamente,
                    // sem erro nem rastro. Interromper a apuracao e sinalizar erro auditavel pelo TCE.
                    if (!incidencias.TryGetValue(e.Rubrica.Codigo, out var flags))
                    {
                        throw new CalculoFolhaException(
                            $"Rubrica '{e.Rubrica.Codigo}' lancada para o servidor {servidorId} nao possui " +
                            $"vigencia valida na competencia {competencia}. Apuracao interrompida (fail-closed): " +
                            "as incidencias de INSS/RPPS/IRRF nao podem ser presumidas zero.");
                    }

                    var (inss, rpps, irrf) = flags;
                    return new VerbaCalculo(e.Rubrica, e.Tipo == TipoEvento.Provento, e.Valor, inss, rpps, irrf);
                })
                .ToList();

            var insumos = new InsumosCalculoServidor(servidorId, dados.Regime, dados.QuantidadeDependentes, 0m, verbas);
            var resultado = MotorDeCalculoFolha.Calcular(insumos, tabelaInss, tabelaIrrf, tabelaRpps);

            LancarSeHouver(folha, servidorId, codigoInss, resultado.BaseInss, resultado.DescontoInss, dados.Regime);
            LancarSeHouver(folha, servidorId, codigoRpps, resultado.BaseRpps, resultado.DescontoRpps, dados.Regime);
            LancarSeHouver(folha, servidorId, codigoIrrf, resultado.BaseIrrf, resultado.DescontoIrrf, dados.Regime);
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
