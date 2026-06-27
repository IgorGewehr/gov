using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>Semeia a tabela default de IRRF/PJ (IN RFB 1.234/2012) para o tenant. Idempotente.</summary>
public sealed record SemearTabelaIrrfServicosCommand : ICommand<int>;

/// <summary>Handler do seed da tabela de IRRF/PJ.</summary>
public sealed class SemearTabelaIrrfServicosHandler(
    ITabelaIrrfServicosRepository tabelas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<SemearTabelaIrrfServicosCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(SemearTabelaIrrfServicosCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await tabelas.ExisteVigenciaAsync(TabelaIrrfServicosCatalogo.VigenciaInicioPadrao, cancellationToken).ConfigureAwait(false))
        {
            return 0;
        }

        var tabela = TabelaIrrfServicos.Criar(
            tenant.TenantId,
            TabelaIrrfServicosCatalogo.VigenciaInicioPadrao,
            vigenciaFim: null,
            TabelaIrrfServicosCatalogo.ValorMinimoRetencaoPadrao,
            TabelaIrrfServicosCatalogo.Faixas());

        tabelas.Adicionar(tabela);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tabela.Faixas.Count;
    }
}
