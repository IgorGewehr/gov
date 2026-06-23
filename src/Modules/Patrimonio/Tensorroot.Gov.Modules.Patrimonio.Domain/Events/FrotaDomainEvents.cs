using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Events;

/// <summary>Abastecimento de um veículo da frota registrado (I-10).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Litros">Litros abastecidos.</param>
/// <param name="Valor">Valor do abastecimento.</param>
/// <param name="Data">Data do abastecimento.</param>
public sealed record AbastecimentoRegistrado(VeiculoId VeiculoId, decimal Litros, decimal Valor, DateOnly Data) : IDomainEvent;

/// <summary>Manutenção (ordem de serviço) de um veículo concluída (I-7).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="OrdemServicoId">Identificador da ordem de serviço concluída.</param>
/// <param name="CustoRealizado">Custo realizado na manutenção.</param>
public sealed record ManutencaoConcluida(VeiculoId VeiculoId, ManutencaoOsId OrdemServicoId, decimal CustoRealizado) : IDomainEvent;

/// <summary>Multa de trânsito (CTB) atribuída a um veículo registrada (I-8).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="CodigoInfracaoCtb">Código de infração do CTB.</param>
/// <param name="Valor">Valor da multa.</param>
/// <param name="DataInfracao">Data da infração.</param>
public sealed record MultaRegistrada(VeiculoId VeiculoId, string CodigoInfracaoCtb, decimal Valor, DateOnly DataInfracao) : IDomainEvent;

/// <summary>Depreciação de uma competência reconhecida sobre o veículo (BUG-P4).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="ValorDepreciado">Valor depreciado na competência.</param>
/// <param name="Competencia">Mês/ano de referência do reconhecimento.</param>
public sealed record VeiculoDepreciado(VeiculoId VeiculoId, decimal ValorDepreciado, DateOnly Competencia) : IDomainEvent;
