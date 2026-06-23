using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>
/// EFEITO NA FOLHA das consignacoes (design RH §2.5): lanca, numa folha ABERTA, os descontos consignados de
/// cada servidor com contrato Averbado vigente, RESPEITANDO a margem disponivel apurada da MESMA competencia
/// (corte por prioridade quando a base caiu). Ordem na consolidacao: rodar APOS os descontos legais
/// (ConsolidarDescontosLegaisMensais) — o consignado incide sobre a margem da remuneracao, ja com proventos
/// ajustados por afastamento. Idempotente: remove os descontos consignados ja lancados (pelas rubricas dos
/// contratos do servidor) antes de relancar. Registra a glosa por insuficiencia de margem para auditoria.
/// </summary>
/// <param name="FolhaDePagamentoId">Folha (aberta) que recebe os descontos consignados.</param>
public sealed record LancarConsignadosNaFolhaCommand(Guid FolhaDePagamentoId) : ICommand<int>;

/// <summary>Regras de validacao do lancamento de consignados na folha.</summary>
public sealed class LancarConsignadosNaFolhaValidator : AbstractValidator<LancarConsignadosNaFolhaCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarConsignadosNaFolhaValidator()
        => RuleFor(c => c.FolhaDePagamentoId).NotEmpty().WithMessage("Folha e obrigatoria.");
}

/// <summary>Handler do lancamento de descontos consignados na folha (com corte por margem).</summary>
public sealed class LancarConsignadosNaFolhaHandler(
    IFolhaDePagamentoRepository folhas,
    IServidorRegimeConsulta servidores,
    LancadorDescontosConsignados lancador,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LancarConsignadosNaFolhaCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(LancarConsignadosNaFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        var servidoresNaFolha = folha.Eventos.Select(e => e.ServidorId).Distinct().ToList();
        var lancados = 0;

        foreach (var servidorId in servidoresNaFolha)
        {
            var resolvidos = await lancador.ResolverAsync(servidorId, folha.Competencia, cancellationToken).ConfigureAwait(false);
            if (resolvidos.Count == 0)
            {
                continue;
            }

            var regime = await servidores.ObterRegimeAsync(servidorId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Servidor {servidorId} nao encontrado.");

            // Idempotencia: remove os descontos consignados ja lancados (pelas rubricas dos contratos do
            // servidor) antes de relancar — recalculo seguro enquanto a folha estiver aberta.
            var rubricasConsignadas = resolvidos.Select(r => Rubrica.De(r.CodigoRubrica)).Distinct().ToList();
            folha.RemoverDescontosLegais(servidorId, rubricasConsignadas);

            foreach (var desconto in resolvidos.Where(d => d.ValorLancado > 0m))
            {
                folha.AdicionarEvento(
                    servidorId,
                    Rubrica.De(desconto.CodigoRubrica),
                    TipoEvento.Desconto,
                    BaseCalculo.De(desconto.ValorContratado),
                    desconto.ValorLancado,
                    regime);
                lancados++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return lancados;
    }
}
