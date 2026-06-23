using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>
/// Aluno cadastrado na rede de ensino (situacao inicial <see cref="SituacaoAluno.Ativo"/>).
/// Emitida pela fabrica <c>Aluno.Cadastrar</c> (I-A1/I-A2/I-A3).
/// </summary>
/// <param name="AlunoId">Identificador do aluno cadastrado.</param>
public sealed record AlunoCadastrado(AlunoId AlunoId) : IDomainEvent;

/// <summary>Dados cadastrais do aluno atualizados (dados civis e/ou endereco).</summary>
/// <param name="AlunoId">Identificador do aluno atualizado.</param>
public sealed record DadosAlunoAtualizados(AlunoId AlunoId) : IDomainEvent;

/// <summary>Responsavel adicionado ao aluno (LGPD art. 14).</summary>
/// <param name="AlunoId">Identificador do aluno.</param>
/// <param name="ResponsavelId">Identificador do responsavel adicionado.</param>
public sealed record ResponsavelAdicionado(AlunoId AlunoId, ResponsavelId ResponsavelId) : IDomainEvent;

/// <summary>Aluno inativado (estado terminal).</summary>
/// <param name="AlunoId">Identificador do aluno inativado.</param>
/// <param name="Motivo">Motivo da inativacao.</param>
public sealed record AlunoInativado(AlunoId AlunoId, string Motivo) : IDomainEvent;
