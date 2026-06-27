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

/// <summary>Veículo reavaliado/impairment ao valor justo informado (efeito prospectivo na depreciação).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="NovoValorContabil">Novo valor contábil resultante.</param>
public sealed record VeiculoReavaliado(VeiculoId VeiculoId, decimal NovoValorContabil) : IDomainEvent;

/// <summary>Veículo baixado/alienado do acervo (saída contábil; cessa a depreciação).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Motivo">Motivo da baixa/alienação.</param>
/// <param name="ValorContabil">Valor contábil na saída.</param>
public sealed record VeiculoBaixado(VeiculoId VeiculoId, string Motivo, decimal ValorContabil) : IDomainEvent;

/// <summary>Pneu instalado em uma posição (eixo/lado) de um veículo (controle de posicionamento).</summary>
/// <param name="PneuId">Identificador do pneu.</param>
/// <param name="VeiculoId">Veículo onde o pneu foi instalado.</param>
/// <param name="Posicao">Posição (eixo/lado) de montagem.</param>
/// <param name="Odometro">Odômetro do veículo na instalação (km).</param>
public sealed record PneuInstalado(PneuId PneuId, VeiculoId VeiculoId, PosicaoPneu Posicao, int Odometro) : IDomainEvent;

/// <summary>Pneu removido de um veículo (rodízio/reposicionamento/manutenção); acumula km rodados.</summary>
/// <param name="PneuId">Identificador do pneu.</param>
/// <param name="VeiculoId">Veículo de onde o pneu foi removido.</param>
/// <param name="KmRodadosNoCiclo">Quilômetros rodados nesta passagem (ciclo) de instalação.</param>
public sealed record PneuRemovido(PneuId PneuId, VeiculoId VeiculoId, int KmRodadosNoCiclo) : IDomainEvent;

/// <summary>Pneu descartado/sucateado (fim de vida útil por sulco mínimo ou km), saída do controle.</summary>
/// <param name="PneuId">Identificador do pneu.</param>
/// <param name="Motivo">Motivo do descarte.</param>
/// <param name="KmTotalAcumulado">Quilômetros totais rodados pelo pneu até o descarte.</param>
public sealed record PneuDescartado(PneuId PneuId, string Motivo, int KmTotalAcumulado) : IDomainEvent;

/// <summary>Apólice de seguro de veículo contratada (cobertura passa a vigente).</summary>
/// <param name="ApoliceId">Identificador da apólice.</param>
/// <param name="VeiculoId">Veículo segurado.</param>
/// <param name="FimVigencia">Data de término da vigência (alvo do alerta de vencimento).</param>
public sealed record ApoliceContratada(ApoliceId ApoliceId, VeiculoId VeiculoId, DateOnly FimVigencia) : IDomainEvent;

/// <summary>CNH de um condutor renovada/atualizada (efeito direto no bloqueio de viagem por validade).</summary>
/// <param name="CondutorId">Identificador do condutor.</param>
/// <param name="NovaValidade">Nova data de validade do exame de aptidão (CNH).</param>
public sealed record CnhRenovada(CondutorId CondutorId, DateOnly NovaValidade) : IDomainEvent;
