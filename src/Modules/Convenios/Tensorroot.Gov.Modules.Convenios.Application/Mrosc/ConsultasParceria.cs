using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

namespace Tensorroot.Gov.Modules.Convenios.Application.Mrosc;

/// <summary>Item de lista de parceria OSC (read model — fluxo B).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="OscRazaoSocial">Razao social da OSC.</param>
/// <param name="TipoInstrumento">Tipo de instrumento.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record ParceriaListItem(Guid Id, string OscRazaoSocial, string TipoInstrumento, SituacaoParceriaOsc Situacao);

/// <summary>Detalhe de um repasse a OSC (read model).</summary>
/// <param name="NumeroOrdem">Numero de ordem.</param>
/// <param name="Valor">Valor.</param>
/// <param name="Situacao">Situacao do repasse.</param>
/// <param name="ExecucaoCompleta">Espelho de execucao orcamentaria completo (empenho+liquidacao+pagamento).</param>
public sealed record RepasseOscDetalhe(int NumeroOrdem, decimal Valor, string Situacao, bool ExecucaoCompleta);

/// <summary>Detalhe de uma parceria OSC (read model — fluxo B).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="OscRazaoSocial">Razao social.</param>
/// <param name="OscCnpj">CNPJ.</param>
/// <param name="TipoInstrumento">Tipo de instrumento.</param>
/// <param name="FormaSelecao">Forma de selecao.</param>
/// <param name="ValorGlobal">Valor global.</param>
/// <param name="Situacao">Situacao.</param>
/// <param name="VigenciaInicio">Inicio da vigencia (nulo ate a celebracao).</param>
/// <param name="VigenciaFim">Fim da vigencia (nulo ate a celebracao).</param>
/// <param name="PrestacaoSituacao">Situacao da PC da OSC (nula ate a abertura).</param>
/// <param name="Repasses">Repasses da parceria.</param>
public sealed record ParceriaDetalhe(
    Guid Id,
    string OscRazaoSocial,
    string OscCnpj,
    string TipoInstrumento,
    string FormaSelecao,
    decimal ValorGlobal,
    SituacaoParceriaOsc Situacao,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    string? PrestacaoSituacao,
    IReadOnlyList<RepasseOscDetalhe> Repasses);

/// <summary>Lista parcerias do tenant, opcionalmente por situacao.</summary>
/// <param name="Situacao">Situacao (opcional).</param>
public sealed record ListarParceriasQuery(SituacaoParceriaOsc? Situacao) : IQuery<IReadOnlyList<ParceriaListItem>>;

/// <summary>Handler da listagem de parcerias.</summary>
public sealed class ListarParceriasHandler(IParceriaOscRepository parcerias)
    : IQueryHandler<ListarParceriasQuery, IReadOnlyList<ParceriaListItem>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ParceriaListItem>> Handle(ListarParceriasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await parcerias.ListarAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return lista
            .Select(parceria => new ParceriaListItem(
                parceria.Id.Value,
                parceria.Osc.RazaoSocial,
                parceria.TipoInstrumento.ToString(),
                parceria.Situacao))
            .ToList();
    }
}

/// <summary>Detalha uma parceria do tenant.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record ObterParceriaQuery(Guid ParceriaId) : IQuery<ParceriaDetalhe?>;

/// <summary>Handler do detalhe de parceria.</summary>
public sealed class ObterParceriaHandler(IParceriaOscRepository parcerias)
    : IQueryHandler<ObterParceriaQuery, ParceriaDetalhe?>
{
    /// <inheritdoc />
    public async Task<ParceriaDetalhe?> Handle(ObterParceriaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await parcerias.ObterPorIdAsync(new ParceriaOscId(request.ParceriaId), cancellationToken).ConfigureAwait(false);
        if (parceria is null)
        {
            return null;
        }

        var repasses = parceria.Repasses
            .Select(repasse => new RepasseOscDetalhe(
                repasse.NumeroOrdem, repasse.Valor.Valor, repasse.Situacao.ToString(), repasse.ExecucaoOrcamentariaCompleta))
            .ToList();

        return new ParceriaDetalhe(
            parceria.Id.Value,
            parceria.Osc.RazaoSocial,
            parceria.Osc.Cnpj.Digitos,
            parceria.TipoInstrumento.ToString(),
            parceria.FormaSelecao.Tipo.ToString(),
            parceria.Plano?.ValorGlobal.Valor ?? 0m,
            parceria.Situacao,
            parceria.Vigencia?.Inicio,
            parceria.Vigencia?.Fim,
            parceria.Prestacao?.Situacao.ToString(),
            repasses);
    }
}
