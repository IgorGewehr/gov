using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

namespace Tensorroot.Gov.Modules.Convenios.Application.Recebidos;

/// <summary>Item de lista de convenio recebido (read model — fluxo A).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ConcedenteNome">Nome do concedente.</param>
/// <param name="NumeroTransferegov">Numero no Transferegov (nulo ate a celebracao).</param>
/// <param name="Objeto">Objeto do plano.</param>
/// <param name="ValorGlobal">Valor global.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record ConvenioListItem(
    Guid Id,
    string ConcedenteNome,
    string? NumeroTransferegov,
    string Objeto,
    decimal ValorGlobal,
    SituacaoConvenioRecebido Situacao);

/// <summary>Detalhe de uma PC do convenio (read model).</summary>
/// <param name="Id">Identificador da PC.</param>
/// <param name="Tipo">Tipo (parcial/final).</param>
/// <param name="Situacao">Situacao da PC.</param>
/// <param name="DataSubmissao">Data de submissao (nula enquanto pendente).</param>
/// <param name="PrazoAnalise">Prazo de analise (nulo enquanto pendente).</param>
public sealed record PrestacaoConvenioDetalhe(
    Guid Id,
    string Tipo,
    string Situacao,
    DateOnly? DataSubmissao,
    DateOnly? PrazoAnalise);

/// <summary>Detalhe de um convenio recebido (read model — fluxo A).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ConcedenteNome">Nome do concedente.</param>
/// <param name="ConcedenteCnpj">CNPJ do concedente.</param>
/// <param name="NumeroTransferegov">Numero no Transferegov.</param>
/// <param name="Objeto">Objeto.</param>
/// <param name="ValorRepasse">Valor do repasse.</param>
/// <param name="ValorContrapartida">Valor da contrapartida.</param>
/// <param name="ValorGlobal">Valor global.</param>
/// <param name="Situacao">Situacao do convenio.</param>
/// <param name="VigenciaInicio">Inicio da vigencia (nulo ate a celebracao).</param>
/// <param name="VigenciaFim">Fim da vigencia (nulo ate a celebracao).</param>
/// <param name="Prestacoes">PCs do convenio.</param>
public sealed record ConvenioDetalhe(
    Guid Id,
    string ConcedenteNome,
    string ConcedenteCnpj,
    string? NumeroTransferegov,
    string Objeto,
    decimal ValorRepasse,
    decimal ValorContrapartida,
    decimal ValorGlobal,
    SituacaoConvenioRecebido Situacao,
    DateOnly? VigenciaInicio,
    DateOnly? VigenciaFim,
    IReadOnlyList<PrestacaoConvenioDetalhe> Prestacoes);

/// <summary>Lista convenios do tenant, opcionalmente por situacao.</summary>
/// <param name="Situacao">Situacao (opcional).</param>
public sealed record ListarConveniosQuery(SituacaoConvenioRecebido? Situacao) : IQuery<IReadOnlyList<ConvenioListItem>>;

/// <summary>Handler da listagem de convenios.</summary>
public sealed class ListarConveniosHandler(IConvenioRecebidoRepository convenios)
    : IQueryHandler<ListarConveniosQuery, IReadOnlyList<ConvenioListItem>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConvenioListItem>> Handle(ListarConveniosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lista = await convenios.ListarAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return lista
            .Select(convenio => new ConvenioListItem(
                convenio.Id.Value,
                convenio.Concedente.Nome,
                convenio.Concedente.NumeroConvenioTransferegov,
                convenio.Plano.Objeto,
                convenio.Plano.ValorGlobal.Valor,
                convenio.Situacao))
            .ToList();
    }
}

/// <summary>Detalha um convenio do tenant.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
public sealed record ObterConvenioQuery(Guid ConvenioId) : IQuery<ConvenioDetalhe?>;

/// <summary>Handler do detalhe de convenio.</summary>
public sealed class ObterConvenioHandler(IConvenioRecebidoRepository convenios)
    : IQueryHandler<ObterConvenioQuery, ConvenioDetalhe?>
{
    /// <inheritdoc />
    public async Task<ConvenioDetalhe?> Handle(ObterConvenioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await convenios.ObterPorIdAsync(new ConvenioRecebidoId(request.ConvenioId), cancellationToken).ConfigureAwait(false);
        if (convenio is null)
        {
            return null;
        }

        var prestacoes = convenio.Prestacoes
            .Select(pc => new PrestacaoConvenioDetalhe(
                pc.Id, pc.Tipo.ToString(), pc.Situacao.ToString(), pc.DataSubmissao, pc.PrazoAnalise?.Vencimento))
            .ToList();

        return new ConvenioDetalhe(
            convenio.Id.Value,
            convenio.Concedente.Nome,
            convenio.Concedente.Cnpj.Digitos,
            convenio.Concedente.NumeroConvenioTransferegov,
            convenio.Plano.Objeto,
            convenio.Plano.ValorRepasse.Valor,
            convenio.Plano.ValorContrapartida.Valor,
            convenio.Plano.ValorGlobal.Valor,
            convenio.Situacao,
            convenio.Vigencia?.Inicio,
            convenio.Vigencia?.Fim,
            prestacoes);
    }
}
