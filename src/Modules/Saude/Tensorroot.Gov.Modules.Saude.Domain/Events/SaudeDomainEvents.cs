using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Paciente cadastrado no PEP a partir de um CNS (evento emitido na criacao).</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
/// <param name="Cns">Cartao Nacional de Saude do paciente.</param>
public sealed record PacienteCadastrado(PacienteId PacienteId, Cns Cns) : IDomainEvent;

/// <summary>Cadastro do paciente confirmado/validado na base nacional CADSUS.</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
/// <param name="Cns">Cartao Nacional de Saude confirmado.</param>
public sealed record CadastroConfirmadoNoCadsus(PacienteId PacienteId, Cns Cns) : IDomainEvent;

/// <summary>Dados cadastrais (identificacao/endereco) do paciente atualizados.</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
public sealed record CadastroAtualizado(PacienteId PacienteId) : IDomainEvent;

/// <summary>Condicao de saude registrada no historico clinico do paciente.</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
/// <param name="Codigo">Codigo CID-10/CIAP-2 da condicao registrada.</param>
public sealed record CondicaoDeSaudeRegistrada(PacienteId PacienteId, string Codigo) : IDomainEvent;

/// <summary>Alergia registrada no historico clinico do paciente.</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
/// <param name="Substancia">Substancia/agente da alergia registrada.</param>
public sealed record AlergiaRegistrada(PacienteId PacienteId, string Substancia) : IDomainEvent;

/// <summary>Paciente inativado administrativamente (obito, transferencia, duplicidade).</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
/// <param name="Motivo">Motivo da inativacao.</param>
public sealed record PacienteInativado(PacienteId PacienteId, string Motivo) : IDomainEvent;
