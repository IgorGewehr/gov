using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Queries;

/// <summary>Resumo de um PPA.</summary>
public sealed record PpaResumo(Guid Id, int AnoInicio, int AnoFim, string NumeroLei, string Situacao, int QuantidadeProgramas);

/// <summary>Resumo de uma LDO.</summary>
public sealed record LdoResumo(Guid Id, int Exercicio, Guid PpaId, string NumeroLei, string Situacao, int QuantidadePrioridades, int QuantidadeMetasFiscais);

/// <summary>Resumo de uma LOA.</summary>
public sealed record LoaResumo(
    Guid Id,
    int Exercicio,
    Guid LdoId,
    Guid PpaId,
    string Situacao,
    decimal TotalReceitaPrevista,
    decimal TotalDespesaFixada,
    decimal LimiteSuplementacaoPercentual);

/// <summary>Linha do QDD (item de despesa fixada).</summary>
public sealed record ItemQddResumo(
    Guid Id,
    string Classificacao,
    Guid AcaoPpaId,
    string NaturezaDespesa,
    decimal ValorFixado,
    Guid? DotacaoId,
    bool OrigemCreditoEspecial);

/// <summary>Relatório de compatibilidade da LOA com PPA/LDO.</summary>
public sealed record CompatibilidadeResumo(bool Compativel, IReadOnlyList<string> Motivos);

/// <summary>Consulta um PPA por identificador.</summary>
public sealed record ConsultarPpaQuery(Guid PpaId) : IQuery<PpaResumo?>;

/// <summary>Consulta uma LDO por identificador.</summary>
public sealed record ConsultarLdoQuery(Guid LdoId) : IQuery<LdoResumo?>;

/// <summary>Consulta uma LOA por identificador.</summary>
public sealed record ConsultarLoaQuery(Guid LoaId) : IQuery<LoaResumo?>;

/// <summary>Lista os itens do QDD de uma LOA.</summary>
public sealed record ListarItensQddQuery(Guid LoaId) : IQuery<IReadOnlyList<ItemQddResumo>>;

/// <summary>Relatório de compatibilidade LOA ⊆ LDO ⊆ PPA + equilíbrio.</summary>
public sealed record ConsultarCompatibilidadeQuery(Guid LoaId) : IQuery<CompatibilidadeResumo?>;

/// <summary>Handler da consulta de PPA.</summary>
public sealed class ConsultarPpaHandler(IPpaRepository ppas) : IQueryHandler<ConsultarPpaQuery, PpaResumo?>
{
    /// <inheritdoc />
    public async Task<PpaResumo?> Handle(ConsultarPpaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new Domain.Planejamento.Ppa.PpaId(request.PpaId), cancellationToken).ConfigureAwait(false);
        return ppa is null ? null : new PpaResumo(ppa.Id.Value, ppa.AnoInicio, ppa.AnoFim, ppa.NumeroLei, ppa.Situacao.ToString(), ppa.Programas.Count);
    }
}

/// <summary>Handler da consulta de LDO.</summary>
public sealed class ConsultarLdoHandler(ILdoRepository ldos) : IQueryHandler<ConsultarLdoQuery, LdoResumo?>
{
    /// <inheritdoc />
    public async Task<LdoResumo?> Handle(ConsultarLdoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new Domain.Planejamento.Ldo.LdoId(request.LdoId), cancellationToken).ConfigureAwait(false);
        return ldo is null
            ? null
            : new LdoResumo(ldo.Id.Value, ldo.Exercicio, ldo.PpaId.Value, ldo.NumeroLei, ldo.Situacao.ToString(), ldo.Prioridades.Count, ldo.MetasFiscais.Count);
    }
}

/// <summary>Handler da consulta de LOA.</summary>
public sealed class ConsultarLoaHandler(ILoaRepository loas) : IQueryHandler<ConsultarLoaQuery, LoaResumo?>
{
    /// <inheritdoc />
    public async Task<LoaResumo?> Handle(ConsultarLoaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false);
        return loa is null
            ? null
            : new LoaResumo(
                loa.Id.Value, loa.Exercicio, loa.LdoId.Value, loa.PpaId.Value, loa.Situacao.ToString(),
                loa.TotalReceitaPrevista.Valor, loa.TotalDespesaFixada.Valor, loa.LimiteSuplementacaoPercentual);
    }
}

/// <summary>Handler da listagem de itens do QDD.</summary>
public sealed class ListarItensQddHandler(ILoaRepository loas) : IQueryHandler<ListarItensQddQuery, IReadOnlyList<ItemQddResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemQddResumo>> Handle(ListarItensQddQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false);
        if (loa is null)
        {
            return [];
        }

        return loa.Itens
            .Select(i => new ItemQddResumo(
                i.Id.Value, i.Classificacao.ParaTexto(), i.AcaoPpaId.Value, i.NaturezaDespesa,
                i.ValorFixado.Valor, i.DotacaoId?.Value, i.OrigemCreditoEspecial))
            .ToList();
    }
}

/// <summary>Handler do relatório de compatibilidade.</summary>
public sealed class ConsultarCompatibilidadeHandler(ILoaRepository loas, ICompatibilidadeOrcamentariaService servico)
    : IQueryHandler<ConsultarCompatibilidadeQuery, CompatibilidadeResumo?>
{
    /// <inheritdoc />
    public async Task<CompatibilidadeResumo?> Handle(ConsultarCompatibilidadeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loa = await loas.ObterPorIdAsync(new LoaId(request.LoaId), cancellationToken).ConfigureAwait(false);
        if (loa is null)
        {
            return null;
        }

        var resultado = await servico.VerificarAsync(loa, cancellationToken).ConfigureAwait(false);
        return new CompatibilidadeResumo(resultado.Compativel, resultado.Motivos);
    }
}
