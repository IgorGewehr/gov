namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Dados civis do paciente (entrada de comando).</summary>
/// <param name="Nome">Nome civil (obrigatorio, ate 120 caracteres).</param>
/// <param name="DataNascimento">Data de nascimento (nao futura).</param>
/// <param name="Sexo">Sexo (1 = Feminino, 2 = Masculino, 9 = Ignorado).</param>
/// <param name="NomeSocial">Nome social (opcional).</param>
/// <param name="Cpf">CPF com ou sem mascara (opcional).</param>
public sealed record IdentificacaoDto(
    string Nome,
    DateOnly DataNascimento,
    int Sexo,
    string? NomeSocial,
    string? Cpf);

/// <summary>Endereco residencial do paciente (entrada de comando).</summary>
/// <param name="Logradouro">Logradouro (obrigatorio).</param>
/// <param name="Numero">Numero/complemento.</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">Unidade da federacao (2 caracteres).</param>
/// <param name="Cep">CEP (somente digitos).</param>
public sealed record EnderecoDto(
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string Cep);
