using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Pbf;

/// <summary>Linha de condicionalidade do acompanhamento (projecao de leitura, sem dado sigiloso).</summary>
/// <param name="RegistroId">Identificador do registro.</param>
/// <param name="Tipo">Eixo da condicionalidade.</param>
/// <param name="MembroId">Membro da familia.</param>
/// <param name="Status">Status do cumprimento.</param>
/// <param name="Observacao">Observacao/motivo (se houver).</param>
public sealed record CondicionalidadeResultado(Guid RegistroId, string Tipo, Guid MembroId, string Status, string? Observacao);

/// <summary>Resultado consolidado do acompanhamento de condicionalidades de uma familia.</summary>
/// <param name="AcompanhamentoId">Identificador do acompanhamento.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Competencia">Competencia (ano/mes).</param>
/// <param name="Efeito">Efeito gradativo vigente.</param>
/// <param name="DescumprimentosEfetivos">Quantidade de descumprimentos efetivos.</param>
/// <param name="Condicionalidades">Registros do periodo.</param>
public sealed record AcompanhamentoResultado(
    Guid AcompanhamentoId,
    Guid FamiliaId,
    string Competencia,
    string Efeito,
    int DescumprimentosEfetivos,
    IReadOnlyList<CondicionalidadeResultado> Condicionalidades);

/// <summary>
/// 3d.1: lista as familias EM DESCUMPRIMENTO (efeito gradativo &gt;= bloqueio por padrao) numa
/// competencia — base da busca ativa do CRAS, tenant-scoped.
/// </summary>
/// <param name="Competencia">Competencia (ano/mes).</param>
/// <param name="EfeitoMinimo">Efeito minimo a incluir (nulo = somente os com algum efeito, &gt;= advertencia).</param>
public sealed record ObterDescumprimentosQuery(Competencia Competencia, EfeitoDescumprimento? EfeitoMinimo) : IQuery<IReadOnlyList<AcompanhamentoResultado>>;

/// <summary>Handler da listagem de descumprimentos.</summary>
public sealed class ObterDescumprimentosHandler(IAcompanhamentoCondicionalidadeRepository acompanhamentos)
    : IQueryHandler<ObterDescumprimentosQuery, IReadOnlyList<AcompanhamentoResultado>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AcompanhamentoResultado>> Handle(ObterDescumprimentosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var efeito = request.EfeitoMinimo ?? EfeitoDescumprimento.Advertencia;
        var encontrados = await acompanhamentos.ListarPorCompetenciaAsync(request.Competencia, efeito, cancellationToken).ConfigureAwait(false);

        return encontrados.Select(Projetar).ToList();
    }

    private static AcompanhamentoResultado Projetar(AcompanhamentoCondicionalidade acompanhamento)
        => new(
            acompanhamento.Id.Value,
            acompanhamento.FamiliaId.Value,
            acompanhamento.Competencia.ToString(),
            acompanhamento.Efeito.ToString(),
            acompanhamento.DescumprimentosEfetivos,
            acompanhamento.Registros
                .Select(r => new CondicionalidadeResultado(r.Id.Value, r.Tipo.ToString(), r.MembroId, r.Status.ToString(), r.Observacao))
                .ToList());
}
