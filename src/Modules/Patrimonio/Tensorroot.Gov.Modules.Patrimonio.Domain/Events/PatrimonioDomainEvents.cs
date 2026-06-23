using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Events;

/// <summary>Bem patrimonial incorporado ao acervo (variação patrimonial aumentativa).</summary>
/// <param name="BemPatrimonialId">Identificador do bem.</param>
/// <param name="ValorInicial">Valor de incorporação (custo de ingresso).</param>
/// <param name="Origem">Origem do ingresso (aquisição, doação, produção própria).</param>
public sealed record BemIncorporado(BemPatrimonialId BemPatrimonialId, ValorMonetario ValorInicial, string Origem) : IDomainEvent;

/// <summary>Bem patrimonial tombado (registrado com número de tombo único).</summary>
/// <param name="BemPatrimonialId">Identificador do bem.</param>
/// <param name="NumeroTombamento">Número de tombamento atribuído.</param>
public sealed record BemTombado(BemPatrimonialId BemPatrimonialId, string NumeroTombamento) : IDomainEvent;

/// <summary>Depreciação de uma competência reconhecida sobre o bem.</summary>
/// <param name="BemPatrimonialId">Identificador do bem.</param>
/// <param name="ValorDepreciado">Valor depreciado na competência.</param>
/// <param name="Competencia">Mês/ano de referência do reconhecimento.</param>
public sealed record BemDepreciado(BemPatrimonialId BemPatrimonialId, decimal ValorDepreciado, DateOnly Competencia) : IDomainEvent;

/// <summary>Bem reavaliado a valor justo (ou ajustado por impairment).</summary>
/// <param name="BemPatrimonialId">Identificador do bem.</param>
/// <param name="NovoValor">Novo valor contábil resultante.</param>
public sealed record BemReavaliado(BemPatrimonialId BemPatrimonialId, decimal NovoValor) : IDomainEvent;

/// <summary>Bem baixado/alienado do acervo (variação patrimonial diminutiva).</summary>
/// <param name="BemPatrimonialId">Identificador do bem.</param>
/// <param name="MotivoBaixa">Motivo da baixa (ou "Alienacao").</param>
/// <param name="ValorContabil">Valor contábil no momento da baixa.</param>
public sealed record BemBaixado(BemPatrimonialId BemPatrimonialId, string MotivoBaixa, ValorMonetario ValorContabil) : IDomainEvent;

/// <summary>Requisição de material atendida — gerou saída e reconheceu a despesa no consumo (I-7).</summary>
/// <param name="ItemEstoqueId">Identificador do item de estoque.</param>
/// <param name="RequisicaoId">Identificador da requisição atendida.</param>
/// <param name="Quantidade">Quantidade baixada no atendimento.</param>
public sealed record RequisicaoAtendida(ItemEstoqueId ItemEstoqueId, RequisicaoId RequisicaoId, decimal Quantidade) : IDomainEvent;

/// <summary>Ponto de pedido atingido após uma saída — dispara a reposição junto à Administração (I-6).</summary>
/// <param name="ItemEstoqueId">Identificador do item de estoque.</param>
/// <param name="SaldoAtual">Saldo resultante após a baixa.</param>
public sealed record PontoPedidoAtingido(ItemEstoqueId ItemEstoqueId, decimal SaldoAtual) : IDomainEvent;

/// <summary>Inventário aberto (levantamento iniciado — Lei 4.320 art. 96).</summary>
/// <param name="InventarioId">Identificador do inventário.</param>
/// <param name="Exercicio">Exercício (ano-base) do levantamento.</param>
/// <param name="Tipo">Tipo (anual/por setor/eventual/transferência).</param>
public sealed record InventarioAberto(InventarioId InventarioId, int Exercicio, TipoInventario Tipo) : IDomainEvent;

/// <summary>Inventário encerrado — apura as recomendações de movimentação/baixa para efetivação downstream.</summary>
/// <param name="InventarioId">Identificador do inventário.</param>
/// <param name="Exercicio">Exercício (ano-base) do levantamento.</param>
/// <param name="TotalDivergencias">Quantidade de divergências apuradas.</param>
public sealed record InventarioEncerrado(InventarioId InventarioId, int Exercicio, int TotalDivergencias) : IDomainEvent;
