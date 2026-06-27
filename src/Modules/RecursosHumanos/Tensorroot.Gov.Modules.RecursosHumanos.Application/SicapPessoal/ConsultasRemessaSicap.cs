using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.SicapPessoal;

/// <summary>Resumo de uma remessa de pessoal SICAP-AP/SIAPES (linha de lista).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="CodigoOrgao">Codigo do orgao remetente.</param>
/// <param name="SequencialLote">Sequencial do lote.</param>
/// <param name="DataGeracaoLote">Data de geracao do lote.</param>
/// <param name="QuantidadeAtos">Quantidade de atos no corpo.</param>
/// <param name="Situacao">Situacao da remessa.</param>
/// <param name="Protocolo">Protocolo de transmissao (quando houver).</param>
public sealed record RemessaSicapResumo(
    Guid Id,
    int CodigoOrgao,
    int SequencialLote,
    DateOnly DataGeracaoLote,
    int QuantidadeAtos,
    string Situacao,
    string? Protocolo);

/// <summary>Item de ato de admissao da remessa (linha do corpo) — CPF mascarado (LGPD).</summary>
/// <param name="Id">Identificador do ato.</param>
/// <param name="IdentificadorAto">Identificador unico do ato.</param>
/// <param name="TipoAto">Titulo de admissao.</param>
/// <param name="Regime">Regime juridico.</param>
/// <param name="Nome">Nome do servidor.</param>
/// <param name="CpfMascarado">CPF mascarado (***.NNN.NNN-**).</param>
/// <param name="DescricaoCargo">Descricao do cargo.</param>
/// <param name="DataAto">Data do ato.</param>
/// <param name="DataTermino">Data de termino (quando houver).</param>
public sealed record AtoAdmissaoItem(
    Guid Id,
    string IdentificadorAto,
    string TipoAto,
    string Regime,
    string Nome,
    string CpfMascarado,
    string DescricaoCargo,
    DateOnly DataAto,
    DateOnly? DataTermino);

/// <summary>Detalhe de uma remessa (cabecalho + atos do corpo).</summary>
/// <param name="Resumo">Resumo da remessa.</param>
/// <param name="VersaoLeiaute">Versao do leiaute.</param>
/// <param name="Atos">Atos do corpo.</param>
public sealed record RemessaSicapDetalhe(
    RemessaSicapResumo Resumo,
    int VersaoLeiaute,
    IReadOnlyList<AtoAdmissaoItem> Atos);

/// <summary>Projecoes do dominio da remessa para os DTOs (CPF mascarado — LGPD).</summary>
internal static class ProjetarRemessaSicap
{
    internal static RemessaSicapResumo ParaResumo(RemessaSicapPessoal remessa) => new(
        remessa.Id.Value,
        remessa.CodigoOrgao,
        remessa.SequencialLote,
        remessa.DataGeracaoLote,
        remessa.QuantidadeAtos,
        remessa.Situacao.ToString(),
        remessa.ProtocoloTransmissao);

    internal static RemessaSicapDetalhe ParaDetalhe(RemessaSicapPessoal remessa)
    {
        var atos = remessa.Atos
            .OrderBy(a => a.Nome)
            .Select(a => new AtoAdmissaoItem(
                a.Id.Value,
                a.IdentificadorAto,
                a.TipoAto.ToString(),
                a.Regime.ToString(),
                a.Nome,
                MascararCpf(a.Cpf),
                a.DescricaoCargo,
                a.DataAto,
                a.DataTermino))
            .ToList();

        return new RemessaSicapDetalhe(ParaResumo(remessa), remessa.VersaoLeiaute, atos);
    }

    // Mascara o CPF preservando apenas os digitos do meio (LGPD): ***.NNN.NNN-**.
    private static string MascararCpf(string cpf)
    {
        var digitos = new string(cpf.Where(char.IsDigit).ToArray());
        return digitos.Length == 11
            ? $"***.{digitos.Substring(3, 3)}.{digitos.Substring(6, 3)}-**"
            : "***.***.***-**";
    }
}

/// <summary>Lista as remessas de pessoal do tenant (navegabilidade); read-only.</summary>
/// <param name="Situacao">Filtro opcional por situacao.</param>
public sealed record ListarRemessasSicapQuery(SituacaoRemessaSicap? Situacao) : IQuery<IReadOnlyList<RemessaSicapResumo>>;

/// <summary>Handler da lista de remessas.</summary>
public sealed class ListarRemessasSicapHandler(IRemessaSicapPessoalRepository remessas)
    : IQueryHandler<ListarRemessasSicapQuery, IReadOnlyList<RemessaSicapResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemessaSicapResumo>> Handle(ListarRemessasSicapQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await remessas.ListarAsync(request.Situacao, cancellationToken).ConfigureAwait(false);
        return itens.Select(ProjetarRemessaSicap.ParaResumo).ToList();
    }
}

/// <summary>Obtem uma remessa por identificador (cabecalho + atos); read-only.</summary>
/// <param name="RemessaId">Identificador da remessa.</param>
public sealed record ObterRemessaSicapQuery(Guid RemessaId) : IQuery<RemessaSicapDetalhe>;

/// <summary>Handler do detalhe de remessa.</summary>
public sealed class ObterRemessaSicapHandler(IRemessaSicapPessoalRepository remessas)
    : IQueryHandler<ObterRemessaSicapQuery, RemessaSicapDetalhe>
{
    /// <inheritdoc />
    public async Task<RemessaSicapDetalhe> Handle(ObterRemessaSicapQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var remessa = await remessas.ObterPorIdAsync(new RemessaSicapPessoalId(request.RemessaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa de pessoal nao encontrada.");
        return ProjetarRemessaSicap.ParaDetalhe(remessa);
    }
}
