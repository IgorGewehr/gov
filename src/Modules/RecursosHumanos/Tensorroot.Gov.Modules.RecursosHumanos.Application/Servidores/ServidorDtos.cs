namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Dados cadastrais do servidor para entrada de comando (dados sensiveis — LGPD).</summary>
/// <param name="Nome">Nome civil do servidor.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
public sealed record DadosPessoaisDto(string Nome, DateOnly DataNascimento);

/// <summary>
/// Projecao de leitura de um servidor (CPF mascarado — LGPD). Tenant-scoped via Global Query
/// Filter no DbContext.
/// </summary>
/// <param name="Id">Identificador do servidor.</param>
/// <param name="Cpf">CPF mascarado (LGPD).</param>
/// <param name="Matricula">Matricula do vinculo.</param>
/// <param name="NomeServidor">Nome civil do servidor.</param>
/// <param name="CargoId">Cargo provido.</param>
/// <param name="Regime">Regime previdenciario (texto).</param>
/// <param name="Situacao">Situacao atual (texto).</param>
/// <param name="DataNomeacao">Data de nomeacao.</param>
/// <param name="DataExercicio">Data de inicio de exercicio (nula antes de iniciado).</param>
public sealed record ServidorResumo(
    Guid Id,
    string Cpf,
    string Matricula,
    string NomeServidor,
    Guid CargoId,
    string Regime,
    string Situacao,
    DateOnly DataNomeacao,
    DateOnly? DataExercicio);
