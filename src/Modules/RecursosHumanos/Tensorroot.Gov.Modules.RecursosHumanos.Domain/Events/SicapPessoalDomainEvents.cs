using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Remessa de pessoal SICAP-AP/SIAPES aberta (situacao inicial <c>Aberta</c>).</summary>
/// <param name="RemessaId">Identificador da remessa.</param>
/// <param name="CodigoOrgao">Codigo do orgao remetente.</param>
/// <param name="SequencialLote">Sequencial do lote no orgao/tenant.</param>
public sealed record RemessaSicapAberta(RemessaSicapPessoalId RemessaId, int CodigoOrgao, int SequencialLote) : IDomainEvent;

/// <summary>Remessa de pessoal gerada (arquivo de importacao fechado; pronta para transmissao).</summary>
/// <param name="RemessaId">Identificador da remessa.</param>
/// <param name="CodigoOrgao">Codigo do orgao remetente.</param>
/// <param name="SequencialLote">Sequencial do lote.</param>
/// <param name="QuantidadeAtos">Quantidade de atos no corpo.</param>
public sealed record RemessaSicapGerada(
    RemessaSicapPessoalId RemessaId,
    int CodigoOrgao,
    int SequencialLote,
    int QuantidadeAtos) : IDomainEvent;
