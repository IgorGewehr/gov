using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.SharedKernel;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Item cadastrado no catalogo de medicamentos (REMUME).</summary>
/// <param name="MedicamentoId">Identificador do medicamento.</param>
/// <param name="PrincipioAtivo">Principio ativo do item.</param>
public sealed record MedicamentoCadastrado(MedicamentoId MedicamentoId, string PrincipioAtivo) : IDomainEvent;

/// <summary>Entrada de lote registrada no estoque de um medicamento num estabelecimento.</summary>
/// <param name="EstoqueId">Identificador da posicao de estoque.</param>
/// <param name="MedicamentoId">Medicamento que recebeu a entrada.</param>
/// <param name="EstabelecimentoId">Estabelecimento detentor do estoque.</param>
/// <param name="NumeroLote">Numero do lote movimentado.</param>
/// <param name="Quantidade">Quantidade que entrou.</param>
public sealed record EntradaMedicamentoRegistrada(
    EstoqueMedicamentoId EstoqueId,
    MedicamentoId MedicamentoId,
    EstabelecimentoId EstabelecimentoId,
    string NumeroLote,
    decimal Quantidade) : IDomainEvent;

/// <summary>Medicamento dispensado a um paciente (um por item entregue) — base da trilha LGPD/indicadores.</summary>
/// <param name="DispensacaoId">Identificador da dispensacao.</param>
/// <param name="PacienteId">Paciente que retirou.</param>
/// <param name="MedicamentoId">Medicamento entregue.</param>
/// <param name="Quantidade">Quantidade entregue.</param>
public sealed record MedicamentoDispensado(
    DispensacaoId DispensacaoId,
    PacienteId PacienteId,
    MedicamentoId MedicamentoId,
    decimal Quantidade) : IDomainEvent;

/// <summary>Dispensacao estornada (devolucao/erro) — saldo de estoque recomposto.</summary>
/// <param name="DispensacaoId">Identificador da dispensacao.</param>
/// <param name="PacienteId">Paciente da dispensacao estornada.</param>
/// <param name="Motivo">Motivo do estorno.</param>
public sealed record DispensacaoEstornada(
    DispensacaoId DispensacaoId,
    PacienteId PacienteId,
    string Motivo) : IDomainEvent;
