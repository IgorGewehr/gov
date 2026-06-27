using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Plano de carreira (PCCS) instituido por lei (situacao inicial <c>Ativo</c>).</summary>
/// <param name="PlanoCarreiraId">Identificador do plano.</param>
/// <param name="DenominacaoCarreira">Denominacao da carreira regida.</param>
public sealed record PlanoCarreiraInstituido(PlanoCarreiraId PlanoCarreiraId, string DenominacaoCarreira) : IDomainEvent;

/// <summary>Servidor enquadrado inicialmente num plano de carreira (posicao de ingresso).</summary>
/// <param name="EnquadramentoId">Identificador do enquadramento.</param>
/// <param name="ServidorId">Servidor enquadrado.</param>
/// <param name="PlanoCarreiraId">Plano de carreira destino.</param>
/// <param name="Classe">Classe de ingresso.</param>
/// <param name="Referencia">Referencia de ingresso.</param>
/// <param name="Vencimento">Vencimento resultante da posicao.</param>
public sealed record ServidorEnquadrado(
    EnquadramentoServidorId EnquadramentoId,
    ServidorId ServidorId,
    PlanoCarreiraId PlanoCarreiraId,
    int Classe,
    int Referencia,
    decimal Vencimento) : IDomainEvent;

/// <summary>Progressao horizontal concedida (avanco de referencia na mesma classe).</summary>
/// <param name="EnquadramentoId">Identificador do enquadramento.</param>
/// <param name="ServidorId">Servidor beneficiado.</param>
/// <param name="Classe">Classe (inalterada) da posicao resultante.</param>
/// <param name="Referencia">Nova referencia.</param>
/// <param name="Vencimento">Vencimento resultante.</param>
public sealed record ProgressaoConcedida(
    EnquadramentoServidorId EnquadramentoId,
    ServidorId ServidorId,
    int Classe,
    int Referencia,
    decimal Vencimento) : IDomainEvent;

/// <summary>Promocao vertical concedida (avanco de classe, voltando a referencia inicial).</summary>
/// <param name="EnquadramentoId">Identificador do enquadramento.</param>
/// <param name="ServidorId">Servidor beneficiado.</param>
/// <param name="Classe">Nova classe.</param>
/// <param name="Referencia">Referencia (inicial) da nova classe.</param>
/// <param name="Vencimento">Vencimento resultante.</param>
public sealed record PromocaoConcedida(
    EnquadramentoServidorId EnquadramentoId,
    ServidorId ServidorId,
    int Classe,
    int Referencia,
    decimal Vencimento) : IDomainEvent;
