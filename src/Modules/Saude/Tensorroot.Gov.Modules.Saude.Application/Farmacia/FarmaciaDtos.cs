using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>Item da lista do catalogo de medicamentos.</summary>
/// <param name="Id">Identificador do medicamento.</param>
/// <param name="PrincipioAtivo">Principio ativo (DCB/DCI).</param>
/// <param name="Apresentacao">Apresentacao descritiva.</param>
/// <param name="Concentracao">Concentracao/dosagem.</param>
/// <param name="Forma">Forma farmaceutica (descricao).</param>
/// <param name="Unidade">Unidade de medida (descricao).</param>
/// <param name="Controle">Classe de controle especial (descricao).</param>
/// <param name="ExigeReceitaControlada">Indica exigencia de receita controlada.</param>
/// <param name="Ativo">Indica se o item esta ativo.</param>
public sealed record MedicamentoItemLista(
    Guid Id,
    string PrincipioAtivo,
    string Apresentacao,
    string Concentracao,
    string Forma,
    string Unidade,
    string Controle,
    bool ExigeReceitaControlada,
    bool Ativo);

/// <summary>Lote na posicao de estoque.</summary>
/// <param name="NumeroLote">Numero do lote.</param>
/// <param name="Validade">Validade.</param>
/// <param name="Saldo">Saldo remanescente.</param>
/// <param name="Vencido">Indica se o lote esta vencido na data de consulta.</param>
public sealed record LoteDto(string NumeroLote, DateOnly Validade, decimal Saldo, bool Vencido);

/// <summary>Posicao de estoque de um medicamento num estabelecimento.</summary>
/// <param name="EstoqueId">Identificador da posicao de estoque.</param>
/// <param name="EstabelecimentoId">Estabelecimento.</param>
/// <param name="MedicamentoId">Medicamento.</param>
/// <param name="PrincipioAtivo">Principio ativo do medicamento.</param>
/// <param name="Saldo">Saldo total.</param>
/// <param name="SaldoValido">Saldo valido (nao vencido) na data de consulta.</param>
/// <param name="PontoDeRessuprimento">Ponto de ressuprimento.</param>
/// <param name="EmRuptura">Indica ruptura (saldo valido abaixo do ponto).</param>
/// <param name="Lotes">Lotes da posicao.</param>
public sealed record PosicaoEstoqueDto(
    Guid EstoqueId,
    Guid EstabelecimentoId,
    Guid MedicamentoId,
    string PrincipioAtivo,
    decimal Saldo,
    decimal SaldoValido,
    int PontoDeRessuprimento,
    bool EmRuptura,
    IReadOnlyList<LoteDto> Lotes);

/// <summary>Alerta de lote a vencer/vencido.</summary>
/// <param name="EstabelecimentoId">Estabelecimento.</param>
/// <param name="MedicamentoId">Medicamento.</param>
/// <param name="PrincipioAtivo">Principio ativo.</param>
/// <param name="NumeroLote">Lote.</param>
/// <param name="Validade">Validade.</param>
/// <param name="Saldo">Saldo do lote.</param>
/// <param name="Vencido">Indica vencido na data de referencia.</param>
public sealed record AlertaValidadeDto(
    Guid EstabelecimentoId,
    Guid MedicamentoId,
    string PrincipioAtivo,
    string NumeroLote,
    DateOnly Validade,
    decimal Saldo,
    bool Vencido);

/// <summary>Item entregue numa dispensacao (historico).</summary>
/// <param name="MedicamentoId">Medicamento.</param>
/// <param name="Quantidade">Quantidade entregue.</param>
/// <param name="Posologia">Posologia orientada.</param>
public sealed record ItemDispensadoDto(Guid MedicamentoId, decimal Quantidade, string Posologia);

/// <summary>Registro de dispensacao no historico do paciente.</summary>
/// <param name="Id">Identificador da dispensacao.</param>
/// <param name="EstabelecimentoId">Estabelecimento.</param>
/// <param name="DataHora">Data/hora.</param>
/// <param name="Situacao">Situacao (descricao).</param>
/// <param name="PrescricaoId">Prescricao de origem (opcional).</param>
/// <param name="Itens">Itens entregues.</param>
public sealed record DispensacaoDto(
    Guid Id,
    Guid EstabelecimentoId,
    DateTimeOffset DataHora,
    string Situacao,
    Guid? PrescricaoId,
    IReadOnlyList<ItemDispensadoDto> Itens);

/// <summary>Linha de pedido de dispensacao (medicamento + quantidade + posologia).</summary>
/// <param name="MedicamentoId">Medicamento a dispensar.</param>
/// <param name="Quantidade">Quantidade a entregar (> 0).</param>
/// <param name="Posologia">Posologia/orientacao de uso.</param>
public sealed record ItemDispensacaoInput(Guid MedicamentoId, decimal Quantidade, string Posologia);
