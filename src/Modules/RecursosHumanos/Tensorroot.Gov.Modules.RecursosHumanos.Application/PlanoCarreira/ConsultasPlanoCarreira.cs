using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.PlanoCarreira;

/// <summary>Lista os planos de carreira do tenant (navegabilidade); read-only.</summary>
public sealed record ListarPlanosCarreiraQuery : IQuery<IReadOnlyList<PlanoCarreiraResumo>>;

/// <summary>Handler da lista de planos de carreira.</summary>
public sealed class ListarPlanosCarreiraHandler(IPlanoCarreiraRepository planos)
    : IQueryHandler<ListarPlanosCarreiraQuery, IReadOnlyList<PlanoCarreiraResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanoCarreiraResumo>> Handle(ListarPlanosCarreiraQuery request, CancellationToken cancellationToken)
    {
        var itens = await planos.ListarAsync(cancellationToken).ConfigureAwait(false);
        return itens.Select(ProjetarPlanoCarreira.ParaResumo).ToList();
    }
}

/// <summary>Obtem um plano de carreira por identificador com a matriz salarial derivada; read-only.</summary>
/// <param name="PlanoCarreiraId">Identificador do plano.</param>
public sealed record ObterPlanoCarreiraQuery(Guid PlanoCarreiraId) : IQuery<PlanoCarreiraDetalhe>;

/// <summary>Handler do detalhe de plano de carreira.</summary>
public sealed class ObterPlanoCarreiraHandler(IPlanoCarreiraRepository planos)
    : IQueryHandler<ObterPlanoCarreiraQuery, PlanoCarreiraDetalhe>
{
    /// <inheritdoc />
    public async Task<PlanoCarreiraDetalhe> Handle(ObterPlanoCarreiraQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoCarreiraId(request.PlanoCarreiraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Plano de carreira nao encontrado.");
        return ProjetarPlanoCarreira.ParaDetalhe(plano);
    }
}

/// <summary>Obtem o enquadramento vigente de um servidor (posicao + historico); read-only.</summary>
/// <param name="ServidorId">Servidor enquadrado.</param>
public sealed record ObterEnquadramentoDoServidorQuery(Guid ServidorId) : IQuery<EnquadramentoDetalhe?>;

/// <summary>Handler do enquadramento do servidor.</summary>
public sealed class ObterEnquadramentoDoServidorHandler(
    IEnquadramentoServidorRepository enquadramentos,
    IPlanoCarreiraRepository planos)
    : IQueryHandler<ObterEnquadramentoDoServidorQuery, EnquadramentoDetalhe?>
{
    /// <inheritdoc />
    public async Task<EnquadramentoDetalhe?> Handle(ObterEnquadramentoDoServidorQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var enquadramento = await enquadramentos.ObterPorServidorAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false);
        if (enquadramento is null)
        {
            return null;
        }

        var plano = await planos.ObterPorIdAsync(enquadramento.PlanoCarreiraId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Plano de carreira do enquadramento nao encontrado.");
        return ProjetarPlanoCarreira.ParaDetalhe(enquadramento, plano);
    }
}
