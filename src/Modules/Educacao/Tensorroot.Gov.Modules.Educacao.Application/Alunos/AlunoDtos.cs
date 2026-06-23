using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>Dados civis de entrada do aluno (corpo dos comandos de cadastro/atualizacao).</summary>
/// <param name="Nome">Nome civil (obrigatorio).</param>
/// <param name="DataNascimento">Data de nascimento (nao futura).</param>
/// <param name="Sexo">Sexo.</param>
/// <param name="NomeMae">Nome da mae (obrigatorio — EducaCenso).</param>
/// <param name="NomePai">Nome do pai (opcional).</param>
/// <param name="NomeSocial">Nome social (opcional).</param>
public sealed record DadosCivisPayload(
    string Nome,
    DateOnly DataNascimento,
    Sexo Sexo,
    string NomeMae,
    string? NomePai,
    string? NomeSocial);

/// <summary>Endereco residencial de entrada do aluno.</summary>
/// <param name="Logradouro">Logradouro (obrigatorio).</param>
/// <param name="Numero">Numero/complemento.</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">Unidade da federacao (2 caracteres).</param>
/// <param name="Cep">CEP.</param>
public sealed record EnderecoAlunoPayload(
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string Cep);

/// <summary>Responsavel de entrada (corpo dos comandos de cadastro/adicao de responsavel).</summary>
/// <param name="Nome">Nome do responsavel (obrigatorio).</param>
/// <param name="Cpf">CPF (opcional).</param>
/// <param name="Parentesco">Grau de parentesco/vinculo.</param>
/// <param name="Telefone">Telefone de contato (opcional).</param>
/// <param name="ResponsavelFinanceiro">Indica se e o responsavel financeiro.</param>
/// <param name="AutorizadoBuscar">Indica se esta autorizado a buscar o aluno.</param>
public sealed record ResponsavelPayload(
    string Nome,
    string? Cpf,
    Parentesco Parentesco,
    string? Telefone,
    bool ResponsavelFinanceiro,
    bool AutorizadoBuscar);

/// <summary>
/// Item da lista de alunos (picker do front): projecao MINIMIZADA (LGPD) — sem CPF na lista.
/// </summary>
/// <param name="Id">Identificador do aluno.</param>
/// <param name="Nome">Nome civil.</param>
/// <param name="NomeSocial">Nome social, quando informado.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="Sexo">Sexo (descricao).</param>
/// <param name="CodigoInepAluno">Codigo INEP do aluno, quando ja atribuido.</param>
/// <param name="Situacao">Situacao atual do cadastro.</param>
public sealed record AlunoItemLista(
    Guid Id,
    string Nome,
    string? NomeSocial,
    DateOnly DataNascimento,
    string Sexo,
    string? CodigoInepAluno,
    string Situacao);

/// <summary>Responsavel projetado na ficha do aluno.</summary>
/// <param name="Id">Identificador do responsavel.</param>
/// <param name="Nome">Nome do responsavel.</param>
/// <param name="CpfMascarado">CPF mascarado (LGPD), quando informado.</param>
/// <param name="Parentesco">Grau de parentesco/vinculo (descricao).</param>
/// <param name="Telefone">Telefone de contato.</param>
/// <param name="ResponsavelFinanceiro">Indica se e o responsavel financeiro.</param>
/// <param name="AutorizadoBuscar">Indica se esta autorizado a buscar o aluno.</param>
public sealed record ResponsavelDto(
    Guid Id,
    string Nome,
    string? CpfMascarado,
    string Parentesco,
    string? Telefone,
    bool ResponsavelFinanceiro,
    bool AutorizadoBuscar);

/// <summary>Ficha completa do aluno (dados civis + endereco + responsaveis).</summary>
/// <param name="Id">Identificador do aluno.</param>
/// <param name="Nome">Nome civil.</param>
/// <param name="NomeSocial">Nome social, quando informado.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="Sexo">Sexo (descricao).</param>
/// <param name="NomeMae">Nome da mae.</param>
/// <param name="NomePai">Nome do pai, quando informado.</param>
/// <param name="CpfMascarado">CPF do aluno mascarado (LGPD), quando informado.</param>
/// <param name="CodigoInepAluno">Codigo INEP do aluno, quando ja atribuido.</param>
/// <param name="Situacao">Situacao atual do cadastro.</param>
/// <param name="Logradouro">Endereco — logradouro.</param>
/// <param name="Numero">Endereco — numero/complemento.</param>
/// <param name="Bairro">Endereco — bairro.</param>
/// <param name="Municipio">Endereco — municipio.</param>
/// <param name="Uf">Endereco — UF.</param>
/// <param name="Cep">Endereco — CEP.</param>
/// <param name="Responsaveis">Responsaveis vinculados.</param>
public sealed record AlunoFicha(
    Guid Id,
    string Nome,
    string? NomeSocial,
    DateOnly DataNascimento,
    string Sexo,
    string NomeMae,
    string? NomePai,
    string? CpfMascarado,
    string? CodigoInepAluno,
    string Situacao,
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string Cep,
    IReadOnlyList<ResponsavelDto> Responsaveis);
